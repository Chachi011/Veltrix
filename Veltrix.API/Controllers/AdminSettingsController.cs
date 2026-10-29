using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;

namespace Veltrix.API.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = RoleNames.Admin)]
[Produces("application/json")]
public class AdminSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settingsService;

    public AdminSettingsController(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>Starting balance for new accounts and the daily bonus amount (virtual VLX).</summary>
    [HttpGet]
    [ProducesResponseType<SystemSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemSettingsDto>> Get(CancellationToken ct) =>
        Ok(await _settingsService.GetAsync(ct));

    /// <summary>Changes take effect immediately (no restart). Each changed value is written to the audit log.</summary>
    [HttpPut]
    [ProducesResponseType<SystemSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SystemSettingsDto>> Update(UpdateSystemSettingsRequest request, CancellationToken ct) =>
        Ok(await _settingsService.UpdateAsync(request, ct));
}
