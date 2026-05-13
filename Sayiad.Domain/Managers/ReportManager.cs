using Microsoft.Extensions.Logging;
using Sayiad.Domain.Dtos.ReportDtos;

namespace Sayiad.Domain.Managers;

public class ReportManager : IReportManager
{
    private readonly IReportRepository _repo;
    private readonly IProductRepository _productRepo;
    private readonly ILogger<ReportManager> _logger;

    public ReportManager(
        IReportRepository repo,
        IProductRepository productRepo,
        ILogger<ReportManager> logger)
    {
        _repo = repo;
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<ReportResponse> CreateAsync(int reporterId, CreateReportRequest request)
    {
        if (!await _productRepo.ExistsAsync(request.ProductId))
            throw new KeyNotFoundException("Product not found");

        if (await _repo.ExistsByReporterAndProductAsync(reporterId, request.ProductId))
            throw new InvalidOperationException("You already reported this product");

        var report = new Report
        {
            ReporterId = reporterId,
            ProductId = request.ProductId,
            Reason = request.Reason,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(report);

        var saved = await _repo.GetByIdAsync(report.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created report");

        _logger.LogInformation("Report created: {ReportId} for product {ProductId}", report.Id, request.ProductId);
        return MapToResponse(saved);
    }

    public async Task<IEnumerable<ReportResponse>> GetAllAsync()
    {
        var reports = await _repo.GetAllAsync();
        return reports.Select(MapToResponse);
    }

    public async Task<ReportResponse> GetByIdAsync(int reportId)
    {
        var report = await _repo.GetByIdAsync(reportId)
            ?? throw new KeyNotFoundException("Report not found");
        return MapToResponse(report);
    }

    public async Task<ReportResponse> ResolveAsync(int reportId, ResolveReportRequest request)
    {
        var report = await _repo.GetByIdAsync(reportId)
            ?? throw new KeyNotFoundException("Report not found");

        report.Status = request.Status;
        await _repo.UpdateAsync(report);

        _logger.LogInformation("Report {ReportId} resolved to {Status}", reportId, request.Status);
        return MapToResponse(report);
    }

    private static ReportResponse MapToResponse(Report report) => new(
        report.Id, report.ReporterId, report.Reporter.FullName,
        report.ProductId, report.Product.Title,
        report.Reason, report.Status, report.CreatedAt);
}