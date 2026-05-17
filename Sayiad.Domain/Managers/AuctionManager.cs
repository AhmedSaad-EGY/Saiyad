using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sayiad.Data.Common;
using Sayiad.Data.Data;
using Sayiad.Domain.Contracts;
using Sayiad.Domain.Dtos.AuctionDtos;

namespace Sayiad.Domain.Managers;

public class AuctionManager : IAuctionManager
{
    private readonly IAuctionRepository _auctionRepo;
    private readonly IProductRepository _productRepo;
    private readonly INotificationManager _notificationManager;
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuctionManager> _logger;

    public AuctionManager(
        IAuctionRepository auctionRepo,
        IProductRepository productRepo,
        INotificationManager notificationManager,
        IEmailService emailService,
        IUserRepository userRepo,
        IUnitOfWork unitOfWork,
        ILogger<AuctionManager> logger)
    {
        _auctionRepo = auctionRepo;
        _productRepo = productRepo;
        _notificationManager = notificationManager;
        _emailService = emailService;
        _userRepo = userRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResult<AuctionResponse>> GetActiveAsync(AuctionFilterRequest? filter = null, PaginationRequest? pagination = null)
    {
        var f = filter ?? new AuctionFilterRequest();
        var p = pagination ?? new PaginationRequest();
        var result = await _auctionRepo.GetActiveAsync(f, p);
        return new PagedResult<AuctionResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    public async Task<AuctionDetailResponse> GetByIdAsync(int auctionId)
    {
        var auction = await _auctionRepo.GetByIdWithDetailsAsync(auctionId)
            ?? throw new KeyNotFoundException("Auction not found");

        return new AuctionDetailResponse(
            MapToResponse(auction),
            auction.Bids.OrderByDescending(b => b.Amount)
                .Select(b => new BidResponse(
                    b.Id, b.UserId, b.User!.FullName, b.Amount,
                    b.IsAutoBid, b.MaxAutoBidAmount, b.BidStatus.ToString(), b.CreatedAt))
                .ToList()
        );
    }

    public async Task<AuctionResponse> CreateAsync(int userId, CreateAuctionRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            throw new KeyNotFoundException("User not found");

        var monthlyLimit = SubscriptionManager.GetMonthlyLimit(user.SubscriptionTier);
        var monthlyCount = await _auctionRepo.GetUserMonthlyAuctionCountAsync(userId);

        if (monthlyCount >= monthlyLimit)
            throw new InvalidOperationException(
                "You have reached your monthly auction limit. Upgrade your subscription to create more auctions.");

        var product = await _productRepo.GetByIdAsync(request.ProductId)
            ?? throw new KeyNotFoundException("Product not found");

        if (product.SellerId != userId)
            throw new UnauthorizedAccessException("You can only auction your own products");

        var auction = new Auction
        {
            ProductId = request.ProductId,
            CreatedByUserId = userId,
            StartTime = DateTime.UtcNow,
            EndTime = request.EndTime,
            StartingPrice = request.StartingPrice,
            ReservePrice = request.ReservePrice,
            MinimumIncrement = request.MinimumIncrement,
            CurrentHighestBid = request.StartingPrice,
            Status = AuctionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        product.IsAuctioned = true;
        await _auctionRepo.AddAsync(auction);

        var saved = await _auctionRepo.GetByIdWithDetailsAsync(auction.Id)
            ?? throw new InvalidOperationException("Failed to load saved auction");

        _logger.LogInformation("Auction created: {AuctionId} for product {ProductId}", auction.Id, request.ProductId);
        return MapToResponse(saved);
    }

    public async Task<BidResponse> PlaceBidAsync(int auctionId, int userId, PlaceBidRequest request)
    {
        const int maxRetries = 3;
        var amount = request.Amount;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await PlaceBidInternalAsync(auctionId, userId, amount, request.MaxAutoBidAmount);
                await ResolveAutoBidsAsync(auctionId);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex,
                    "Concurrency conflict placing bid on auction {AuctionId}, attempt {Attempt}/{MaxRetries}",
                    auctionId, attempt, maxRetries);

                if (attempt == maxRetries)
                    throw new InvalidOperationException("Bid failed due to concurrent activity. Please try again.");
            }
        }

        throw new InvalidOperationException("Bid placement failed");
    }

    private async Task<BidResponse> PlaceBidInternalAsync(int auctionId, int userId, decimal amount, decimal? maxAutoBid)
    {
        var auction = await _auctionRepo.GetByIdWithBidsAsync(auctionId)
            ?? throw new KeyNotFoundException("Auction not found");

        if (auction.Status != AuctionStatus.Active)
            throw new InvalidOperationException("Auction is not active");

        if (amount < auction.CurrentHighestBid + auction.MinimumIncrement)
            throw new InvalidOperationException(
                $"Bid must be at least {auction.CurrentHighestBid + auction.MinimumIncrement:C}");

        if (auction.EndTime <= DateTime.UtcNow)
        {
            auction.Status = AuctionStatus.Finished;
            await _unitOfWork.SaveChangesAsync();
            throw new InvalidOperationException("Auction has ended");
        }

        var previousWinnerId = auction.Bids
            .Where(b => b.BidStatus == BidStatus.Winning)
            .Select(b => (int?)b.UserId)
            .FirstOrDefault();

        foreach (var prevBid in auction.Bids.Where(b => b.BidStatus == BidStatus.Winning))
        {
            prevBid.BidStatus = BidStatus.Valid;
        }

        var bid = new Bid
        {
            AuctionId = auctionId,
            UserId = userId,
            Amount = amount,
            IsAutoBid = false,
            MaxAutoBidAmount = maxAutoBid > amount ? maxAutoBid : null,
            BidStatus = BidStatus.Winning,
            CreatedAt = DateTime.UtcNow
        };

        auction.Bids.Add(bid);
        auction.CurrentHighestBid = amount;

        await _unitOfWork.SaveChangesAsync();

        if (previousWinnerId.HasValue && previousWinnerId.Value != userId)
        {
            await _notificationManager.CreateAsync(previousWinnerId.Value, "Outbid",
                $"You have been outbid on auction #{auctionId}.");
        }

        _logger.LogInformation("Bid placed: {BidAmount} on auction {AuctionId} by user {UserId}",
            amount, auctionId, userId);

        return new BidResponse(
            bid.Id, bid.UserId, string.Empty, bid.Amount,
            bid.IsAutoBid, bid.MaxAutoBidAmount, bid.BidStatus.ToString(), bid.CreatedAt);
    }

    private async Task ResolveAutoBidsAsync(int auctionId)
    {
        const int maxIterations = 20;
        for (int i = 0; i < maxIterations; i++)
        {
            var auction = await _auctionRepo.GetByIdWithBidsAsync(auctionId);
            if (auction == null || auction.Status != AuctionStatus.Active) return;

            var currentWinnerId = auction.Bids
                .Where(b => b.BidStatus == BidStatus.Winning)
                .Select(b => b.UserId)
                .FirstOrDefault();

            var bestAutoBid = auction.Bids
                .Where(b => b.UserId != currentWinnerId
                         && b.MaxAutoBidAmount > auction.CurrentHighestBid
                         && (b.BidStatus == BidStatus.Valid || b.BidStatus == BidStatus.Winning))
                .GroupBy(b => b.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    MaxBid = g.Max(b => b.MaxAutoBidAmount!.Value)
                })
                .OrderByDescending(x => x.MaxBid)
                .FirstOrDefault();

            if (bestAutoBid == null) return;

            var nextBid = Math.Min(
                bestAutoBid.MaxBid,
                auction.CurrentHighestBid + auction.MinimumIncrement);

            if (nextBid <= auction.CurrentHighestBid) return;

            foreach (var prevBid in auction.Bids.Where(b => b.BidStatus == BidStatus.Winning))
            {
                prevBid.BidStatus = BidStatus.Valid;
            }

            var autoBid = new Bid
            {
                AuctionId = auctionId,
                UserId = bestAutoBid.UserId,
                Amount = nextBid,
                IsAutoBid = true,
                MaxAutoBidAmount = bestAutoBid.MaxBid,
                BidStatus = BidStatus.Winning,
                CreatedAt = DateTime.UtcNow
            };

            auction.Bids.Add(autoBid);
            auction.CurrentHighestBid = nextBid;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Auto-bid placed: {BidAmount} on auction {AuctionId} by user {UserId}",
                nextBid, auctionId, bestAutoBid.UserId);
        }
    }

    public async Task<AuctionResponse> EndAuctionAsync(int auctionId, int userId)
    {
        var auction = await _auctionRepo.GetByIdWithDetailsAsync(auctionId)
            ?? throw new KeyNotFoundException("Auction not found");

        if (auction.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("You can only end your own auctions.");

        if (auction.Status != AuctionStatus.Active)
            throw new InvalidOperationException("Auction is already finished or cancelled");

        auction.Status = AuctionStatus.Finished;

        var winningBid = auction.Bids
            .Where(b => b.BidStatus == BidStatus.Winning)
            .MaxBy(b => b.Amount);

        if (winningBid != null && winningBid.Amount >= auction.ReservePrice)
        {
            auction.WinnerUserId = winningBid.UserId;

            if (auction.Product != null)
            {
                auction.Product.Status = ProductStatus.Sold;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        if (auction.WinnerUserId.HasValue)
        {
            await _notificationManager.CreateAsync(auction.WinnerUserId.Value, "Auction Won",
                $"You won auction #{auctionId}!");
        }

        if (auction.WinnerUserId.HasValue && auction.Winner != null)
        {
            await _emailService.SendAsync(
                auction.Winner.Email,
                "You won an auction on Sayiad!",
                $@"<p>Hello {auction.Winner.FullName},</p>
                   <p>Congratulations! You won the auction for
                   <strong>{auction.Product?.Title ?? "Item"}</strong>.</p>
                   <p>Winning bid: <strong>{winningBid?.Amount:N2} EGP</strong></p>
                   <p>The seller will be in touch shortly.</p>");
        }

        if (auction.Product != null)
        {
            var winningAmount = winningBid?.Amount ?? 0;
            await _notificationManager.CreateAsync(auction.Product.SellerId, "Auction Ended",
                $"Your auction for '{auction.Product.Title}' has ended. Winning bid: {winningAmount} EGP.");
        }

        _logger.LogInformation("Auction ended: {AuctionId}, winner: {WinnerId}",
            auctionId, auction.WinnerUserId);

        return MapToResponse(auction);
    }

    private static AuctionResponse MapToResponse(Auction auction) => new(
        auction.Id, auction.ProductId, auction.Product?.Title ?? "Deleted Product",
        auction.Product?.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
        auction.WinnerUserId, auction.Winner?.FullName,
        auction.StartTime, auction.EndTime,
        auction.StartingPrice, auction.ReservePrice,
        auction.MinimumIncrement, auction.CurrentHighestBid,
        auction.Status, auction.Bids?.Count ?? 0, auction.CreatedAt);
}
