using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Features.IpInformation.Queries;
using Microsoft.AspNetCore.Mvc;
using MediatR;


namespace ExpressYourself.API.Controllers;

[ApiController]
[Route("api/ips")]
public sealed class IpController : ControllerBase
{
    private readonly ISender _sender;
    public IpController(ISender sender)
    {
        _sender = sender;
    }
    [HttpGet("{address}")]
    public async Task<ActionResult<IpInformationDto>> GetAsync(string address, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetIpInformationQuery(address), cancellationToken);
        return Ok(result);
    }
}
