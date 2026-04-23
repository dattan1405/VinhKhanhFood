using System.Diagnostics;

namespace VinhKhanhFood.App.Services
{
    public class HeartbeatService
    {
        private readonly QrAccessService _qrAccessService;
        private CancellationTokenSource? _cts;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);

        public HeartbeatService()
        {
            _qrAccessService = new QrAccessService();
        }

        public void Start()
        {
            if (_cts != null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await _qrAccessService.SendHeartbeatAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Global heartbeat error: {ex.Message}");
                    }

                    try
                    {
                        await Task.Delay(_interval, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        public void Stop()
        {
            if (_cts == null)
            {
                return;
            }

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}