using ExpressYourself.API.Configuration;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Features.IpInformation.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;


namespace ExpressYourself.API.Controllers;

[ApiController]
[Route("api/ips")]
[EnableRateLimiting(RateLimitingOptions.IpLookupPolicy)]
public sealed class IpController : ControllerBase
{
    private readonly ISender _sender;
    public IpController(ISender sender)
    {
        _sender = sender;
    }
    [HttpGet("{address}")]
    [ProducesResponseType<IpInformationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>  (StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>  (StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>  (StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>  (StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IpInformationDto>> GetAsync([FromRoute][System.ComponentModel.DataAnnotations.StringLength(15)]
    string address,CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetIpInformationQuery(address), cancellationToken);
        return Ok(result);
    }
}
