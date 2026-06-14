namespace Sayiad.Data.Models;

public enum ReportType
{
    ProductIssue,
    AuctionIssue,
    BugReport
}

public enum ReportTargetType
{
    Product,
    Auction,
    General
}

public enum ReportStatus
{
    Pending,
    UnderReview,
    Resolved,
    Dismissed
}
