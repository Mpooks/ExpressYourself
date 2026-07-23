using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Features.IpRefresh.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace ExpressYourself.API.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHostEnvironment _environment;

    public AdminController(ISender sender, IHostEnvironment environment)
    {
        _sender = sender;
        _environment = environment;
    }

    [HttpPost("refresh")]
    [ProducesResponseType<RefreshStoredIpsResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        RefreshStoredIpsResult result = await _sender.Send(new RefreshStoredIpsCommand(), cancellationToken);

        return Ok(result);
    }
}