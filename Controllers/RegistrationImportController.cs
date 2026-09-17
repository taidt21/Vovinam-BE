using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VovinamApi.Services;

namespace VovinamApi.Controllers;

[ApiController]
[Route("api/import")]
public class RegistrationImportController : ControllerBase
{
    private readonly ExcelRegistrationImportService _service;
    public RegistrationImportController(ExcelRegistrationImportService service) => _service = service;

    [Authorize(Roles="Admin")]
    [HttpPost("registration")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Thiếu file Excel");
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _service.ImportAsync(stream));
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
