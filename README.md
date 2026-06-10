# Sayiad (صياد) — Backend API

**.NET 10.0 Web API** | **SQL Server** | **SignalR** | **JWT Auth**

The backend for the Sayiad fishing marketplace & auction platform. Located alongside the vanilla JS frontend at `F:\DEPI Graduation Project\Front-end`.

> Full platform documentation is maintained at `Front-end/README.md`. This file covers backend-specific details.

---

## Project Structure

```
Back-end/
├── Sayiad.API/              # Presentation layer (thin controllers)
│   ├── Controllers/         # 16 controllers, 60+ endpoints
│   ├── Middleware/           # Exception, InputSanitization, RequestLogging
│   ├── Hubs/                # SignalR AuctionHub
│   ├── Services/
│   │   └── Background/      # AuctionExpiryService (hosted service)
│   └── Program.cs           # DI, Auth, Serilog, SignalR, CORS
├── Sayiad.Domain/           # Business logic layer
│   ├── Managers/            # 15 managers + interfaces (30 files)
│   ├── Dtos/                # 15 subfolders (per-feature DTOs)
│   ├── Validators/          # 21 FluentValidation files
│   ├── Contracts/           # IEmailService, IFileStorageService
│   └── Common/              # InputSanitizer
├── Sayiad.Data/             # Data access layer
│   ├── Models/              # 22 entity files + 8 enums
│   ├── Data/                # DbContext, UnitOfWork
│   ├── Repository/          # 17 repository implementations
│   ├── Migrations/          # EF Core migrations
│   └── Common/              # Pagination, Result pattern
├── Sayiad.Tests/            # Unit + integration tests
│   ├── Managers/            # AuthManager, ProductManager tests
│   └── Integration/         # Auction concurrency tests (EF InMemory)
└── SQL/                     # Raw SQL scripts
```

---

## Quick Start

```bash
# Restore & build
dotnet restore Sayiad.API/Sayiad.Api.csproj
dotnet build Sayiad.API/Sayiad.Api.csproj

# Apply migrations
dotnet ef database update --project Sayiad.Data --startup-project Sayiad.API

# Run
dotnet run --project Sayiad.API/Sayiad.Api.csproj
# → https://localhost:7030 | http://localhost:5002
# → Swagger at /swagger
```

---

## Architecture

```
Controller → Manager (business logic + auth + transactions)
                ↓
         Repository Interface (Domain.Contracts)
                ↓
         Repository Implementation (Data layer, EF Core)
                ↓
         SQL Server (via ApplicationDbContext)
```

- **API** references Domain + Data
- **Domain** references Data (contains managers, DTOs, validators)
- **Data** contains models, DbContext, migrations, repo implementations

---

## API Endpoints (16 Controllers)

| Controller | Routes | Auth |
|-----------|--------|:----:|
| `AuthController` | register, login, refresh, logout, change-password, forgot-password, reset-password, verify-email | Mixed |
| `AuctionsController` | CRUD, bids, end, requests (submit/my/pending/approve/reject), dashboard | Mixed |
| `ProductsController` | CRUD, seller-products, images | Mixed |
| `CartController` | get, add-item, update-item, remove-item, clear | Auth |
| `OrdersController` | create-from-cart, get-my, get-seller, get-by-id, update-status | Auth |
| `ReviewsController` | get-product-reviews, get-product-rating, create, delete | Mixed |
| `PaymentsController` | initiate, confirm, get-order-payments | Auth |
| `UsersController` | profile (get/put), get-all, get-by-id, toggle-status | Mixed |
| `WishlistController` | get, toggle, remove | Auth |
| `NotificationsController` | get-all, unread-count, mark-read, mark-all-read | Auth |
| `CategoriesController` | get-all, create, delete | Mixed |
| `ShippingAddressesController` | create, get-my, delete | Auth |
| `SellerProfileController` | create, get-by-id, update, get-my, dashboard | Mixed |
| `ReportsController` | create, get-all, get-by-id, resolve | Mixed |
| `SubscriptionsController` | upgrade, get-my, get-all | Mixed |
| `UploadController` | upload image | Auth |

---

## Key Features

- **5 user roles:** Admin, Customer, Fisherman, BaitSeller, Auctioneer
- **4 subscription tiers:** Free (3/mo), Basic (10/mo), Pro (25/mo), Enterprise (100/mo)
- **Real-time bidding:** SignalR with group-based auction rooms
- **Auto-bid engine:** Resolves counter-bids for up to 20 rounds
- **Auction concurrency:** RowVersion timestamp + 3-retry loop
- **Auction scheduling:** Optional StartTime, background activation
- **Auction requests:** Fisherman→Auctioneer workflow with approval/rejection
- **Unit of Work:** Cross-repo transaction coordination
- **Stock management:** Atomic decrement on order placement
- **Token refresh:** Rotation + SHA256 hashing at rest
- **Input sanitization:** XSS protection at middleware level
- **File upload:** Magic bytes validation, 5MB limit, Cloudinary storage

---

## Tests

```
22 total — 16 unit + 6 integration
All passing
```

| Test Suite | Tests | Type |
|-----------|:-----:|:----:|
| AuthManager | 6 | Unit |
| ProductManager | 2 | Unit |
| SubscriptionManager | 3 | Unit |
| Auction quota | 2 | Unit |
| Auction integration | 6 | Integration (EF InMemory) |
| Forgot/reset password | 3 | Unit |

---

## Migrations

| Migration | Purpose |
|-----------|---------|
| `AddAuctionRequestSystem` | `AuctionRequests` table + `AuctionRequestStatus` enum |
| `AddAuctionScheduling` | `Scheduled` in `AuctionStatus` enum |
| `FixDecimalPrecision` | HasPrecision on Bid, OrderItem, Payment, SellerProfile, Transaction |
| `AddAutoBidMax` | `MaxAutoBidAmount` on Bids |
| `AddPasswordResetFields` | Password reset fields on User |
| `AddSubscriptionSystem` | Subscription tables + tier field on User |

---

## Complete Documentation

For the full platform documentation including:
- Complete endpoint reference with request/response shapes
- Frontend route map and page descriptions
- User role matrix and permissions
- All data models and relationships
- Implemented features checklist
- Future roadmap and missing features

→ See `Front-end/README.md` or `Front-end/PROJECT_MAP_FRONT-END.md`
