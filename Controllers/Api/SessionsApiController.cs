using System;
using Microsoft.AspNetCore.Mvc;

namespace AutoRepairERD.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsApiController : ControllerBase
    {
        // POST: api/sessions/extend
        [HttpPost("extend")]
        public IActionResult Extend()
        {
            try
            {
                // Touch the session so server resets the IdleTimeout
                HttpContext.Session.SetInt32("LastKeepAlive", (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                return Ok(new { message = "Session extended" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Unable to extend session" });
            }
        }
    }
}
