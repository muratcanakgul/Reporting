using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ReportingAPI.Models;
using ReportingAPI.Services;

namespace ReportingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("pong");
        }

        // POST: api/report/filtered
        [HttpPost("filtered")]
        public async Task<IActionResult> GetFilteredReport([FromBody] FilteredReportRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is null!" });

            try
            {
                var result = await _reportService.GetFilteredReportAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Hata detaylarını loglayabilirsin
                return StatusCode(500, new { error = "An error occurred while generating the report.", details = ex.Message });
            }
        }
    }
}
