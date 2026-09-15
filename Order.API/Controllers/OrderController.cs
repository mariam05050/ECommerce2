using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Services;

namespace Order.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _orderService.CheckoutAsync(
            userId,
            cancellationToken);

        return StatusCode(201, response);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _orderService.GetMyOrdersAsync(
            userId,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _orderService.GetByIdAsync(
            userId,
            orderId,
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