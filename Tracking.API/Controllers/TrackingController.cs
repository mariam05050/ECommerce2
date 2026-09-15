using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracking.Data;
using Tracking.Services;

namespace Tracking.API.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly ITrackingService _trackingService;

    public TrackingController(ITrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetTracking(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _trackingService.GetTrackingAsync(
            userId,
            orderId,
            cancellationToken);

        return Ok(response);
    }
    [HttpPost("orders/{orderId:guid}/status/{status}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
    Guid orderId,
    string status,
    CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TrackingStatus>(
            status,
            true,
            out var trackingStatus))
        {
            return BadRequest(
                $"Invalid tracking status: {status}");
        }

        var response = await _trackingService.UpdateStatusAsync(
            orderId,
            trackingStatus,
            cancellationToken);

        return Ok(response);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException(
                "User identity is invalid.");
        }

        return userId;
    }
}