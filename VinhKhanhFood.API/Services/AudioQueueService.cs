using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace VinhKhanhFood.API.Services
{
    public class AudioQueueService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public AudioQueueService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        private class AudioQueueItem
        {
            public string RequestId { get; set; } = Guid.NewGuid().ToString();
            public string ClientId { get; set; } = string.Empty;
            public int PoiId { get; set; }
            public string AudioText { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public CancellationTokenSource Cts { get; set; } = new();
            public TaskCompletionSource<bool> PlayingTcs { get; set; } = new();
        }

        private readonly Dictionary<int, Queue<AudioQueueItem>> _poiQueues = new();
        private readonly Dictionary<int, AudioQueueItem> _currentlyPlaying = new();
        private readonly object _lockObj = new object();

        // Event để notify clients qua SignalR
        public event Func<string, int, int, Task>? OnQueuePositionChanged; // (requestId, poiId, position)
        public event Func<string, int, Task>? OnAudioStart; // (requestId, poiId)
        public event Func<string, int, Task>? OnAudioFinish; // (requestId, poiId)

        public async Task<AudioQueueResponse> EnqueueAudioAsync(int poiId, string clientId, string audioText)
        {
            lock (_lockObj)
            {
                if (!_poiQueues.ContainsKey(poiId))
                {
                    _poiQueues[poiId] = new Queue<AudioQueueItem>();
                }

                var item = new AudioQueueItem
                {
                    ClientId = clientId,
                    PoiId = poiId,
                    AudioText = audioText
                };

                _poiQueues[poiId].Enqueue(item);

                // Nếu là item đầu tiên hoặc không có item đang play → bắt đầu ngay
                if (!_currentlyPlaying.ContainsKey(poiId) && _poiQueues[poiId].Count == 1)
                {
                    _ = PlayNextAudioAsync(poiId);
                    return new AudioQueueResponse
                    {
                        RequestId = item.RequestId,
                        Status = "playing",
                        Position = 1,
                        Message = "Now playing"
                    };
                }

                // Nếu không thì queue
                int position = _poiQueues[poiId].Count;
                _ = OnQueuePositionChanged?.Invoke(item.RequestId, poiId, position);

                return new AudioQueueResponse
                {
                    RequestId = item.RequestId,
                    Status = "queued",
                    Position = position,
                    Message = $"Queued at position {position}"
                };
            }
        }

        private async Task PlayNextAudioAsync(int poiId)
        {
            AudioQueueItem? item = null;

            lock (_lockObj)
            {
                if (!_poiQueues.ContainsKey(poiId) || _poiQueues[poiId].Count == 0)
                    return;

                if (_currentlyPlaying.ContainsKey(poiId))
                    return; // Đang play, đợi xong

                item = _poiQueues[poiId].Dequeue();
                _currentlyPlaying[poiId] = item;

                // 🌟 LOG LƯỢT NGHE NGAY KHI AUDIO THỰC SỰ BẮT ĐẦU PHÁT
                _ = LogAudioListenAsync(item.PoiId, item.ClientId);

                _ = OnAudioStart?.Invoke(item.RequestId, poiId);
            }

            if (item == null)
                return;

            // Giả lập phát âm thanh (ngoài lock)
            // Trong thực tế, duration được tính dựa trên độ dài text
            int durationMs = Math.Max(1000, Math.Min(10000, item.AudioText.Length * 80));

            try
            {
                await Task.Delay(durationMs, item.Cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Audio bị hủy
            }

            lock (_lockObj)
            {
                if (_currentlyPlaying.ContainsKey(poiId) && _currentlyPlaying[poiId].RequestId == item.RequestId)
                {
                    _currentlyPlaying.Remove(poiId);
                    _ = OnAudioFinish?.Invoke(item.RequestId, poiId);

                    // Play cái tiếp theo nếu có
                    if (_poiQueues[poiId].Count > 0)
                    {
                        _ = PlayNextAudioAsync(poiId);
                    }
                }
            }
        }

        public async Task CancelAudioAsync(string requestId, int poiId)
        {
            lock (_lockObj)
            {
                if (_currentlyPlaying.ContainsKey(poiId) && _currentlyPlaying[poiId].RequestId == requestId)
                {
                    _currentlyPlaying[poiId].Cts.Cancel();
                    _currentlyPlaying.Remove(poiId);
                }

                // Hoặc remove từ queue nếu còn chưa play
                if (_poiQueues.ContainsKey(poiId))
                {
                    var toRemove = _poiQueues[poiId].FirstOrDefault(x => x.RequestId == requestId);
                    if (toRemove != null)
                    {
                        var newQueue = new Queue<AudioQueueItem>(
                            _poiQueues[poiId].Where(x => x.RequestId != requestId)
                        );
                        _poiQueues[poiId] = newQueue;
                    }
                }
            }

            await Task.CompletedTask;
        }

        public AudioQueueStatus GetQueueStatus(int poiId)
        {
            lock (_lockObj)
            {
                var playing = _currentlyPlaying.ContainsKey(poiId) ? _currentlyPlaying[poiId] : null;
                var queuedCount = _poiQueues.ContainsKey(poiId) ? _poiQueues[poiId].Count : 0;

                return new AudioQueueStatus
                {
                    PoiId = poiId,
                    IsPlaying = playing != null,
                    CurrentRequestId = playing?.RequestId,
                    QueuedCount = queuedCount,
                    TotalWaiting = queuedCount + (playing != null ? 1 : 0)
                };
            }
        }

        private async Task LogAudioListenAsync(int poiId, string clientId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<VinhKhanhFood.API.Data.AppDbContext>();
                
                var log = new VinhKhanhFood.API.Models.AudioListenLog
                {
                    PoiId = poiId,
                    VisitorId = clientId,
                    ListenedAtUtc = DateTime.UtcNow
                };
                
                context.AudioListenLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi ghi nhận lượt nghe: {ex.Message}");
            }
        }
    }

    public class AudioQueueResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "playing" or "queued"
        public int Position { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AudioQueueStatus
    {
        public int PoiId { get; set; }
        public bool IsPlaying { get; set; }
        public string? CurrentRequestId { get; set; }
        public int QueuedCount { get; set; }
        public int TotalWaiting { get; set; }
    }
}
