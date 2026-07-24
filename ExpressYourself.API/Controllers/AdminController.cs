using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Features.IpRefresh.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ExpressYourself.API.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("refresh")]
    [ProducesResponseType<RefreshStoredIpsResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        RefreshStoredIpsResult result = await _sender.Send(new RefreshStoredIpsCommand(), cancellationToken);

        return Ok(result);
    }
}