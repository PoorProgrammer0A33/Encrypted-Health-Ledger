using EHL.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EHL.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthStatsController : ControllerBase
{
    private readonly CryptoService _cryptoService;

    public HealthStatsController(CryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    public record SubmitRequest(string CiphertextBase64);

    [HttpPost("submit")]
    public IActionResult Submit([FromBody] SubmitRequest request)
    {
        byte[] bytes = Convert.FromBase64String(request.CiphertextBase64);
        _cryptoService.Submit(bytes);
        return Ok(new { message = "Submitted", totalCount = _cryptoService.SubmissionCount });
    }

    [HttpGet("average")]
    public IActionResult GetAverage()
    {
        try
        {
            byte[] resultBytes = _cryptoService.ComputeEncryptedAverage();
            string base64 = Convert.ToBase64String(resultBytes);
            return Ok(new { encryptedAverageBase64 = base64, basedOnCount = _cryptoService.SubmissionCount });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}