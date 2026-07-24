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
    private readonly IWebHostEnvironment _environment;

    public AdminController(ISender sender, IWebHostEnvironment environment)
    {
        _sender = sender;
        _environment = environment;
    }

    [HttpPost("refresh")]
    [ProducesResponseType<RefreshStoredIpsResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        if(!_environment.IsDevelopment())
    {
            return NotFound();
        }
        RefreshStoredIpsResult result = await _sender.Send(new RefreshStoredIpsCommand(), cancellationToken);

        return Ok(result);
    }
}