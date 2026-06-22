using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sayiad.Data.Common;
using Sayiad.Data.Data;
using Sayiad.Domain.Constants;
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
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuctionManager> _logger;
    private readonly IWalletManager _walletManager;

    public AuctionManager(
        IAuctionRepository auctionRepo,
        IProductRepository productRepo,
        INotificationManager notificationManager,
        IEmailService emailService,
        IUserRepository userRepo,
        ISubscriptionPlanRepository planRepo,
        IUnitOfWork unitOfWork,
        ILogger<AuctionManager> logger,
        IWalletManager walletManager)
    {
        _auctionRepo = auctionRepo;
        _productRepo = productRepo;
        _notificationManager = notificationManager;
        _emailService = emailService;
        _userRepo = userRepo;
        _planRepo = planRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _walletManager = walletManager;
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
                    b.Id, b.AuctionId, b.UserId, b.User!.FullName, b.Amount,
                    b.IsAutoBid, b.MaxAutoBidAmount, b.BidStatus.ToString(), b.CreatedAt))
                .ToList()
        );
    }

    public async Task<AuctionResponse> CreateAsync(int userId, CreateAuctionRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user is null)
            throw new KeyNotFoundException("User not found");

        var plan = await _planRepo.GetByTierAsync(user.SubscriptionTier);
        var monthlyLimit = plan?.MaxAuctionsPerMonth ?? 3;
        var monthlyCount = await _auctionRepo.GetUserMonthlyAuctionCountAsync(userId);

        if (monthlyCount >= monthlyLimit)
            throw new InvalidOperationException(
                "You have reached your monthly auction limit. Upgrade your subscription to create more auctions.");

        var product = await _productRepo.GetByIdAsync(request.ProductId)
            ?? throw new KeyNotFoundException("Product not found");


        var startTime = request.StartTime ?? DateTime.UtcNow;
        var isScheduled = startTime > DateTime.UtcNow.AddMinutes(1);

        var auction = new Auction
        {
            ProductId = request.ProductId,
            CreatedByUserId = userId,
            StartTime = startTime,
            EndTime = request.EndTime,
            StartingPrice = request.StartingPrice,
            ReservePrice = request.ReservePrice,
            BidIncrement = request.BidIncrement,
            CurrentHighestBid = request.StartingPrice,
            Status = isScheduled ? AuctionStatus.Scheduled : AuctionStatus.Active,
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
        var bidUser = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        var bidPlan = await _planRepo.GetByTierAsync(bidUser.SubscriptionTier);
        var bidLimit = bidPlan?.MaxBidsPerMonth ?? 3;
        var bidCount = await _auctionRepo.GetUserMonthlyBidCountAsync(userId);
        if (bidCount >= bidLimit)
            throw new InvalidOperationException(
                "You have reached your monthly bid limit. Upgrade your subscription to place more bids.");

        // Auto-bidding requires Basic subscription or higher
        if (request.MaxAutoBidAmount.HasValue && request.MaxAutoBidAmount > 0)
        {
            if (bidUser.SubscriptionTier == SubscriptionTier.Free)
                throw new InvalidOperationException(
                    "Auto-bidding requires a Basic subscription or higher. Upgrade your plan to use this feature.");
        }

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

        if (amount < auction.CurrentHighestBid + auction.BidIncrement)
            throw new InvalidOperationException(
                $"Bid must be at least {auction.CurrentHighestBid + auction.BidIncrement:C}");

        if (auction.EndTime <= DateTime.UtcNow)
        {
            auction.Status = AuctionStatus.Finished;
            await _unitOfWork.SaveChangesAsync();
            throw new InvalidOperationException("Auction has ended");
        }

        var previousWinningBid = auction.Bids
            .FirstOrDefault(b => b.BidStatus == BidStatus.Winning);
        if (previousWinningBid?.UserId == userId)
            throw new InvalidOperationException("You are already the highest bidder on this auction.");

        if (!await _walletManager.HasSufficientBalanceAsync(userId, amount))
            throw new InvalidOperationException("Insufficient balance. Please deposit funds to your wallet.");

        var previousWinnerId = previousWinningBid?.UserId;
        var previousWinnerAmount = previousWinningBid?.Amount;

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
            await _walletManager.ReleaseHeldFundsAsync(
                previousWinnerId.Value, previousWinnerAmount!.Value, "Auction", auctionId);
        }
        await _walletManager.HoldFundsAsync(userId, amount, "Auction", auctionId);

        if (previousWinnerId.HasValue && previousWinnerId.Value != userId)
        {
            await _notificationManager.CreateAsync(previousWinnerId.Value, "Outbid",
                $"You have been outbid on auction #{auctionId}.");
        }

        _logger.LogInformation("Bid placed: {BidAmount} on auction {AuctionId} by user {UserId}",
            amount, auctionId, userId);

        return new BidResponse(
            bid.Id, auctionId, bid.UserId, string.Empty, bid.Amount,
            bid.IsAutoBid, bid.MaxAutoBidAmount, bid.BidStatus.ToString(), bid.CreatedAt);
    }

    private async Task ResolveAutoBidsAsync(int auctionId)
    {
        var auction = await _auctionRepo.GetByIdWithBidsAsync(auctionId);
        if (auction == null || auction.Status != AuctionStatus.Active) return;

        const int maxIterations = 20;
        var changed = false;

        for (int i = 0; i < maxIterations; i++)
        {
            var currentWinningBid = auction.Bids
                .FirstOrDefault(b => b.BidStatus == BidStatus.Winning);
            var currentWinnerId = currentWinningBid?.UserId;
            var currentWinnerAmount = currentWinningBid?.Amount;

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

            if (bestAutoBid == null) break;

            var nextBid = Math.Min(
                bestAutoBid.MaxBid,
                auction.CurrentHighestBid + auction.BidIncrement);

            if (nextBid <= auction.CurrentHighestBid) break;

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
            changed = true;

            if (currentWinnerId.HasValue && currentWinnerId.Value != bestAutoBid.UserId)
            {
                await _walletManager.ReleaseHeldFundsAsync(
                    currentWinnerId.Value, currentWinnerAmount!.Value, "Auction", auctionId);
            }
            await _walletManager.HoldFundsAsync(bestAutoBid.UserId, nextBid, "Auction", auctionId);

            _logger.LogInformation("Auto-bid placed: {BidAmount} on auction {AuctionId} by user {UserId}",
                nextBid, auctionId, bestAutoBid.UserId);
        }

        if (changed)
            await _unitOfWork.SaveChangesAsync();
    }

    public async Task<AuctionResponse> EndAuctionAsync(int auctionId, int userId)
    {
        var auction = await _auctionRepo.GetByIdWithDetailsAsync(auctionId)
            ?? throw new KeyNotFoundException("Auction not found");

        var user = await _userRepo.GetByIdAsync(userId);
        var isAdmin = user?.Role == UserRole.Admin;
        if (!isAdmin && auction.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("You can only end your own auctions.");

        if (auction.Status != AuctionStatus.Active)
            throw new InvalidOperationException("Auction is already finished or cancelled");

        var winningBid = auction.Bids
            .Where(b => b.BidStatus == BidStatus.Winning)
            .MaxBy(b => b.Amount);

        if (winningBid != null && auction.ReservePrice > 0 && winningBid.Amount < auction.ReservePrice)
        {
            auction.Status = AuctionStatus.PendingSellerConfirmation;
            auction.ConfirmationDeadline = DateTime.UtcNow.AddHours(24);
            await _unitOfWork.SaveChangesAsync();

            if (auction.Product != null)
                await _notificationManager.CreateAsync(
                    auction.Product.SellerId,
                    "Reserve Price Not Met",
                    $"Your auction #{auction.Id} ended with a highest bid of {winningBid.Amount:N2} EGP, " +
                    $"which is below your reserve price of {auction.ReservePrice:N2} EGP. " +
                    "Do you accept this bid? You have 24 hours to respond. " +
                    "If no response, the auction will be cancelled.");

            _logger.LogInformation(
                "Auction {AuctionId} awaiting seller reserve confirmation until {Deadline}",
                auction.Id, auction.ConfirmationDeadline);

            return MapToResponse(auction);
        }

        auction.Status = AuctionStatus.Finished;

        if (winningBid != null)
            await SettleAuctionAsync(auction, winningBid);
        else
            await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Auction ended: {AuctionId}, winner: {WinnerId}",
            auctionId, auction.WinnerUserId);

        return MapToResponse(auction);
    }

    public async Task ConfirmReservePriceBidAsync(int auctionId, bool accept, int fishermanId)
    {
        var auction = await _auctionRepo.GetByIdWithDetailsAsync(auctionId)
            ?? throw new KeyNotFoundException("Auction not found");

        if (auction.Product?.SellerId != fishermanId)
            throw new UnauthorizedAccessException("Only the auction seller can confirm the bid");

        if (auction.Status != AuctionStatus.PendingSellerConfirmation)
            throw new InvalidOperationException("Auction is not awaiting seller confirmation");

        var winningBid = auction.Bids
            .Where(b => b.BidStatus == BidStatus.Winning)
            .MaxBy(b => b.Amount)
            ?? throw new InvalidOperationException("Auction has no winning bid to confirm");

        if (accept)
        {
            auction.Status = AuctionStatus.Finished;
            await SettleAuctionAsync(auction, winningBid);

            await _notificationManager.CreateAsync(
                winningBid.UserId,
                "Seller Accepted Your Bid",
                $"The seller accepted your bid for auction #{auction.Id}.");
        }
        else
        {
            auction.Status = AuctionStatus.Cancelled;
            auction.ConfirmationDeadline = null;
            await _unitOfWork.SaveChangesAsync();

            await ReleaseActiveBidHoldsAsync(auction);
            await NotifyAllBiddersAuctionCancelledAsync(auction);
        }
    }

    private async Task SettleAuctionAsync(Auction auction, Bid winningBid)
    {
        auction.WinnerUserId = winningBid.UserId;
        auction.ConfirmationDeadline = null;

        if (auction.Product != null)
            auction.Product.Status = ProductStatus.Sold;

        await _unitOfWork.SaveChangesAsync();

        if (auction.Product != null)
        {
            await _walletManager.SettleAuctionPaymentAsync(
                winningBid.UserId, auction.Product.SellerId, winningBid.Amount, auction.Id, auction.CreatedByUserId);

            var auctioneer = await _userRepo.GetByIdAsync(auction.CreatedByUserId);
            if (auctioneer != null)
            {
                var fee = winningBid.Amount * FinancialConstants.AuctionAuctioneerFee;
                await _walletManager.CreditPlatformFeeAsync(auctioneer.Id, fee, "Auction", auction.Id);
            }
        }

        await _notificationManager.CreateAsync(winningBid.UserId, "Auction Won",
            $"You won auction #{auction.Id}!");

        if (auction.Winner != null)
        {
            await _emailService.SendAsync(
                auction.Winner.Email,
                "You won an auction on Sayiad!",
                $@"<p>Hello {auction.Winner.FullName},</p>
                   <p>Congratulations! You won the auction for
                   <strong>{auction.Product?.Title ?? "Item"}</strong>.</p>
                   <p>Winning bid: <strong>{winningBid.Amount:N2} EGP</strong></p>
                   <p>Payment of {winningBid.Amount:N2} EGP has been deducted from your wallet."
                   + (auction.Product != null
                       ? $" The seller will receive {FinancialConstants.AuctionFishermanShare:P1} ({winningBid.Amount * FinancialConstants.AuctionFishermanShare:N2} EGP).</p>"
                       : "</p>"));
        }

        if (auction.Product != null)
        {
            var sellerAmount = winningBid.Amount * FinancialConstants.AuctionFishermanShare;
            await _notificationManager.CreateAsync(auction.Product.SellerId, "Auction Ended",
                $"Your auction for '{auction.Product.Title}' has ended. Winning bid: {winningBid.Amount} EGP. "
                + $"You received {sellerAmount:N2} EGP ({FinancialConstants.AuctionFishermanShare:P1} after platform and auctioneer fees).");
        }
    }

    private async Task ReleaseActiveBidHoldsAsync(Auction auction)
    {
        var heldBids = auction.Bids
            .Where(b => b.BidStatus == BidStatus.Winning)
            .GroupBy(b => b.UserId)
            .Select(g => new { UserId = g.Key, Amount = g.Max(b => b.Amount) });

        foreach (var bid in heldBids)
            await _walletManager.ReleaseHeldFundsAsync(bid.UserId, bid.Amount, "Auction", auction.Id);
    }

    private async Task NotifyAllBiddersAuctionCancelledAsync(Auction auction)
    {
        foreach (var bidderId in auction.Bids.Select(b => b.UserId).Distinct())
            await _notificationManager.CreateAsync(
                bidderId,
                "Auction Cancelled",
                $"Auction #{auction.Id} was cancelled and any held funds have been released.");
    }

    // ── Auction request system ──────────────────────────────────────

    private static AuctionRequestResponse MapRequestToResponse(AuctionRequest r) =>
        new(r.Id, r.FishermanId, r.Fisherman?.FullName ?? "",
            r.ProductTitle, r.ProductDescription, r.ProductImageUrl,
            r.EstimatedValue, r.QuantityKg, r.FishType, r.CatchLocation,
            r.CatchDate, r.Status.ToString(), r.RejectionReason,
            r.ResultingAuctionId, r.CreatedAt);

    public async Task<AuctionRequestResponse> SubmitRequestAsync(
        int fishermanId, SubmitAuctionRequestRequest request)
    {
        var fisherman = await _userRepo.GetByIdAsync(fishermanId)
            ?? throw new KeyNotFoundException("User not found");

        if (fisherman.Role != UserRole.Fisherman)
            throw new UnauthorizedAccessException("Only Fishermen can submit auction requests.");

        var requestPlan = await _planRepo.GetByTierAsync(fisherman.SubscriptionTier);
        var requestLimit = requestPlan?.MaxAuctionRequestsPerMonth ?? 3;
        var requestCount = await _auctionRepo.GetUserMonthlyRequestCountAsync(fishermanId);
        if (requestCount >= requestLimit)
            throw new InvalidOperationException(
                "You have reached your monthly auction request limit. Upgrade your subscription to submit more requests.");

        var auctionRequest = new AuctionRequest
        {
            FishermanId = fishermanId,
            ProductTitle = request.ProductTitle,
            ProductDescription = request.ProductDescription,
            ProductImageUrl = request.ProductImageUrl,
            EstimatedValue = request.EstimatedValue,
            QuantityKg = request.QuantityKg,
            FishType = request.FishType,
            CatchLocation = request.CatchLocation,
            CatchDate = request.CatchDate ?? DateTime.UtcNow,
            Status = AuctionRequestStatus.Pending
        };

        var created = await _auctionRepo.CreateRequestAsync(auctionRequest);

        _logger.LogInformation("Auction request {Id} submitted by fisherman {FishermanId}",
            created.Id, fishermanId);

        var auctioneers = await _userRepo.GetUsersByRoleAsync(UserRole.Auctioneer);
        if (auctioneers?.Any() == true)
        {
            foreach (var auctioneer in auctioneers)
            {
                await _notificationManager.CreateAsync(
                    auctioneer.Id,
                    "New Auction Request",
                    $"Fisherman '{fisherman.FullName}' submitted a new auction request for '{created.ProductTitle}'.");
            }
        }

        return MapRequestToResponse(created);
    }

    public async Task<PagedResult<AuctionRequestResponse>> GetMyRequestsAsync(
        int fishermanId, PaginationRequest pagination)
    {
        var result = await _auctionRepo.GetFishermanRequestsAsync(fishermanId, pagination);
        return new PagedResult<AuctionRequestResponse>
        {
            Items = result.Items.Select(MapRequestToResponse).ToList(),
            TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
        };
    }

    public async Task<PagedResult<AuctionRequestResponse>> GetPendingRequestsAsync(
        PaginationRequest pagination)
    {
        var result = await _auctionRepo.GetPendingRequestsAsync(pagination);
        return new PagedResult<AuctionRequestResponse>
        {
            Items = result.Items.Select(MapRequestToResponse).ToList(),
            TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize
        };
    }

    public async Task<AuctionResponse> ApproveRequestAsync(
        int auctionRequestId, int auctioneerId, ApproveAuctionRequestRequest request)
    {
        var ownsTransaction = _unitOfWork.CurrentTransaction == null;
        var transaction = ownsTransaction
            ? await _unitOfWork.BeginTransactionAsync()
            : _unitOfWork.CurrentTransaction!;

        try
        {
            var auctionRequest = await _auctionRepo.GetRequestByIdAsync(auctionRequestId)
                ?? throw new KeyNotFoundException("Auction request not found");

            if (auctionRequest.Status != AuctionRequestStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be approved.");

            var auctioneer = await _userRepo.GetByIdAsync(auctioneerId)
                ?? throw new KeyNotFoundException("Auctioneer not found");

            var auctioneerPlan = await _planRepo.GetByTierAsync(auctioneer.SubscriptionTier);
            var auctioneerLimit = auctioneerPlan?.MaxAuctionsPerMonth ?? 3;
            var monthlyCount = await _auctionRepo.GetUserMonthlyAuctionCountAsync(auctioneerId);
            if (monthlyCount >= auctioneerLimit)
                throw new InvalidOperationException("Monthly auction limit reached. Upgrade subscription.");

            var product = new Product
            {
                Title = auctionRequest.ProductTitle,
                Description = auctionRequest.ProductDescription,
                SellerId = auctionRequest.FishermanId,
                Price = request.StartingPrice,
                StockQuantity = 1,
                Status = ProductStatus.Available,
                CategoryId = request.CategoryId,
                Condition = ProductCondition.New,
                Brand = "",
                Location = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            if (auctionRequest.ProductImageUrl != null)
                product.Images.Add(new ProductImage { ImageUrl = auctionRequest.ProductImageUrl, IsPrimary = true });

            await _productRepo.AddAsync(product);

            var createRequest = new CreateAuctionRequest(
                product.Id, request.EndTime, request.StartingPrice,
                request.ReservePrice, request.BidIncrement);

            var auction = await CreateAsync(auctioneerId, createRequest);

            auctionRequest.Status = AuctionRequestStatus.Approved;
            auctionRequest.ReviewedByAuctioneerId = auctioneerId;
            auctionRequest.ResultingAuctionId = auction.Id;
            await _auctionRepo.UpdateRequestAsync(auctionRequest);

            await _notificationManager.CreateAsync(
                auctionRequest.FishermanId,
                "Auction Request Approved",
                $"Your auction request for '{auctionRequest.ProductTitle}' was approved! Auction #{auction.Id} is now live.");

            if (ownsTransaction)
                await transaction.CommitAsync();

            _logger.LogInformation("Auction request {RequestId} approved — auction {AuctionId} created",
                auctionRequestId, auction.Id);

            return auction;
        }
        catch
        {
            if (ownsTransaction)
                await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (ownsTransaction)
                await transaction.DisposeAsync();
        }
    }

    public async Task<AuctionRequestResponse> RejectRequestAsync(
        int auctionRequestId, int auctioneerId, RejectAuctionRequestRequest request)
    {
        var auctionRequest = await _auctionRepo.GetRequestByIdAsync(auctionRequestId)
            ?? throw new KeyNotFoundException("Auction request not found");

        if (auctionRequest.Status != AuctionRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be rejected.");

        auctionRequest.Status = AuctionRequestStatus.Rejected;
        auctionRequest.ReviewedByAuctioneerId = auctioneerId;
        auctionRequest.RejectionReason = request.Reason;
        await _auctionRepo.UpdateRequestAsync(auctionRequest);

        await _notificationManager.CreateAsync(
            auctionRequest.FishermanId,
            "Auction Request Rejected",
            $"Your auction request for '{auctionRequest.ProductTitle}' was rejected. Reason: {request.Reason}");

        _logger.LogInformation("Auction request {RequestId} rejected by auctioneer {AuctioneerId}",
            auctionRequestId, auctioneerId);

        return MapRequestToResponse(auctionRequest);
    }

    // ── Auctioneer analytics ────────────────────────────────────────

    public async Task<AuctioneerDashboardResponse> GetAuctioneerDashboardAsync(int auctioneerId)
    {
        var dashboardStats = await _auctionRepo.GetDashboardStatsAsync(auctioneerId);
        var requestCounts = await _auctionRepo.GetRequestCountsByStatusAsync();

        var avgBids = dashboardStats.Total > 0
            ? (double)dashboardStats.TotalBids / dashboardStats.Total
            : 0;

        return new AuctioneerDashboardResponse(
            dashboardStats.Total, dashboardStats.Active, dashboardStats.Finished,
            requestCounts.Pending, requestCounts.Approved, requestCounts.Rejected,
            dashboardStats.TotalBidValue, dashboardStats.TotalBids, Math.Round(avgBids, 1));
    }

    private static AuctionResponse MapToResponse(Auction auction) => new(
        auction.Id, auction.ProductId, auction.Product?.Title ?? "Deleted Product",
        auction.Product?.Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
        auction.WinnerUserId, auction.Winner?.FullName,
        auction.StartTime, auction.EndTime,
        auction.StartingPrice, auction.ReservePrice,
        auction.BidIncrement, auction.CurrentHighestBid,
        auction.Status, auction.Bids?.Count ?? 0, auction.CreatedAt,
        auction.Product?.SellerId, auction.Product?.Seller?.FullName,
        auction.CreatedByUserId, auction.CreatedByUser?.FullName,
        auction.ConfirmationDeadline);
}
