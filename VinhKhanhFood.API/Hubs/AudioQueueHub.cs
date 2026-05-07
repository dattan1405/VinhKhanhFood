using Microsoft.AspNetCore.SignalR;
using VinhKhanhFood.API.Services;

namespace VinhKhanhFood.API.Hubs
{
    public class AudioQueueHub : Hub
    {
        private readonly AudioQueueService _audioQueueService;

        public AudioQueueHub(AudioQueueService audioQueueService)
        {
            _audioQueueService = audioQueueService;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task JoinPoiQueue(int poiId)
        {
            // Client join group for POI
            await Groups.AddToGroupAsync(Context.ConnectionId, $"poi-{poiId}");
        }

        public async Task LeavePoiQueue(int poiId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"poi-{poiId}");
        }

        // Server calls these methods to notify clients
        public async Task NotifyQueuePositionChanged(string requestId, int poiId, int position)
        {
            await Clients.Group($"poi-{poiId}").SendAsync("QueuePositionChanged", new
            {
                RequestId = requestId,
                PoiId = poiId,
                Position = position,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyAudioStart(string requestId, int poiId)
        {
            await Clients.Group($"poi-{poiId}").SendAsync("AudioStart", new
            {
                RequestId = requestId,
                PoiId = poiId,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyAudioFinish(string requestId, int poiId)
        {
            await Clients.Group($"poi-{poiId}").SendAsync("AudioFinish", new
            {
                RequestId = requestId,
                PoiId = poiId,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
