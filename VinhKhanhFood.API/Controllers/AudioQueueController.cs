using Microsoft.AspNetCore.Mvc;
using VinhKhanhFood.API.Services;

namespace VinhKhanhFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioQueueController : ControllerBase
    {
        private readonly AudioQueueService _audioQueueService;

        public AudioQueueController(AudioQueueService audioQueueService)
        {
            _audioQueueService = audioQueueService;
        }

        [HttpPost("play-with-queue")]
        public async Task<IActionResult> PlayAudioWithQueue([FromBody] PlayAudioRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId) || request.PoiId <= 0 || string.IsNullOrWhiteSpace(request.AudioText))
            {
                return BadRequest("Invalid request: ClientId, PoiId, and AudioText are required");
            }

            var result = await _audioQueueService.EnqueueAudioAsync(request.PoiId, request.ClientId, request.AudioText);

            return Ok(result);
        }

        [HttpPost("cancel-audio")]
        public async Task<IActionResult> CancelAudio([FromBody] CancelAudioRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestId) || request.PoiId <= 0)
            {
                return BadRequest("Invalid request: RequestId and PoiId are required");
            }

            await _audioQueueService.CancelAudioAsync(request.RequestId, request.PoiId);

            return Ok(new { success = true, message = "Audio cancelled" });
        }

        [HttpGet("queue-status/{poiId}")]
        public IActionResult GetQueueStatus(int poiId)
        {
            var status = _audioQueueService.GetQueueStatus(poiId);
            return Ok(status);
        }

        public class PlayAudioRequest
        {
            public string ClientId { get; set; } = string.Empty;
            public int PoiId { get; set; }
            public string AudioText { get; set; } = string.Empty;
        }

        public class CancelAudioRequest
        {
            public string RequestId { get; set; } = string.Empty;
            public int PoiId { get; set; }
        }
    }
}
