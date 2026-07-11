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

    public record SubmitRequest(double Value);

    [HttpPost("submit")]
    public IActionResult Submit([FromBody] SubmitRequest request)
    {
        _cryptoService.Submit(request.Value);
        return Ok(new { message = "Submitted", totalCount = _cryptoService.SubmissionCount });
    }

    [HttpGet("average")]
    public IActionResult GetAverage()
    {
        try
        {
            double average = _cryptoService.ComputeAverage();
            return Ok(new { average, basedOnCount = _cryptoService.SubmissionCount });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}