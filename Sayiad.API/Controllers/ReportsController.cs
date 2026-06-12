namespace Sayiad.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : BaseController
{
    private readonly IReportManager _reportManager;

    public ReportsController(IReportManager reportManager)
    {
        _reportManager = reportManager;
    }

    [Authorize(Roles = $"{nameof(UserRole.Customer)},{nameof(UserRole.Fisherman)},{nameof(UserRole.BaitSeller)},{nameof(UserRole.Auctioneer)}")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReportRequest request)
    {
        var userId = GetUserId();
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
