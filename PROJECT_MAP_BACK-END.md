# Sayiad — Backend Project Map

**Date:** 2026-05-22 | **.NET 10 | EF Core 10 | SQL Server**

---

## Latest Session: Revenue Flow Corrections Applied (May 22, 2026)

### ✅ Auction 5% fee → Auctioneer (corrected)
- **AuctionManager.EndAuctionAsync** — replaced `_userRepo.GetByEmailAsync("sayiadapp@gmail.com")` with `_userRepo.GetByIdAsync(auction.CreatedByUserId)`
- **AuctionExpiryService** — same change on auto-close
- Notification messages updated: "95% after 5% auctioneer fee"

### ✅ E-commerce 5% platform fee (newly wired)
- **PaymentManager.ConfirmAsync** — after order is marked `Paid`, for each `OrderItem` group by seller:
  1. `DeductForOrderAsync(buyerId, totalPrice, orderId)` — deduct buyer's wallet
  2. `CreditSellerAsync(sellerId, sellerTotal * 0.95, orderId)` — credit seller 95%
  3. `CreditPlatformFeeAsync(adminId, sellerTotal * 0.05, "Order", orderId)` — credit admin 5%
- Injected `IWalletManager` + `IUserRepository` into `PaymentManager`
- Previously dead methods `DeductForOrderAsync` and `CreditSellerAsync` now in use

### ✅ Subscription payment deducted from wallet
- **SubscriptionManager** — deducts plan price via `DeductForSubscriptionAsync`, credits admin via `CreditPlatformFeeAsync`
- Free plan (price = 0) skipped

### ✅ Subscription prices USD→EGP (×50)
- Migration `UpdateSubscriptionPricesToEGP`

### ✅ CreditPlatformFeeAsync (generic method)
- Accepts any `platformUserId` — used for auctioneer fees, admin fees, subscription fees

**Build:** 0 errors | **Tests:** 22/23 (same pre-existing)

## Previous: Wave 5 — Product Review Flow (May 22, 2026)

### New products start as PendingReview, Admin approve/reject
- **ProductStatus.PendingReview** — new enum value added
- **Product entity** — added `ReviewedByUserId`, `ReviewedAt`, `RejectionReason` fields
- **Migration** `AddProductReview` — adds columns to Products table
- **ProductManager.CreateAsync** — new products now created with `Status = PendingReview`
- **ProductRepository** — `GetPendingReviewAsync` returns paginated pending products; public listing (`GetAllAsync`) already filters `Status == Available`, so pending products are invisible to customers
- **ProductManager** — `ApproveProductAsync` (sets Available, records reviewer), `RejectProductAsync` (sets Rejected, stores reason)
- **ProductsController** — 3 new admin endpoints: `GET /api/products/pending-review` (paginated), `PATCH /api/products/{id}/approve`, `PATCH /api/products/{id}/reject` (body: `{ reason }`)
- **ProductResponse DTO** — added `ReviewedByUserId`, `ReviewedAt`, `RejectionReason` fields
- **Build:** 0 errors | **Tests:** 22/23 (same pre-existing)

### Product Status Values
| Status | Description | Visible to customers |
|--------|-------------|---------------------|
| `PendingReview` | Created but not yet approved by admin | No |
| `Available` | Approved and visible | Yes |
| `Rejected` | Rejected with reason | No |
| `Sold` | Purchased | No (filtered) |
| `Draft` | Not yet published | No |
| `Suspended` | Admin-suspended | No |

## Previous: Wave 4 — Bid-Wallet Integration (May 22, 2026)

### Wallet holds funds on bid placement, releases on outbid/end
- **AuctionManager** — `IWalletManager` injected; `PlaceBidInternalAsync` now checks `HasSufficientBalanceAsync` before bidding, then `HoldFundsAsync` after save. Outbid: `ReleaseHeldFundsAsync` for previous winner.
- **ResolveAutoBidsAsync** — Each auto-bid round releases the previous winner's held funds and holds the auto-bidder's funds.
- **EndAuctionAsync** — When reserve not met (`WinnerUserId == null`), `ReleaseHeldFundsAsync` for the last winning bidder.
- **AuctionExpiryService** — Same reserve-not-met release logic for auto-expired auctions.
- **Build:** 0 errors | **Tests:** 22/23 (same pre-existing)

### Admin — E-commerce features removed (10 endpoints)
- Removed Admin from `OrdersController`, `CartController`, `WishlistController`, `ShippingAddressesController`, `PaymentsController` → class-level `[Authorize]` changed to `[Authorize(Roles = "Customer,Fisherman,BaitSeller")]`
- Removed Admin from `ReviewsController.Create`/`Delete`, `ReportsController.Create` → method-level roles narrowed
- Removed Admin from `SubscriptionsController.Upgrade`/`GetMySubscription` → method-level roles added
- Removed Admin from `AuctionsController.PlaceBid` → now Customer-only

### Admin — Auction features gained (4 endpoints)
- Added Admin to `AuctionsController.Create` (start auctions)
- Added Admin to `AuctionsController.GetPendingRequests`, `ApproveRequest`, `RejectRequest` (review auction requests)
- Admin already had: `EndAuction`, `GetAuctioneerDashboard`

### Auctioneer — All e-commerce + product features removed (12 endpoints)
- Removed Auctioneer from `OrdersController`, `CartController`, `WishlistController`, `ShippingAddressesController`, `PaymentsController` (class-level roles narrowed)
- Removed Auctioneer from `ReviewsController.Create`/`Delete`, `ReportsController.Create`
- Removed Auctioneer from all 6 product endpoints in `ProductsController` (Create, Update, Delete, GetMyProducts, AddImage, DeleteImage) — changed from `Fisherman,BaitSeller,Auctioneer` to `Fisherman,BaitSeller`

### Frontend — Permission gates updated
- `SELLER_ROLES` constant: removed Auctioneer (now only Fisherman, BaitSeller)
- Navbar `data-roles`: My Orders/Wishlist → `Customer,Fisherman,BaitSeller`; My Products → `Fisherman,BaitSeller`; Subscriptions → `Customer,Fisherman,BaitSeller,Auctioneer`
- Route guards: cart/checkout/shipping/order-detail → require e-commerce roles; auction-requests-review/auctioneer-analytics → also allow Admin
- Dashboard tabs: orders/wishlist hidden for Admin/Auctioneer; products hidden for Auctioneer; review + analytics tabs visible to Admin too
- Profile quick links: orders/wishlist/shipping hidden for Admin/Auctioneer; My Products hidden for Auctioneer
- Home role links: Seller products link hidden for Auctioneer
- Seller profile page: Auctioneer excluded from seller profile access
- Product detail: "Start Auction" button no longer shown for Auctioneer

**Build:** 0 errors | **Tests:** 22/23 passing (1 pre-existing ForgotPassword failure)

---

## Previous: Critical bugfixes — C1–C4 from deploy verification (May 20, 2026)

- **C1 — RoleClaimType fix** — Removed `RoleClaimType = "role"` from `Program.cs`. Default `ClaimTypes.Role` matches JWT correctly. Previous fix used wrong claim type short name, causing 403 on all `[Authorize(Roles)]` endpoints.
- **C2 — Nested transaction leak** — Removed nested `BeginTransactionAsync()` from `OrderRepository.CreateOrderTransactionAsync`; transaction ownership stays in `OrderManager`. Also removed duplicate stock deduction in `OrderManager.CreateFromCartAsync` (repo already handles it).
- **C3 — Shipping route mismatch** — Fixed 5 frontend API paths in `checkout.js`/`shipping.js` from `/shipping-addresses` (404) to `/shippingaddresses` (200).
- **C4 — Subscription data leak** — `SubscriptionsController.GetAll` changed from `[Authorize]` to `[Authorize(Roles = "Admin")]`.
- **Build:** 0 errors | **Tests:** 22/23 passing (1 pre-existing ForgotPassword failure)

## Previous: Phase 2 — Enums & admin moderation (May 20)
- `JsonStringEnumConverter` added globally in `Program.cs` → API returns enums as strings
- `Rejected`, `Suspended` added to `ProductStatus` enum
- `PATCH /api/products/{id}/status` endpoint (Admin-only) with `UpdateProductStatusRequest` DTO
- Public product listing filter: `Status == Available` (hides Draft/Rejected/Suspended)
- `IProductManager.UpdateStatusAsync` / `ProductManager.UpdateStatusAsync`

## Previous: Phase 3 — Payment/Subscription hardening (May 20)
- `PaymentReference` uniqueness: app check + DB unique filtered index `WHERE PaymentReference IS NOT NULL`
- `PaymentStatus` enum (Pending/Confirmed/Failed/Cancelled) with EF value converter: `Confirmed ↔ "Paid"` (backward compat)
- `PaymentReference` in `SubscriptionResponse` DTO
- Migration: `Phase3_PaymentSubscriptionHardening`

## Previous: Phase 1 — Feature work (May 20)
- Order cancellation (`Cancelled` enum + `CancelAsync` + stock restoration + notification)
- Fisherman license number (entity + FluentValidation + migration)
- Product filters: `InStock`, `SortBy`, `SortDirection` on `ProductFilterRequest`
- Auction status filter (defaults Active when null)
- Auctioneer notification on Fisherman request
- Build 0 errors, 21/22 tests passing

---

## TECH STACK

| Component | Version |
|-----------|---------|
| .NET | 10.0 (preview) |
| ASP.NET Core | 10.0.7 |
| EF Core | 10.0.7 |
| SQL Server | LocalDB (dev) / SQL Server (prod) |
| Serilog | 10.0.0 |
| FluentValidation | 12.1.1 |
| BCrypt.Net-Next | 4.2.0 |
| JWT Bearer | 10.0.7 |
| Swagger/Swashbuckle | 10.1.7 |
| SignalR | Built-in |
| Cloudinary | File storage |
| SMTP | Email (Mailtrap dev / SendGrid prod) |
| xUnit | Testing |
| Moq | Mocking |

---

## ARCHITECTURE

**Pattern:** 3-Layer API (Api → Domain ← Data) + separate Vanilla JS frontend

```
Sayiad.API/          # Presentation — thin controllers, middleware, hubs
Sayiad.Domain/       # Business Logic — managers, validators, DTOs, contracts, enums
Sayiad.Data/         # Data Access — EF Core DbContext, repos, migrations
Sayiad.Tests/        # xUnit — unit + integration tests
```

**Dependency rules:**
- **API** → references Domain + Data (DI registration, middleware)
- **Domain** → references NO project (pure models, enums, interfaces)
- **Data** → references Domain (implements repo interfaces)
- No circular dependencies

**Flow:**
```
Controller → Manager (business logic, auth checks, transactions)
                ↓
         Repository interface (in Domain.Contracts)
                ↓
         Repository impl (in Data) → EF Core → SQL Server
```

---

## PROJECT STRUCTURE

```
Back-end/
├── .github/workflows/ci.yml         # GitHub Actions — build + test on push to main
├── PROJECT_MAP_BACK-END.md           # This file
├── README.md
├── SQL/Migrations/                   # Raw SQL migration scripts (if any)
│
├── Sayiad.API/                       # ASP.NET Core Web API
│   ├── Program.cs                    # DI, middleware, auth, CORS, SignalR, health checks
│   ├── GlobalUsings.cs
│   ├── Sayiad.Api.csproj
│   ├── Sayiad.Api.slnx
│   ├── Sayiad.Api.http              # HTTP file for testing endpoints
│   ├── appsettings.json             # Base config (placeholders for secrets)
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json  # #{TOKEN}# placeholders
│   ├── Properties/
│   │   ├── launchSettings.json
│   │   └── PublishProfiles/         # Web Deploy to sayiad.runasp.net
│   ├── Controllers/                 # 18 controllers
│   │   ├── AuctionsController.cs
│   │   ├── AuthController.cs
│   │   ├── CartController.cs
│   │   ├── CategoriesController.cs
│   │   ├── NotificationsController.cs
│   │   ├── OrdersController.cs
│   │   ├── PaymentsController.cs
│   │   ├── ProductsController.cs
│   │   ├── ReportsController.cs
│   │   ├── ReviewsController.cs
│   │   ├── SellerProfileController.cs
│   │   ├── ShippingAddressesController.cs
│   │   ├── SubscriptionPlansController.cs
│   │   ├── SubscriptionsController.cs
│   │   ├── UploadController.cs
│   │   ├── UsersController.cs
│   │   ├── WalletController.cs
│   │   └── WishlistController.cs
│   ├── Middleware/
│   │   ├── ApiErrorResponse.cs
│   │   ├── ExceptionMiddleware.cs      # Catches → 401/404/400/500 JSON
│   │   ├── InputSanitizationMiddleware.cs # Strips HTML tags from form/query
│   │   └── RequestLoggingMiddleware.cs # Logs method/path/status/duration
│   ├── Hubs/
│   │   └── AuctionHub.cs              # SignalR — JoinAuctionGroup, LeaveAuctionGroup
│   └── Services/
│       ├── Background/
│       │   └── AuctionExpiryService.cs # Activates Scheduled, closes Expired auctions
│       ├── Email/
│       │   └── SmtpEmailService.cs     # SMTP email sending
│       ├── FileStorage/
│       │   └── CloudinaryFileStorageService.cs
│       └── Token/
│           └── TokenService.cs         # JWT generation
│
├── Sayiad.Domain/                     # Business Logic Layer
│   ├── Common/
│   │   ├── Pagination.cs             # PaginationRequest, PagedResult<T>
│   │   ├── Result.cs                 # Result<T>, Result (success/error pattern)
│   │   └── InputSanitizer.cs         # Strips HTML tags
│   ├── Contracts/                    # Interfaces
│   │   ├── IEmailService.cs
│   │   ├── IFileStorageService.cs
│   │   └── Subscription/
│   │       └── ISubscriptionManager.cs
│   ├── Dtos/                         # 15 subfolders
│   │   ├── AuctionDtos/              # AuctionDto, AuctionAnalyticsDto, AuctionRequestDto
│   │   ├── AuthDtos/                 # AuthDto, ForgotPasswordRequest, ResetPasswordRequest, etc.
│   │   ├── CartDtos/
│   │   ├── CategoryDtos/
│   │   ├── NotificationDtos/
│   │   ├── OrderDtos/
│   │   ├── PaymentDtos/
│   │   ├── ProductDtos/
│   │   ├── ReportDtos/
│   │   ├── ReviewDtos/
│   │   ├── SellerProfileDtos/
│   │   ├── ShippingAddressDtos/
│   │   ├── Subscription/            # SubscriptionResponse, UpgradeSubscriptionRequest
│   │   ├── SubscriptionPlanDtos/    # SubscriptionPlanResponse, Create/Update requests
│   │   ├── UserDtos/
│   │   ├── WalletDtos/               # WalletResponse, WalletTransactionResponse, DepositRequest
│   │   └── WishlistDtos/
│   ├── Managers/                     # 17 managers + interfaces
│   │   ├── AuctionManager.cs / IAuctionManager.cs
│   │   ├── AuthManager.cs / IAuthManager.cs
│   │   ├── CartManager.cs / ICartManager.cs
│   │   ├── CategoryManager.cs / ICategoryManager.cs
│   │   ├── NotificationManager.cs / INotificationManager.cs
│   │   ├── OrderManager.cs / IOrderManager.cs
│   │   ├── PaymentManager.cs / IPaymentManager.cs
│   │   ├── ProductManager.cs / IProductManager.cs
│   │   ├── ReportManager.cs / IReportManager.cs
│   │   ├── ReviewManager.cs / IReviewManager.cs
│   │   ├── SellerProfileManager.cs / ISellerProfileManager.cs
│   │   ├── ShippingAddressManager.cs / IShippingAddressManager.cs
│   │   ├── SubscriptionManager.cs / ISubscriptionManager.cs
│   │   ├── SubscriptionPlanManager.cs / ISubscriptionPlanManager.cs
│   │   ├── UserManager.cs / IUserManager.cs
│   │   ├── WalletManager.cs / IWalletManager.cs
│   │   └── WishlistManager.cs / IWishlistManager.cs
│   ├── Validators/                   # FluentValidation per-request validators
│   │   ├── Auth/                     # ForgotPasswordRequestValidator, ResetPasswordRequestValidator
│   │   ├── Subscription/            # UpgradeSubscriptionRequestValidator
│   │   ├── AuctionValidators.cs
│   │   ├── CartValidators.cs
│   │   ├── CategoryValidators.cs
│   │   ├── ChangePasswordValidator.cs
│   │   ├── CreateProductValidator.cs
│   │   ├── CreateShippingAddressValidator.cs
│   │   ├── LoginValidator.cs
│   │   ├── OrderValidators.cs
│   │   ├── RegisterValidator.cs
│   │   ├── ReportValidators.cs
│   │   ├── ReviewValidators.cs
│   │   ├── SellerProfileValidator.cs
│   │   ├── SubmitAuctionRequestValidator.cs
│   │   ├── UpdateProductValidator.cs
│   │   ├── UserValidators.cs
│   │   └── WishlistValidators.cs
│   └── Sayiad.Domain.csproj
│
├── Sayiad.Data/                       # Data Access Layer
│   ├── Data/
│   │   ├── ApplicationDbContext.cs    # 23 DbSets, 8 configs, PaymentStatus converter
│   │   ├── IUnitOfWork.cs
│   │   └── UnitOfWork.cs
│   ├── Models/                       # 21 entity models
│   │   ├── Auction/
│   │   │   ├── Auction.cs
│   │   │   └── AuctionRequest.cs
│   │   ├── Bid/Bid.cs
│   │   ├── Cart/
│   │   │   ├── Cart.cs
│   │   │   └── CartItem.cs
│   │   ├── Category/Category.cs
│   │   ├── Configurations/           # IEntityTypeConfiguration (8 files)
│   │   ├── Notification/Notification.cs
│   │   ├── Order/
│   │   │   ├── CustomerOrder.cs
│   │   │   └── OrderItem.cs
│   │   ├── Payment/Payment.cs
│   │   ├── Product/
│   │   │   ├── Product.cs
│   │   │   └── ProductImage.cs
│   │   ├── Report/Report.cs
│   │   ├── Review/Review.cs
│   │   ├── SellerProfile/SellerProfile.cs
│   │   ├── ShippingAddress/ShippingAddress.cs
│   │   ├── Subscription/Subscription.cs
│   │   ├── SubscriptionPlan/SubscriptionPlan.cs
│   │   ├── Transaction/Transaction.cs
│   │   ├── User/User.cs
│   │   ├── Wallet/
│   │   │   ├── Wallet.cs
│   │   │   └── WalletTransaction.cs
│   │   ├── Wishlist/Wishlist.cs
│   │   ├── AuctionFilterRequest.cs
│   │   ├── AuctionRequestStatus.cs (enum)
│   │   ├── AuctionStatus.cs (enum)
│   │   ├── BidStatus.cs (enum)
│   │   ├── CustomerOrderStatus.cs (enum)
│   │   ├── PaymentStatus.cs (enum)
│   │   ├── ProductCondition.cs (enum)
│   │   ├── ProductFilterRequest.cs
│   │   ├── ProductStatus.cs (enum)
│   │   ├── SubscriptionTier.cs (enum)
│   │   └── UserRole.cs (enum)
│   ├── Repository/                   # 16 repo folders
│   │   ├── AuctionRepo/
│   │   ├── CartRepo/
│   │   ├── CategoryRepo/
│   │   ├── NotificationRepo/
│   │   ├── OrderRepo/
│   │   ├── PaymentRepo/
│   │   ├── ProductRepo/
│   │   ├── ReportRepo/
│   │   ├── ReviewRepo/
│   │   ├── SellerProfileRepo/
│   │   ├── ShippingAddressRepo/
│   │   ├── SubscriptionPlanRepo/
│   │   ├── SubscriptionRepo/
│   │   ├── UserRepo/
│   │   ├── WalletRepo/
│   │   └── WishlistRepo/
│   ├── Migrations/                   # 14 migration snapshots + model snapshot
│   └── Sayiad.Data.csproj
│
└── Sayiad.Tests/
    ├── Integration/
    │   └── AuctionConcurrencyTests.cs  # 6 EF InMemory auction tests
    ├── Managers/
    │   ├── AuctionQuotaTests.cs        # Subscription quota enforcement
    │   ├── AuthManagerTests.cs         # Register, Login, Refresh
    │   ├── ForgotPasswordTests.cs      # Forgot/reset password flow
    │   ├── ProductManagerTests.cs      # Product CRUD
    │   └── SubscriptionManagerTests.cs # Upgrade + duplicate paymentRef
    └── Sayiad.Tests.csproj
```

---

## CONTROLLERS — All 16 Controllers & Every Endpoint

### AuthController — `[Route("api/[controller]")]`
| Method | Route | Auth | Rate-Limited | Description |
|--------|-------|------|-------------|-------------|
| POST | `/api/auth/register` | Public | ✅ `auth` (10/min) | Register with role; admin rejected |
| POST | `/api/auth/login` | Public | ✅ `auth` (10/min) | Login → JWT + refresh token |
| POST | `/api/auth/refresh` | Public | ❌ | Refresh JWT using refresh token |
| POST | `/api/auth/logout` | Authenticated | ❌ | Clear refresh token |
| GET | `/api/auth/verify-email` | Public | ❌ | Verify email via token query param |
| POST | `/api/auth/resend-verification` | Public | ✅ `auth` | Resend verification email |
| POST | `/api/auth/forgot-password` | Public | ✅ `auth` | Send OTP reset code (returns 404 if email not found) |
| POST | `/api/auth/verify-reset-code` | Public | ✅ `auth` | Verify OTP without resetting password |
| POST | `/api/auth/reset-password` | Public | ✅ `auth` | Reset password with verified OTP |
| POST | `/api/auth/change-password` | Authenticated | ❌ | Change password (needs current + new) |

### ProductsController — `[Route("api/[controller]")]`
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/products` | Anonymous | List products (filter: categoryId, minPrice, maxPrice, condition, searchTerm, inStock, sortBy, sortDirection); paginated; only `Status == Available` |
| GET | `/api/products/{id}` | Anonymous | Get product by ID with images + seller info |
| POST | `/api/products` | Fisherman,BaitSeller,Auctioneer | Create product |
| PUT | `/api/products/{id}` | Fisherman,BaitSeller,Auctioneer | Update own product |
| DELETE | `/api/products/{id}` | Fisherman,BaitSeller,Auctioneer | Soft-delete own product |
| GET | `/api/products/my` | Fisherman,BaitSeller,Auctioneer | Get own products |
| POST | `/api/products/{id}/images` | Fisherman,BaitSeller,Auctioneer | Add image to product |
| DELETE | `/api/products/{id}/images/{imageId}` | Fisherman,BaitSeller,Auctioneer | Delete product image |
| GET | `/api/products/pending-review` | Admin | List pending-review products (paginated) |
| PATCH | `/api/products/{id}/approve` | Admin | Approve pending product → Available |
| PATCH | `/api/products/{id}/reject` | Admin | Reject pending product with reason |
| PATCH | `/api/products/{id}/status` | Admin | Update product status (suspend/reject) |

### AuctionsController — `[Route("api/[controller]")]`
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/auctions` | Anonymous | List active auctions (filter: searchTerm, minPrice, maxPrice, status); paginated |
| GET | `/api/auctions/{id}` | Anonymous | Get auction with bids — SignalR ready |
| POST | `/api/auctions` | Auctioneer | Create auction |
| POST | `/api/auctions/{id}/bids` | Customer,Admin | Place bid; broadcasts via SignalR |
| POST | `/api/auctions/{id}/end` | Auctioneer,Admin | End auction early; broadcasts winner |
| POST | `/api/auctions/requests` | Fisherman | Submit auction request |
| GET | `/api/auctions/requests/my` | Fisherman | Get my requests (paginated) |
| GET | `/api/auctions/requests/pending` | Auctioneer | Get pending requests (paginated) |
| POST | `/api/auctions/requests/{id}/approve` | Auctioneer | Approve → creates auction |
| POST | `/api/auctions/requests/{id}/reject` | Auctioneer | Reject with reason |
| GET | `/api/auctions/dashboard` | Auctioneer,Admin | Analytics stats |

### CartController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/cart` | Get user's cart with items |
| POST | `/api/cart/items` | Add item to cart |
| PUT | `/api/cart/items/{productId}` | Update item quantity |
| DELETE | `/api/cart/items/{productId}` | Remove item from cart |
| DELETE | `/api/cart` | Clear entire cart |

### WishlistController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/wishlist` | Get wishlist (paginated via query params) |
| POST | `/api/wishlist/toggle` | Toggle product in wishlist |
| DELETE | `/api/wishlist/{productId}` | Remove from wishlist |

### OrdersController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/orders` | Create order from cart (transactional: order + stock + payment) |
| GET | `/api/orders` | Get my orders (paginated) |
| GET | `/api/orders/seller` | Get seller's received orders |
| GET | `/api/orders/{id}` | Get order by ID with items |
| PUT | `/api/orders/{id}/cancel` | Cancel own pending order (stock restoration) |
| PUT | `/api/orders/{id}/status` | Admin: update order status |

### CategoriesController — `[Route("api/[controller]")]`
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/categories` | Anonymous | List all categories |
| GET | `/api/categories/{id}` | Anonymous | Get category by ID |
| POST | `/api/categories` | Admin | Create category |
| PUT | `/api/categories/{id}` | Admin | Update category |
| DELETE | `/api/categories/{id}` | Admin | Delete category (blocked if has products) |

### ReviewsController — `[Route("api/[controller]")]`
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/reviews/product/{productId}` | Anonymous | Get product reviews |
| GET | `/api/reviews/product/{productId}/rating` | Anonymous | Get average rating + count |
| POST | `/api/reviews` | Authenticated | Create review (transactional: + rating update) |
| DELETE | `/api/reviews/{id}` | Authenticated | Delete own review (transactional: + rating update) |

### NotificationsController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/notifications` | Get user notifications |
| GET | `/api/notifications/unread-count` | Get unread count only |
| PUT | `/api/notifications/{id}/read` | Mark notification as read |
| PUT | `/api/notifications/read-all` | Mark all as read |

### PaymentsController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/payments/initiate` | Initiate payment for order |
| POST | `/api/payments/{paymentId}/confirm` | Confirm payment (mock) |
| GET | `/api/payments/order/{orderId}` | Get payments for order |

### ShippingAddressesController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/shippingaddresses` | Create address |
| GET | `/api/shippingaddresses` | Get my addresses |
| DELETE | `/api/shippingaddresses/{id}` | Delete address (with ownership check) |

### SellerProfileController — `[Route("api/seller-profile")]`
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/seller-profile` | Fisherman,BaitSeller | Create seller profile |
| GET | `/api/seller-profile/{userId}` | AllowAnonymous | Get public seller profile |
| PUT | `/api/seller-profile` | Fisherman,BaitSeller | Update own profile |
| GET | `/api/seller-profile/me` | Fisherman,BaitSeller | Get own seller profile |
| GET | `/api/seller-profile/dashboard` | Fisherman,BaitSeller | Seller dashboard stats |

### UsersController — `[Route("api/[controller]")]`
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/users/profile` | Authenticated | Get own profile |
| PUT | `/api/users/profile` | Authenticated | Update own profile |
| GET | `/api/users` | Admin | List all users (paginated) |
| GET | `/api/users/{id}` | Admin | Get user by ID |
| PATCH | `/api/users/{id}/toggle-status` | Admin | Activate/suspend user |

### SubscriptionsController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/subscriptions/upgrade` | Authenticated | Upgrade subscription tier |
| GET | `/api/subscriptions/my` | Authenticated | Get own subscription |
| GET | `/api/subscriptions` | Admin | Get all subscriptions (paginated) |

### ReportsController — `[Route("api/[controller]")]`
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/reports` | Authenticated | Create report (duplicate check) |
| GET | `/api/reports` | Admin | List all reports |
| GET | `/api/reports/{id}` | Admin | Get report by ID |
| PUT | `/api/reports/{id}/resolve` | Admin | Resolve report |

### WalletController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/wallet` | Get current user's wallet (balance, held, available) |
| POST | `/api/wallet/deposit` | Deposit funds into wallet |
| GET | `/api/wallet/transactions` | Get paginated transaction history |

### UploadController — `[Route("api/[controller]")]`, `[Authorize]`
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/upload` | Upload image (5MB limit, jpg/jpeg/png/webp, magic bytes validation) → returns `{url}` |

---

## ENTITY MODELS — Complete Reference

### User
| Property | Type | Constraints |
|----------|------|-------------|
| Id | int | PK, auto-increment |
| FullName | string | max 150 |
| Email | string | max 100, unique index |
| PasswordHash | string | BCrypt |
| Phone | string | nullable |
| ProfileImage | string? | nullable |
| Role | UserRole | enum: Admin/Fisherman/BaitSeller/Auctioneer/Customer |
| IsActive | bool | default true |
| IsEmailVerified | bool | default false |
| EmailVerificationToken | string? | SHA256-hashed |
| RefreshToken | string? | SHA256-hashed |
| RefreshTokenExpiry | DateTime? | |
| PasswordResetToken | string? | BCrypt-hashed |
| PasswordResetTokenExpiry | DateTime? | |
| LicenseNumber | string? | max 50, required for Fisherman |
| SubscriptionTier | SubscriptionTier | default Free |
| CreatedAt | DateTime | default GETUTCDATE() |
| UpdatedAt | DateTime | default GETUTCDATE() |

**Navigation:** Products, CustomerOrders, Bids, Reviews, ShippingAddresses, Cart (1:1), Wishlists, Notifications, Reports, SellerProfile (1:1), WonAuctions, SoldOrderItems, Subscriptions, Wallet (1:1)

### Product
| Property | Type | Constraints |
|----------|------|-------------|
| Id | int | PK |
| SellerId | int | FK → User (Restrict) |
| CategoryId | int | FK → Category |
| Title | string | max 200 |
| Description | string | |
| Brand | string | max 50 |
| Condition | ProductCondition | New/Used |
| Price | decimal(18,2) | |
| StockQuantity | int | |
| Location | string | max 100 |
| IsAuctioned | bool | |
| Status | ProductStatus | Available/Sold/Draft/Rejected/Suspended |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime | |
| DeletedAt | DateTime? | soft delete |
| ReviewedByUserId | int? | FK → User (admin who reviewed) |
| ReviewedAt | DateTime? | When product was reviewed |
| RejectionReason | string? | Reason if rejected |

**Navigation:** Seller, Category, Images (ProductImage), Auctions, OrderItems, Reviews, CartItems, Wishlists, Reports

### Category
| Property | Type |
|----------|------|
| Id | int PK |
| Name | string |
| Description | string? |
| CreatedAt | DateTime |

### ProductImage
| Property | Type |
|----------|------|
| Id | int PK |
| ProductId | int FK |
| ImageUrl | string |
| IsPrimary | bool |
| CreatedAt | DateTime |

### Auction
| Property | Type | Constraints |
|----------|------|-------------|
| Id | int | PK |
| ProductId | int | FK → Product (Cascade) |
| CreatedByUserId | int | FK → User (Restrict) |
| WinnerUserId | int? | FK → User (SetNull) |
| StartTime | DateTime | |
| EndTime | DateTime | |
| StartingPrice | decimal(18,2) | |
| ReservePrice | decimal(18,2) | |
| MinimumIncrement | decimal(18,2) | |
| CurrentHighestBid | decimal(18,2) | |
| Status | AuctionStatus | Active/Finished/Cancelled/Scheduled |
| RowVersion | byte[] | [Timestamp] concurrency token |
| CreatedAt | DateTime | |

**Navigation:** Product, CreatedByUser, Winner (User), Bids

### AuctionRequest
| Property | Type | Constraints |
|----------|------|-------------|
| Id | int | PK |
| FishermanId | int | FK → User (Restrict) |
| ReviewedByAuctioneerId | int? | FK → User (Restrict) |
| ResultingAuctionId | int? | FK → Auction (Restrict) |
| ProductTitle | string | max 200 |
| ProductDescription | string | max 2000 |
| ProductImageUrl | string? | |
| EstimatedValue | decimal(18,2) | |
| QuantityKg | decimal(10,2) | |
| FishType | string | max 100 |
| CatchLocation | string | max 200 |
| CatchDate | DateTime | |
| Status | AuctionRequestStatus | Pending/Approved/Rejected |
| RejectionReason | string? | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime | |

### Bid
| Property | Type | Constraints |
|----------|------|-------------|
| Id | int | PK |
| AuctionId | int | FK → Auction (Cascade) |
| UserId | int | FK → User |
| Amount | decimal(18,2) | |
| IsAutoBid | bool | |
| MaxAutoBidAmount | decimal(18,2)? | |
| BidStatus | BidStatus | Valid/Rejected/Winning |
| CreatedAt | DateTime | |

### Cart
| Property | Type |
|----------|------|
| Id | int PK |
| UserId | int FK (unique) |
| CreatedAt | DateTime |
| UpdatedAt | DateTime |

### CartItem
| Property | Type |
|----------|------|
| Id | int PK |
| CartId | int FK |
| ProductId | int FK |
| Quantity | int |
| CreatedAt | DateTime |

### CustomerOrder
| Property | Type |
|----------|------|
| Id | int PK |
| BuyerId | int FK → User |
| ShippingAddressId | int FK → ShippingAddress (Restrict) |
| TotalPrice | decimal(18,2) |
| Status | CustomerOrderStatus |
| CreatedAt | DateTime |
| UpdatedAt | DateTime |

### OrderItem
| Property | Type |
|----------|------|
| Id | int PK |
| OrderId | int FK |
| ProductId | int FK |
| SellerId | int FK → User |
| Quantity | int |
| UnitPrice | decimal(18,2) |
| Subtotal | decimal(18,2) |
| CreatedAt | DateTime |

### Payment
| Property | Type |
|----------|------|
| Id | int PK |
| OrderId | int FK |
| Amount | decimal(18,2) |
| PaymentMethod | string |
| PaymentStatus | PaymentStatus (converter: Confirmed↔"Paid") |
| PaidAt | DateTime? |
| CreatedAt | DateTime |

### Transaction
| Property | Type |
|----------|------|
| Id | int PK |
| PaymentId | int FK |
| TransactionReference | string |
| Amount | decimal(18,2) |
| Status | string |
| CreatedAt | DateTime |

### ShippingAddress
| Property | Type |
|----------|------|
| Id | int PK |
| UserId | int FK |
| FullName | string |
| Phone | string |
| City | string |
| AddressLine | string |
| PostalCode | string |
| IsDefault | bool |
| CreatedAt | DateTime |

### Review
| Property | Type |
|----------|------|
| Id | int PK |
| ProductId | int FK |
| UserId | int FK |
| Rating | int (1-5) |
| Comment | string? max 500 |
| CreatedAt | DateTime |

### Notification
| Property | Type |
|----------|------|
| Id | int PK |
| UserId | int FK |
| Title | string |
| Message | string |
| IsRead | bool |
| CreatedAt | DateTime |

### Report
| Property | Type |
|----------|------|
| Id | int PK |
| ReporterId | int FK |
| ProductId | int FK |
| Reason | string |
| Status | string |
| CreatedAt | DateTime |

### SellerProfile
| Property | Type |
|----------|------|
| Id | int PK |
| UserId | int FK (unique) |
| StoreName | string |
| StoreDescription | string? |
| AverageRating | decimal(3,2) |
| TotalSales | int |
| CreatedAt | DateTime |

### Subscription
| Property | Type | Constraints |
|----------|------|-------------|
| Id | int | PK |
| UserId | int | FK → User |
| Tier | SubscriptionTier | Free(0)/Basic(1)/Pro(2)/Enterprise(3) |
| StartDate | DateTime | |
| EndDate | DateTime? | nullable for Free |
| IsActive | bool | |
| PaymentReference | string? | unique filtered index `WHERE PaymentReference IS NOT NULL` |

### Wishlist
| Property | Type |
|----------|------|
| Id | int PK |
| UserId | int FK |
| ProductId | int FK |
| CreatedAt | DateTime |

---

## ENUMS

| Enum | Values | Storage |
|------|--------|---------|
| `UserRole` | Admin, Fisherman, BaitSeller, Auctioneer, Customer | string (JsonStringEnumConverter) |
| `ProductCondition` | New, Used | string |
| `ProductStatus` | Available, Sold, Draft, Rejected, Suspended | string |
| `AuctionStatus` | Active, Finished, Cancelled, Scheduled | string |
| `BidStatus` | Valid, Rejected, Winning | string |
| `CustomerOrderStatus` | Pending, Paid, Shipped, Delivered, Cancelled | string |
| `PaymentStatus` | Pending, Confirmed, Failed, Cancelled | string (value converter: Confirmed↔"Paid") |
| `SubscriptionTier` | Free(0), Basic(1), Pro(2), Enterprise(3) | int |
| `AuctionRequestStatus` | Pending(0), Approved(1), Rejected(2) | int |

---

## KEY MANAGER LOGIC

### AuctionManager — Bid Placement (Concurrency Protected + Wallet Hold)
```
1. Load Auction with RowVersion
2. Validate: Active, not expired, amount >= CurrentHighestBid + MinimumIncrement
3. Check HasSufficientBalanceAsync (wallet)
4. Downgrade previous winning bids to Valid
5. Create new Bid (status: Winning)
6. Update Auction.CurrentHighestBid
7. SaveChangesAsync → DbUpdateConcurrencyException → retry up to 3x with reload
8. ReleaseHeldFundsAsync for outbid user (wallet)
9. HoldFundsAsync for new bidder (wallet)
10. Broadcast via SignalR: BidPlaced
```
**Auto-bid resolution**: After manual bid, resolves eligible auto-bids (up to 20 rounds). Each round releases previous winner's funds and holds auto-bidder's funds. Free-tier users blocked from auto-bid (requires Basic+).
**Auction end**: If reserve not met (`WinnerUserId == null`), releases held funds for last winning bidder.

### OrderManager — CreateFromCartAsync
```
1. BeginTransaction
2. Validate stock for all items
3. Decrement stock for each product
4. Create CustomerOrder + OrderItems
5. Clear cart
6. CommitTransaction
```

### AuthManager — RegisterAsync
```
1. Validate email uniqueness
2. Hash password (BCrypt)
3. Hash email verification token (SHA256)
4. Create user (Admin role rejected)
5. Auto-create Wallet for user (all non-Admin roles)
6. Send verification email via IEmailService
7. Return JWT + refresh token
```

### Quotas per SubscriptionPlan (seeded defaults)
| Plan | Auctions/Month | Bids/Month | Requests/Month | Price |
|------|---------------|-----------|----------------|-------|
| Free | 3 | 3 | 3 | $0 |
| Basic | 10 | 20 | 10 | $10 |
| Pro | 25 | 50 | 25 | $20 |
| Enterprise | 100 | 200 | 100 | $50 |

### SubscriptionManager — UpgradeAsync
```
1. Look up plan from DB by tier
2. Validate PaymentReference uniqueness
3. Deactivate old subscription
4. Create new Subscription record
5. Update User.SubscriptionTier
```

### SubscriptionPlanManager — CRUD
```
GET  /api/subscriptionplans        → Public: list active plans sorted by SortOrder
GET  /api/subscriptionplans/{id}   → Public: get plan by ID
POST /api/subscriptionplans        → Admin: create plan (tier must be unique)
PUT  /api/subscriptionplans/{id}   → Admin: partial update plan fields
DELETE /api/subscriptionplans/{id} → Admin: delete plan
```

### Quota enforcement points (AuctionManager)
```
1. CreateAsync — checks MaxAuctionsPerMonth against monthly auction count
2. ApproveRequestAsync — checks MaxAuctionsPerMonth against monthly auction count (auctioneer)
3. PlaceBidAsync — checks MaxBidsPerMonth against monthly bid count
4. SubmitRequestAsync — checks MaxAuctionRequestsPerMonth against monthly request count (fisherman)
```

---

## FILTER REQUESTS

**ProductFilterRequest**: `record(int? CategoryId, decimal? MinPrice, decimal? MaxPrice, ProductCondition? Condition, string? Location, string? SearchTerm, bool? InStock, string? SortBy, string? SortDirection)`

**AuctionFilterRequest**: `record(string? SearchTerm, decimal? MinPrice, decimal? MaxPrice, string? Status)` — status defaults to Active when null

---

## MIDDLEWARE PIPELINE (order in Program.cs)

```
SerilogRequestLogging
→ InputSanitizationMiddleware (HTML tag stripping)
→ RequestLoggingMiddleware (method/path/status/duration)
→ ExceptionMiddleware (401/404/400/500 JSON)
→ Swagger/SwaggerUI (always enabled)
→ StaticFiles
→ HttpsRedirection
→ CORS ("AllowFrontend": https://saiyad-eg.vercel.app)
→ RateLimiter ("auth": 10/min)
→ Authentication
→ Authorization
→ MapControllers / HealthChecks / SignalR hub
```

---

## TESTS — 23 total

| Test File | Tests | Type |
|-----------|-------|------|
| `AuthManagerTests` | 8 | Unit (Moq) |
| `ForgotPasswordTests` | 3 | Unit (Moq) |
| `ProductManagerTests` | 2 | Unit (Moq) |
| `SubscriptionManagerTests` | 3 | Unit (Moq) + 1 duplicate paymentRef |
| `AuctionQuotaTests` | 2 | Unit (Moq) |
| `AuctionConcurrencyTests` | 6 | Integration (EF InMemory) |

**Known failure:** 1 ForgotPassword test pre-existing (not related to changes)

---

## PROGRAM.CS CONFIGURATION

| Setting | Value |
|---------|-------|
| Connection string | `Dev` (SQL Server) |
| JWT expiry | 60 min (in TokenService) |
| Refresh token expiry | 7 days |
| CORS origin | `https://saiyad-eg.vercel.app` |
| Rate limit (auth) | 10 requests/min, 429 on overflow |
| Enum serialization | `JsonStringEnumConverter` (strings) |
| SignalR hub | `/hubs/auction` |
| Health check | `/health` (SQL Server probe) |
| Swagger | `/swagger/index.html` (always on) |
| Migrations | Auto-applied on startup in Production |
| File upload | 5MB max, jpg/jpeg/png/webp, magic bytes |
| Background service | `AuctionExpiryService` — runs every 5 min |

---

## AUCTION ENGINE

- **RowVersion** (`byte[]` with `[Timestamp]`) — SQL Server rowversion. EF includes original in UPDATE WHERE clause. If modified concurrently, 0 rows affected → `DbUpdateConcurrencyException` → retry up to 3 times.
- **AuctionExpiryService** — Background service checking every 5 min: activates Scheduled auctions, closes Expired auctions, notifies winner, sets Product.Sold.
- **Auto-bid** — `MaxAutoBidAmount` on Bid. After each manual bid, resolves up to 20 rounds of auto-bids.
- **SignalR broadcast** — `BidPlaced` (bidder ID + amount), `AuctionEnded` (winner ID), `AuctionExtended` (new end time).

---

## COMMON PITFALLS

- **Shipping route** — Controller is `ShippingAddressesController`, route is `api/shippingaddresses` (no hyphen, no `-es`)
- **Role claim** — Use `ClaimTypes.Role` (default), not `RoleClaimType = "role"`
- **Transaction ownership** — `OrderManager` owns the transaction, not `OrderRepository`
- **Admin self-registration** — Blocked in `AuthManager.RegisterAsync`
- **Public product filter** — Only `Status == Available` products shown publicly
- **PaymentStatus DB column** — Stores `"Pending"`/`"Paid"`; enum `Confirmed` maps to `"Paid"` string
- **Subscription data** — `GetAll` is Admin-only (C4)
- **TokenService** — Generates JWT with claims: `NameIdentifier` (userId), `Email`, `Role`, `Name` (fullName)

---

## ORPHANS & PENDING

| Item | Status |
|------|--------|
| C1–C4 fixes + Wallet + SubscriptionPlan migrations need deploy to production | ⏳ Pending |
| Bid-Wallet integration (HoldFunds on bid placement) | ✅ Wave 4 |
| Product Review Flow (PendingReview, Admin approve/deny) | ✅ Wave 5 |
| Platform fee deduction on auction win (5%) → admin wallet | ✅ Wave 6 |
| Subscription price deducted from wallet → admin wallet | ✅ Wave 7 |
| Subscription plan prices converted USD→EGP (×50) | ✅ Wave 7 |
| Full email verification gate on login | ⚠️ Partial (gate exists but some flows bypass) |
| Auction seed data for demo | ⚠️ Not yet |
| Full XML doc coverage (Swagger) | ⚠️ Partial (Auth + Auctions only) |
| Serilog file sink + log rotation | ⏳ Pending |
