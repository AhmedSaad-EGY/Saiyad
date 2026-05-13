<div align="center">

<img src="https://img.shields.io/badge/-.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
<img src="https://img.shields.io/badge/-ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
<img src="https://img.shields.io/badge/-SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white"/>
<img src="https://img.shields.io/badge/-JWT-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white"/>
<img src="https://img.shields.io/badge/-Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black"/>

<br/><br/>

# 🎣 Sayiad — Fishing Marketplace & Auction Platform

**A fully-featured RESTful API for buying, selling, and auctioning fishing gear.**  
Built with a clean 3-layer architecture on .NET 10.

<br/>

[**Live API**](https://sayiad.runasp.net) · [**Swagger UI**](https://sayiad.runasp.net/swagger/index.html) · [**Report a Bug**](https://github.com/AhmedSaad-EGY/Grad_project/issues)

</div>

---

## 📖 Table of Contents

- [Overview](#-overview)
- [Architecture](#-architecture)
- [Tech Stack](#-tech-stack)
- [Features](#-features)
- [API Reference](#-api-reference)
- [User Roles & Permissions](#-user-roles--permissions)
- [Auction Engine](#-auction-engine)
- [Order Flow](#-order-flow)
- [Getting Started](#-getting-started)
- [Project Structure](#-project-structure)
- [Roadmap](#-roadmap)

---

## 🌊 Overview

**Sayiad** (صياد — "fisherman" in Arabic) is a marketplace platform built for the fishing community. It allows fishermen and bait sellers to list their gear, customers to browse and purchase, and auctioneers to run real-time competitive auctions — all through a clean, role-based REST API.

The project is a graduation-level backend built on **.NET 10** with a strict 3-layer architecture, JWT authentication, concurrency-safe auction bidding, and a complete order/payment pipeline.

---

## 🏗 Architecture

Sayiad follows a strict **3-Layer Architecture** where each layer has a single responsibility and a defined dependency direction:

```
┌─────────────────────────────────────────────────────────┐
│                     Sayiad.Api                          │
│   Thin controllers — parse claims, delegate, respond    │
│   Middleware — global exception handling                │
│   Services — JWT token generation                       │
└───────────────────┬─────────────────────────────────────┘
                    │ depends on
┌───────────────────▼─────────────────────────────────────┐
│                   Sayiad.Domain                         │
│   Models, Enums, DTOs, Validators                       │
│   Managers — all business logic & orchestration         │
│   Contracts — manager + repository interfaces           │
│   Common — Result<T>, Pagination, MappingConfig         │
└───────────────────┬─────────────────────────────────────┘
                    │ depends on
┌───────────────────▼─────────────────────────────────────┐
│                   Sayiad.Data                           │
│   EF Core repositories (implement Domain contracts)     │
│   ApplicationDbContext + Configurations                 │
│   Migrations                                            │
└─────────────────────────────────────────────────────────┘
```

**Key rules enforced:**
- `Domain` has **zero project references** — it's pure business logic
- `Data` depends on `Domain` (not the other way around)
- Controllers contain **no business logic** — only claim extraction and delegation
- All transactions, authorization checks, and validations live in **Managers**

**Request flow:**
```
HTTP Request
    → Controller (extract userId from JWT claim)
    → Manager (validate, authorize, orchestrate)
    → Repository interface (Domain contract)
    → Repository implementation (EF Core)
    → SQL Server
```

---

## ⚙ Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Framework | ASP.NET Core | 10.0 |
| Language | C# | 13 |
| ORM | Entity Framework Core | 10.0.7 |
| Database | SQL Server | — |
| Authentication | JWT Bearer | 10.0.7 |
| Validation | FluentValidation | 12.1.1 |
| Mapping | Mapster | 10.0.7 |
| Logging | Serilog (structured) | 10.0.0 |
| Password Hashing | BCrypt.Net-Next | 4.2.0 |
| Cloud Storage | CloudinaryDotNet | 1.* |
| Email | System.Net.Mail | (built-in) |
| Health Checks | AspNetCore.HealthChecks.SqlServer | 9.0.0 |
| API Docs | Swagger / Swashbuckle | 10.1.7 |
| Concurrency | EF Core RowVersion | — |
| Testing | xUnit + Moq + FluentAssertions | — |

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🔐 **Auth** | Register, login, JWT + refresh token, change password, email verification |
| 👤 **Users** | Profile management, admin user control, role-based access, paginated listing |
| 📦 **Products** | Full CRUD, category filtering, price/condition/location filter, image management |
| 🛒 **Cart** | Add, update, remove items, full cart view |
| ❤️ **Wishlist** | Toggle products in/out of wishlist |
| 📋 **Orders** | Cart-to-order checkout, stock validation, transaction-safe, email confirmation |
| 🏠 **Shipping** | Create and retrieve shipping addresses |
| 💳 **Payments** | Initiate and confirm payments, transaction history |
| ⭐ **Reviews** | Rate products, get average rating, delete own review |
| 🔔 **Notifications** | Get, mark read, mark all read, unread count |
| 🔨 **Auctions** | Create auctions, place bids, auto-close, concurrency-safe, background expiry |
| 🚩 **Reports** | Report products, admin moderation and resolution |
| 🖼 **Upload** | Image upload to Cloudinary with extension + magic bytes validation |
| ❤️ **Health** | Database health probe endpoint |
| 📧 **Email** | SMTP notifications for registration, orders, auction wins, password changes |

---

## 📡 API Reference

Base URL: `https://sayiad.runasp.net`

### 🔐 Authentication
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/auth/register` | Public | Register a new user (sends verification email) |
| `GET` | `/api/auth/verify-email` | Public | Verify email with token from registration |
| `POST` | `/api/auth/login` | Public | Login (requires verified email) |
| `POST` | `/api/auth/refresh` | Public | Refresh an expired token |
| `POST` | `/api/auth/change-password` | 🔒 Any | Change current password (sends alert email) |
| `POST` | `/api/auth/logout` | 🔒 Any | Clear refresh token |

### 👤 Users
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/users/profile` | 🔒 Any | Get own profile |
| `PUT` | `/api/users/profile` | 🔒 Any | Update own profile |
| `GET` | `/api/users` | 🔒 Admin | List all users (paginated: `?page=1&pageSize=20`) |
| `GET` | `/api/users/{id}` | 🔒 Admin | Get user by ID |
| `PATCH` | `/api/users/{id}/toggle-status` | 🔒 Admin | Enable/disable user |

### 📦 Products
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/products` | Public | Browse products (filterable) |
| `GET` | `/api/products/{id}` | Public | Get product details |
| `POST` | `/api/products` | 🔒 Fisherman/BaitSeller | Create a product |
| `PUT` | `/api/products/{id}` | 🔒 Owner | Update own product |
| `DELETE` | `/api/products/{id}` | 🔒 Owner | Soft-delete own product |
| `GET` | `/api/products/my` | 🔒 Fisherman/BaitSeller | Get own listings |
| `POST` | `/api/products/{id}/images` | 🔒 Owner | Link an image to a product |
| `DELETE` | `/api/products/{id}/images/{imageId}` | 🔒 Owner | Remove image from a product |

**Product filter query params:** `categoryId`, `minPrice`, `maxPrice`, `condition`, `location`, `search`

### 🛒 Cart
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/cart` | 🔒 Any | Get current cart |
| `POST` | `/api/cart/items` | 🔒 Any | Add item to cart |
| `PUT` | `/api/cart/items/{id}` | 🔒 Any | Update item quantity |
| `DELETE` | `/api/cart/items/{id}` | 🔒 Any | Remove item from cart |
| `DELETE` | `/api/cart` | 🔒 Any | Clear entire cart |

### 📋 Orders
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/orders` | 🔒 Any | Checkout — creates order from cart |
| `GET` | `/api/orders` | 🔒 Any | Get own orders |
| `GET` | `/api/orders/seller` | 🔒 Seller | Get orders containing my products |
| `GET` | `/api/orders/{id}` | 🔒 Owner | Get order details |
| `PUT` | `/api/orders/{id}/status` | 🔒 Admin | Update order status |

### 🏠 Shipping Addresses
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/shipping-addresses` | 🔒 Any | Create a shipping address |
| `GET` | `/api/shipping-addresses` | 🔒 Any | Get own addresses |

### 💳 Payments
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/payments/initiate` | 🔒 Any | Initiate payment for an order |
| `POST` | `/api/payments/{id}/confirm` | 🔒 Any | Confirm a pending payment |
| `GET` | `/api/payments/order/{orderId}` | 🔒 Owner | Get payments for an order |

### ⭐ Reviews
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/reviews/product/{id}` | Public | Get reviews for a product |
| `GET` | `/api/reviews/product/{id}/rating` | Public | Get average rating |
| `POST` | `/api/reviews` | 🔒 Any | Submit a review |
| `DELETE` | `/api/reviews/{id}` | 🔒 Owner | Delete own review |

### 🔨 Auctions
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/auctions` | Public | Get all active auctions |
| `GET` | `/api/auctions/{id}` | Public | Get auction details + bid history |
| `POST` | `/api/auctions` | 🔒 Auctioneer | Create an auction |
| `POST` | `/api/auctions/{id}/bids` | 🔒 Customer/Fisherman/BaitSeller | Place a bid |
| `POST` | `/api/auctions/{id}/end` | 🔒 Auctioneer | Manually close an auction |

### 🔔 Notifications
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/notifications` | 🔒 Any | Get all notifications |
| `GET` | `/api/notifications/unread-count` | 🔒 Any | Get unread count |
| `PUT` | `/api/notifications/{id}/read` | 🔒 Any | Mark one as read |
| `PUT` | `/api/notifications/read-all` | 🔒 Any | Mark all as read |

### 🖼 Upload
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/upload` | 🔒 Fisherman/BaitSeller | Upload an image (jpg/png/webp, max 5 MB, magic bytes validated) |

### ❤️ Health
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/health` | Public | Database connectivity probe |

### 🚩 Reports
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/reports` | 🔒 Any | Report a product |
| `GET` | `/api/reports` | 🔒 Admin | Get all reports |
| `GET` | `/api/reports/{id}` | 🔒 Admin | Get report details |
| `PUT` | `/api/reports/{id}/resolve` | 🔒 Admin | Resolve a report |

---

## 👥 User Roles & Permissions

| Role | Description | Key Permissions |
|------|-------------|-----------------|
| `Admin` | Platform administrator | Manage users, resolve reports, update order status |
| `Fisherman` | Fish product seller | Create/manage products, create auctions for own products |
| `BaitSeller` | Bait & accessories seller | Create/manage products, create auctions for own products |
| `Auctioneer` | Auction manager | Create and close auctions |
| `Customer` | Buyer | Browse, cart, order, bid, review, wishlist |

---

## 🔨 Auction Engine

The auction system is built with **optimistic concurrency** to safely handle simultaneous bids.

```
Bid Flow:
─────────
  1. Load Auction entity with RowVersion (SQL Server rowversion column)
  2. Validate:
       • Auction status = Active
       • EndTime not passed
       • Bid amount ≥ CurrentHighestBid + MinimumIncrement
  3. Downgrade previous Winning bids → Valid
  4. Insert new Bid with status = Winning
  5. Update Auction.CurrentHighestBid
  6. SaveChangesAsync()
       └─ If DbUpdateConcurrencyException (race condition detected)
              → Reload + retry, up to 3 attempts

Auction Close:
─────────────
   • Status set to Finished
   • Winner = highest bid that meets ReservePrice
   • Product.Status = Sold
   • Winner and seller notified via Notifications system and email
   • Auctions past EndTime auto-close every 5 minutes via AuctionExpiryService
```

**Concurrency guarantee:** EF Core uses the `RowVersion` column in the SQL `UPDATE WHERE` clause. If two bids arrive simultaneously, one will detect 0 rows affected and retry — no lost updates, no overselling.

---

## 🛒 Order Flow

```
Customer adds items to cart
        ↓
POST /api/shipping-addresses   → get address ID
        ↓
POST /api/orders               → validates cart, checks stock, creates order
  • Wraps in TransactionScope
  • Deducts StockQuantity for each item
  • Sets product Status = Sold when stock hits 0
  • Clears cart after success
        ↓
POST /api/payments/initiate    → creates Payment + Transaction record
        ↓
POST /api/payments/{id}/confirm → marks Payment = Paid, Order = Paid
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB or full)
- Git

### Installation

```bash
# 1. Clone the repository
git clone https://github.com/AhmedSaad-EGY/Grad_project.git
cd Grad_project

# 2. Set your connection string in appsettings.Development.json
# Sayiad.Api/appsettings.Development.json
{
  "ConnectionStrings": {
    "Dev": "Server=(localdb)\\mssqllocaldb;Database=SayiadDb;Trusted_Connection=True;"
  }
}

# 3. Apply migrations
cd Sayiad.Api
dotnet ef database update --project ../Sayiad.Data

# 4. Run the API
dotnet run
```

The API will start at `https://localhost:7001` and Swagger at `https://localhost:7001/swagger`.

### Configuration

| Key | Location | Description |
|-----|----------|-------------|
| `ConnectionStrings:Dev` | `appsettings.json` | SQL Server connection string |
| `Jwt:SecretKey` | `appsettings.json` | JWT signing secret (min 32 chars) |
| `Jwt:Issuer` | `appsettings.json` | Token issuer |
| `Jwt:Audience` | `appsettings.json` | Token audience |
| `Jwt:ExpiryInMinutes` | `appsettings.json` | Access token lifetime (default: 1440) |
| `Cloudinary:CloudName` | `appsettings.json` | Cloudinary cloud name |
| `Cloudinary:ApiKey` | `appsettings.json` | Cloudinary API key |
| `Cloudinary:ApiSecret` | `appsettings.json` | Cloudinary API secret |
| `Email:SmtpHost` | `appsettings.json` | SMTP server hostname |
| `Email:SmtpPort` | `appsettings.json` | SMTP server port (default: 587) |
| `Email:SenderEmail` | `appsettings.json` | SMTP sender email address |
| `Email:SenderPassword` | `appsettings.json` | SMTP app password |

> ⚠️ **Production:** Move all secrets (`Jwt:SecretKey`, connection strings, `Cloudinary:*`, `Email:*`) to environment variables or Azure Key Vault before deploying.

---

## 📁 Project Structure

```
Sayiad/
├── Sayiad.Api/
│   ├── Controllers/          # 15 thin controllers (Auth, Users, Products, etc.)
│   ├── Middleware/           # Global exception handler + error response shape
│   ├── Services/
│   │   ├── Token/            # JWT generation implementation
│   │   ├── Email/            # SMTP email sending service
│   │   ├── FileStorage/      # Cloudinary image upload service
│   │   └── Background/       # AuctionExpiryService (hosted background job)
│   └── Program.cs            # DI registration, auth config, Serilog, health checks
│
├── Sayiad.Domain/
│   ├── Models/               # 18 entity models (in Sayiad.Data)
│   ├── Enums/                # 6 domain enums (UserRole, ProductStatus, etc.)
│   ├── Contracts/            # IFileStorageService, IEmailService
│   ├── Managers/             # 12 business logic managers + interfaces
│   ├── Dtos/                 # 13 feature folders, request/response records
│   ├── Validators/           # FluentValidation validators (request-specific)
│   └── Common/               # MappingConfig (Mapster), Result<T>, Pagination
│
├── Sayiad.Data/
│   ├── Models/               # Entity models by feature subfolder
│   ├── Repository/           # 11 EF Core repository impls + interfaces per feature
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── Configurations/   # IEntityTypeConfiguration files
│   └── Migrations/           # EF Core migration history
│
└── Sayiad.Tests/             # Unit tests (xUnit + Moq + FluentAssertions)
    └── Managers/             # AuthManagerTests, ProductManagerTests
```

---

## 🗺 Roadmap

| Item | Status |
|------|--------|
| Core API (all 13 features) | ✅ Complete |
| JWT Auth + Refresh Tokens | ✅ Complete |
| Concurrency-safe auction bidding | ✅ Complete |
| 3-layer architecture + clean DI | ✅ Complete |
| FluentValidation auto-validation | ✅ Complete |
| Structured logging (Serilog) | ✅ Complete |
| Live deployment | ✅ [sayiad.runasp.net](https://sayiad.runasp.net) |
| Product image upload + linking | ✅ Complete |
| Background job — auto-close expired auctions | ✅ Complete |
| Email verification + notifications | ✅ Complete |
| Health check endpoint | ✅ Complete |
| Paginated user listing | ✅ Complete |
| Magic bytes upload validation | ✅ Complete |
| Unit tests (AuthManager + ProductManager) | ✅ Complete (6 tests) |
| Real-time bid updates (SignalR) | 🔜 Planned |
| Real payment gateway (Fawry / PayMob) | 🔜 Planned |
| Integration tests | 🔜 Planned |
| Frontend UI | 🔜 Planned |

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome.  
Feel free to open an [issue](https://github.com/AhmedSaad-EGY/Grad_project/issues) or submit a pull request.

---

## 📄 License

This project is open source and available under the [MIT License](LICENSE).

---

<div align="center">

Built with ❤️ by [Ahmed Saad](https://github.com/AhmedSaad-EGY)

</div>
