using ai_scans_reader.Common;
using ai_scans_reader.Models;
using Microsoft.AspNetCore.Mvc;

namespace ai_scans_reader.Controllers
{
    [ApiController]
    [Route("api/scans")]
    public class ScansController : ControllerBase
    {
        private readonly ScansContainer container;

        public ScansController(ScansContainer container)
        {
            this.container = container;
        }

        [HttpGet("completed")]
        public IActionResult GetCompleted()
        {
            var completed = container.Scans
                .Where(s => s.Status == ScanStatus.ResultReceived)
                .Select(s => new
                {
                    s.FileName,
                    s.Sha256,
                    s.AiResult
                })
                .ToList();

            return Ok(completed);
        }

        [HttpGet]
        public IActionResult GetAll([FromQuery] ScanStatus? status)
        {
            var query = container.Scans.AsEnumerable();

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);

            var result = query
                .Select(s => new
                {
                    s.FileName,
                    s.Status,
                    s.AiResult
                })
                .ToList();

            return Ok(result);
        }
    }
}