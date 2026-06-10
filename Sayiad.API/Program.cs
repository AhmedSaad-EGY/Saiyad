Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddSwaggerGen();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-Token";
        options.Cookie.Name = "XSRF-TOKEN";
        options.Cookie.HttpOnly = false;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Dev")));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSettings["SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey is missing");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins("https://saiyad-eg.vercel.app")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("auth", limiterOptions =>
        {
            limiterOptions.PermitLimit = 10;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });
        options.RejectionStatusCode = 429;
    });

    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<ICategoryManager, CategoryManager>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IProductManager, ProductManager>();
    builder.Services.AddScoped<ICartRepository, CartRepository>();
    builder.Services.AddScoped<ICartManager, CartManager>();
    builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
    builder.Services.AddScoped<IWishlistManager, WishlistManager>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<IOrderManager, OrderManager>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddScoped<IPaymentManager, PaymentManager>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<INotificationManager, NotificationManager>();
    builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
    builder.Services.AddScoped<IReviewManager, ReviewManager>();
    builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
    builder.Services.AddScoped<IAuctionManager, AuctionManager>();
    builder.Services.AddScoped<IReportRepository, ReportRepository>();
    builder.Services.AddScoped<IReportManager, ReportManager>();
    builder.Services.AddScoped<IShippingAddressRepository, ShippingAddressRepository>();
    builder.Services.AddScoped<IShippingAddressManager, ShippingAddressManager>();
    builder.Services.AddScoped<ISellerProfileRepository, SellerProfileRepository>();
    builder.Services.AddScoped<ISellerProfileManager, SellerProfileManager>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    builder.Services.AddScoped<ISubscriptionManager, SubscriptionManager>();
    builder.Services.AddScoped<IWalletRepository, WalletRepository>();
    builder.Services.AddScoped<IWalletManager, WalletManager>();
    builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
    builder.Services.AddScoped<ISubscriptionPlanManager, SubscriptionPlanManager>();

    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
    builder.Services.AddScoped<IAuthManager, AuthManager>();
    builder.Services.AddScoped<IUserManager, UserManager>();
    builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddSignalR();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            builder.Configuration.GetConnectionString("Dev")!,
            name: "database",
            tags: ["db", "sql"]);
    builder.Services.AddHostedService<AuctionExpiryService>();
    builder.Services.AddTransient<Sayiad.Api.Middleware.ExceptionMiddleware>();
    builder.Services.AddTransient<Sayiad.Api.Middleware.CsrfValidationMiddleware>();

    var app = builder.Build();

    // Apply pending EF migrations on startup in production
    if (app.Environment.IsProduction())
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply database migrations. App will continue but some features may not work.");
        }
    }

    // Ensure admin user has a wallet (platform wallet for fees)
    try
    {
        using var scope = app.Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var walletManager = scope.ServiceProvider.GetRequiredService<IWalletManager>();
        var admin = await userRepo.GetByEmailAsync("sayiadapp@gmail.com");
        if (admin != null)
        {
            await walletManager.CreateWalletAsync(admin.Id);
            Log.Information("Admin wallet ensured for {Email}", admin.Email);
        }
        else
        {
            Log.Warning("Admin user {Email} not found — platform wallet not created", "sayiadapp@gmail.com");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to ensure admin wallet on startup");
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<Sayiad.Api.Middleware.InputSanitizationMiddleware>();
    app.UseMiddleware<Sayiad.Api.Middleware.RequestLoggingMiddleware>();
    app.UseMiddleware<Sayiad.Api.Middleware.ExceptionMiddleware>();

    //if (app.Environment.IsDevelopment())
    //{
        app.UseSwagger();
        app.UseSwaggerUI();
    //}

    app.UseStaticFiles();
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<Sayiad.Api.Middleware.CsrfValidationMiddleware>();
    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapHub<AuctionHub>("/hubs/auction");

    Log.Information("Sayiad API starting");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
