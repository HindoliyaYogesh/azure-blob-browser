using BlobBrowser.Models;
using BlobBrowser.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BlobBrowser.Controllers.Api
{
    [Route("api/blob")]
    [ApiController]
    public class BlobController : ControllerBase
    {
        private readonly IBlobBrowserService _svc;
        private readonly ILogger<BlobController> _logger;
        private const string SessionSasKey = "ContainerSasUrl";

        public BlobController(IBlobBrowserService svc, ILogger<BlobController> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        // POST api/blob/setsas  { sasUrl: "https://...?" }
        [HttpPost("setsas")]
        public IActionResult SetSas([FromBody] SetSasRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.SasUrl))
                return BadRequest(new { error = "SasUrl is required." });

            HttpContext.Session.SetString(SessionSasKey, req.SasUrl.Trim());
            return Ok(new { ok = true });
        }

        // GET api/blob/list?path=dir1/dir2/&continuationToken=...&pageSize=50&search=foo
        [HttpGet("list")]
        public async Task<IActionResult> List([FromQuery] string? path,
                                              [FromQuery] string? continuationToken,
                                              [FromQuery] int pageSize = 50,
                                              [FromQuery] string? search = null)
        {
            var sas = HttpContext.Session.GetString(SessionSasKey);
            if (string.IsNullOrEmpty(sas))
                return Unauthorized(new { error = "SAS URL not provided in session. POST to /api/blob/setsas first." });

            try
            {
                // if search specified, do recursive search; otherwise directory listing under path
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var res = await _svc.SearchAsync(sas, search!, continuationToken, pageSize);
                    return Ok(res);
                }
                else
                {
                    var res = await _svc.ListDirectoryAsync(sas, path, continuationToken, pageSize);
                    return Ok(res);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing blobs");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class SetSasRequest
    {
        public string? SasUrl { get; set; }
    }
}
