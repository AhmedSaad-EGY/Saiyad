import re

with open('Sayiad.API/Program.cs', 'r') as f:
    content = f.read()

# 1. Add AddAntiforgery after AddOpenApi
content = content.replace(
    'builder.Services.AddOpenApi();\n\n    builder.Services.AddDbContext<ApplicationDbContext>',
    '''builder.Services.AddOpenApi();

    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-Token";
        options.Cookie.Name = "XSRF-TOKEN";
        options.Cookie.HttpOnly = false;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    builder.Services.AddDbContext<ApplicationDbContext>'''
)

# 2. Add AutoValidateAntiforgeryToken filter to AddControllers
content = content.replace(
    'builder.Services.AddControllers()\n        .AddJsonOptions',
    '''builder.Services.AddControllers(options =>
        {
            options.Filters.Add<AutoValidateAntiforgeryToken>();
        })
        .AddJsonOptions'''
)

with open('Sayiad.API/Program.cs', 'w') as f:
    f.write(content)

print("Program.cs updated")
