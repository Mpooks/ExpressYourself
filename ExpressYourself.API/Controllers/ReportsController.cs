using Microsoft.AspNetCore.Mvc;
using MediatR;
using ExpressYourself.Application.Features.CountryReports.Contracts;
using ExpressYourself.Application.Features.CountryReports.Queries;


namespace ExpressYourself.API.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;
    public ReportsController(ISender sender)
    {
        _sender = sender;
    }
    [HttpGet("countries")]
    [ProducesResponseType<IReadOnlyList<CountryReportDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CountryReportDto>>> GetAsync([FromQuery] string[]? codes, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCountryReportQuery(codes), cancellationToken);
        return Ok(result);
    }
}
