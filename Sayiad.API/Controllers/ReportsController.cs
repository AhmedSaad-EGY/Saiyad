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

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SubmitReport(SubmitReportRequest request)
    {
        var userId = GetUserId();
        var report = await _reportManager.SubmitReportAsync(userId, request);
        return CreatedAtAction(nameof(GetAll), new { id = report.Id }, report);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ReportStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var reports = await _reportManager.GetAllAsync(status, page, pageSize);
        return Ok(reports);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPatch("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id, ResolveReportRequest request)
    {
        var report = await _reportManager.ResolveAsync(id, request);
        return Ok(report);
    }
}
