namespace Sayiad.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanManager _planManager;

    public SubscriptionPlansController(ISubscriptionPlanManager planManager)
    {
        _planManager = planManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var plans = await _planManager.GetActivePlansAsync();
        return Ok(plans);
    }

    [HttpGet("admin")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> GetAllForAdmin()
    {
        var plans = await _planManager.GetAllPlansAsync();
        return Ok(plans);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var plan = await _planManager.GetByIdAsync(id);
        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanRequest request)
    {
        var plan = await _planManager.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        var plan = await _planManager.UpdateAsync(id, request);
        return Ok(plan);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id)
    {
        await _planManager.DeleteAsync(id);
        return NoContent();
    }
}
