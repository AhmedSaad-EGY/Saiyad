using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sayiad.Domain.Dtos.ReportDtos;

namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportManager _reportManager;

    public ReportsController(IReportManager reportManager)
    {
        _reportManager = reportManager;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReportRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var report = await _reportManager.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var reports = await _reportManager.GetAllAsync();
        return Ok(reports);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var report = await _reportManager.GetByIdAsync(id);
        return Ok(report);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id, ResolveReportRequest request)
    {
        var report = await _reportManager.ResolveAsync(id, request);
        return Ok(report);
    }
}
