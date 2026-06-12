using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, cfg) =>
        cfg.ReadFrom.Configuration(context.Configuration));

    // ── Data / EF ─────────────────────────────────────────────────────────────
    // FIX ①: WebApplication.CreateBuilder already maps ConnectionStrings__Dev →
    //         ConnectionStrings:Dev via environment variables, so the manual
    //         Environment.GetEnvironmentVariable() fallback was redundant dead code.
    var connStr = builder.Configuration.GetConnectionString("Dev")
        ?? throw new InvalidOperationException("Connection string 'Dev' is missing.");

    builder.Services.AddDbContext<ApplicationDbContext>(opt =>
        opt.UseSqlServer(connStr));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddHealthChecks()
        .AddSqlServer(connStr, name: "database", tags: ["db", "sql"]);

    // ── JWT ───────────────────────────────────────────────────────────────────
    // FIX ①: Same as above — Jwt__SecretKey env var is already picked up by
    //         IConfiguration; no manual fallback needed.
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSection["SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey is missing.");

    builder.Services
        .AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };
            opt.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/hubs"))
                    {
                        var token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token))
                        {
                            ctx.Token = token;
                        }
                        else
                        {
                            var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();
                            if (authHeader?.StartsWith("Bearer ") == true)
                                ctx.Token = authHeader["Bearer ".Length..];
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // ── CORS ──────────────────────────────────────────────────────────────────
    var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
        ?? throw new InvalidOperationException("CORS origins not configured.");

    builder.Services.AddCors(opt =>
        opt.AddPolicy("AllowFrontend", policy => policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    // ── Rate Limiting ─────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(opt =>
    {
        opt.AddFixedWindowLimiter("auth", limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });
        opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── API Layer ─────────────────────────────────────────────────────────────
    // FIX ③: Removed AddOpenApi() — it registers .NET 9's built-in OpenAPI spec
    //         generator as a second, unused spec. Swashbuckle (AddSwaggerGen +
    //         UseSwagger + UseSwaggerUI) is the only one wired into the pipeline.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddSignalR();

    builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddScoped<Sayiad.Api.Filters.RequireValidatorFilter>();

    builder.Services
        .AddControllers(opt => opt.Filters.AddService<Sayiad.Api.Filters.RequireValidatorFilter>())
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // ── Repositories ──────────────────────────────────────────────────────────
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ICartRepository, CartRepository>();
    builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
    builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
    builder.Services.AddScoped<IReportRepository, ReportRepository>();
    builder.Services.AddScoped<IShippingAddressRepository, ShippingAddressRepository>();
    builder.Services.AddScoped<ISellerProfileRepository, SellerProfileRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    builder.Services.AddScoped<IWalletRepository, WalletRepository>();
    builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();

    // ── Domain Managers ───────────────────────────────────────────────────────
    builder.Services.AddScoped<ICategoryManager, CategoryManager>();
    builder.Services.AddScoped<IProductManager, ProductManager>();
    builder.Services.AddScoped<ICartManager, CartManager>();
    builder.Services.AddScoped<IWishlistManager, WishlistManager>();
    builder.Services.AddScoped<IOrderManager, OrderManager>();
    builder.Services.AddScoped<IPaymentManager, PaymentManager>();
    builder.Services.AddScoped<INotificationManager, NotificationManager>();
    builder.Services.AddScoped<IReviewManager, ReviewManager>();
    builder.Services.AddScoped<IAuctionManager, AuctionManager>();
    builder.Services.AddScoped<IReportManager, ReportManager>();
    builder.Services.AddScoped<IShippingAddressManager, ShippingAddressManager>();
    builder.Services.AddScoped<ISellerProfileManager, SellerProfileManager>();
    builder.Services.AddScoped<ISubscriptionManager, SubscriptionManager>();
    builder.Services.AddScoped<IWalletManager, WalletManager>();
    builder.Services.AddScoped<ISubscriptionPlanManager, SubscriptionPlanManager>();
    builder.Services.AddScoped<IAuthManager, AuthManager>();
    builder.Services.AddScoped<IUserManager, UserManager>();

    builder.Services.Configure<Sayiad.Domain.Common.AppSettings>(
        builder.Configuration.GetSection("AppSettings"));

    // ── Services ──────────────────────────────────────────────────────────────
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    builder.Services.AddScoped<IAuditService, Sayiad.Api.Services.Audit.AuditService>();

    builder.Services.AddHostedService<AuctionExpiryService>();

    builder.Services.AddTransient<Sayiad.Api.Middleware.ExceptionMiddleware>();

    // ═════════════════════════════════════════════════════════════════════════
    var app = builder.Build();
    // ═════════════════════════════════════════════════════════════════════════

    // ── Migrations (production only) ──────────────────────────────────────────
    if (app.Environment.IsProduction())
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();

            await using var lockCmd = conn.CreateCommand();
            // FIX ②: The original used the default @LockOwner='Transaction'. Without
            //         an explicit BEGIN TRANSACTION the lock is released the moment the
            //         batch completes — i.e. before MigrateAsync even starts. Using
            //         @LockOwner='Session' holds the lock for the lifetime of the
            //         connection, which spans the entire MigrateAsync call.
            lockCmd.CommandText = """
                EXEC sp_getapplock
                    @Resource    = 'SayiadMigration',
                    @LockMode    = 'Exclusive',
                    @LockOwner   = 'Session',
                    @LockTimeout = 30000
                """;

            var lockResult = Convert.ToInt32(await lockCmd.ExecuteScalarAsync());

            if (lockResult >= 0)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    Log.Information("Database migrations applied successfully.");
                }
                finally
                {
                    await using var releaseCmd = conn.CreateCommand();
                    releaseCmd.CommandText = """
                        EXEC sp_releaseapplock
                            @Resource  = 'SayiadMigration',
                            @LockOwner = 'Session'
                        """;
                    await releaseCmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                Log.Warning("Could not acquire migration lock (result: {Result}). Skipping.", lockResult);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Migration failed. Shutting down to prevent schema corruption.");
            throw;
        }
    }

    // ── Admin wallet bootstrap ────────────────────────────────────────────────
    try
    {
        using var scope = app.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var walletManager = scope.ServiceProvider.GetRequiredService<IWalletManager>();
        var adminEmail = app.Configuration["AppSettings:AdminEmail"] ?? "sayiadapp@gmail.com";

        var admin = await userRepo.GetByEmailAsync(adminEmail);

        if (admin is null)
            Log.Warning("Admin user {Email} not found — platform wallet not created.", adminEmail);
        else if (!await walletManager.WalletExistsAsync(admin.Id))
        {
            await walletManager.CreateWalletAsync(admin.Id);
            Log.Information("Admin wallet created for {Email}.", admin.Email);
        }
        else
            Log.Information("Admin wallet already exists for {Email}.", admin.Email);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to ensure admin wallet on startup.");
    }

    // ── Request Pipeline ──────────────────────────────────────────────────────
    app.UseSerilogRequestLogging();

    app.UseMiddleware<Sayiad.Api.Middleware.ExceptionMiddleware>();         // ① global error shield
    app.UseMiddleware<Sayiad.Api.Middleware.RequestLoggingMiddleware>();    // ② telemetry
    app.UseMiddleware<Sayiad.Api.Middleware.InputSanitizationMiddleware>(); // ③ sanitise input

    app.Use(async (ctx, next) =>                                            // ④ security headers
    {
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        await next();
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // FIX ④: CORS must run before UseHttpsRedirection. Browser OPTIONS preflights
    //         that hit UseHttpsRedirection first receive a 307 redirect rather than
    //         CORS headers; browsers do not follow redirects for preflights, so the
    //         CORS check fails. Placing UseCors first ensures preflights are answered
    //         correctly regardless of the scheme.
    app.UseCors("AllowFrontend");    // ⑤
    app.UseHttpsRedirection();       // ⑥
    app.UseStaticFiles();            // ⑦
    app.UseRateLimiter();            // ⑧
    app.UseAuthentication();         // ⑨
    app.UseAuthorization();          // ⑩

    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapHub<AuctionHub>("/hubs/auction").RequireCors("AllowFrontend");

    Log.Information("Sayiad API starting.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}