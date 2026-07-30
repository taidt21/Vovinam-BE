using Microsoft.AspNetCore.Mvc;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/time")]
public class TimeController : ControllerBase
{
    [HttpGet]
    public ActionResult<long> Get() => Ok(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}