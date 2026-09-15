using System.Security.Claims;
using Cart.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCart(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _cartService.GetMyCartAsync(
            userId,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("products")]
    public async Task<IActionResult> AddProduct(
        AddToCartRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var response = await _cartService.AddProductAsync(
            userId,
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("products/{productId:guid}")]
    public async Task<IActionResult> RemoveProduct(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        await _cartService.RemoveProductAsync(
            userId,
            productId,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        await _cartService.ClearCartAsync(
            userId,
            cancellationToken);

        return NoContent();
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