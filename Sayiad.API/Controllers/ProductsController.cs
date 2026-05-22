using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.ProductDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequestSizeLimit(5 * 1024 * 1024)]
public class ProductsController : ControllerBase
{
    private readonly IProductManager _productManager;

    public ProductsController(IProductManager productManager)
    {
        _productManager = productManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ProductFilterRequest? filter, [FromQuery] PaginationRequest? pagination)
    {
        var products = await _productManager.GetAllAsync(filter, pagination);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productManager.GetByIdAsync(id);
        return Ok(product);
    }

    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var product = await _productManager.CreateAsync(sellerId, request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request)
    {
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var product = await _productManager.UpdateAsync(id, sellerId, request);
        return Ok(product);
    }

    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _productManager.DeleteAsync(id, sellerId);
        return NoContent();
    }

    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyProducts()
    {
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var products = await _productManager.GetSellerProductsAsync(sellerId);
        return Ok(products);
    }

    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpPost("{id}/images")]
    public async Task<IActionResult> AddImage(int id, [FromBody] AddProductImageRequest request)
    {
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _productManager.AddImageAsync(id, sellerId, request);
        return Created("", result);
    }

    [Authorize(Roles = $"{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)}")]
    [HttpDelete("{id}/images/{imageId}")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _productManager.DeleteImageAsync(id, imageId, sellerId);
        return NoContent();
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("pending-review")]
    public async Task<IActionResult> GetPendingReview([FromQuery] PaginationRequest? pagination)
    {
        var products = await _productManager.GetPendingReviewAsync(pagination ?? new PaginationRequest());
        return Ok(products);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var product = await _productManager.ApproveProductAsync(id, adminId);
        return Ok(product);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectProductRequest request)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var product = await _productManager.RejectProductAsync(id, adminId, request.Reason);
        return Ok(product);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateProductStatusRequest request)
    {
        var product = await _productManager.UpdateStatusAsync(id, request.Status);
        return Ok(product);
    }
}
