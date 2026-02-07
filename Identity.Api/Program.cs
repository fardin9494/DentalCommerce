using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Identity.Api.Endpoints;
using Identity.Api.Services;
using Identity.Application.Abstractions;
using Identity.Application.Common.Behaviors;
using Identity.Application.Markers;
using Identity.Application.Options;
using AppSessionOptions = Identity.Application.Options.SessionOptions;
using Identity.Infrastructure.Auth;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Services;
using Identity.Infrastructure.Sms;
using Identity.Infrastructure.Transactions;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<IdentityDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("IdentityDb")
             ?? throw new InvalidOperationException("Connection string 'IdentityDb' is not configured.");
    opt.UseSqlServer(cs, sql =>
    {
        sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.DefaultSchema);
        sql.EnableRetryOnFailure();
    });
});
builder.Services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());
builder.Services.AddScoped<ITransactionRunner, EfTransactionRunner>();

// Clock
builder.Services.AddSingleton<IClock, SystemClock>();

// Options (simple DI-friendly)
var otpOptions = builder.Configuration.GetSection("Otp").Get<OtpOptions>() ?? new OtpOptions();
var sessionOptions = builder.Configuration.GetSection("Session").Get<AppSessionOptions>() ?? new AppSessionOptions();
var passwordOptions = builder.Configuration.GetSection("Password").Get<PasswordOptions>() ?? new PasswordOptions();
var securityOptions = builder.Configuration.GetSection("Security").Get<SecurityOptions>() ?? new SecurityOptions();
var adminOptions = builder.Configuration.GetSection("Admin").Get<AdminOptions>() ?? new AdminOptions();
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var smsOptions = builder.Configuration.GetSection("SmsIr").Get<SmsIrOptions>() ?? new SmsIrOptions();

builder.Services.AddSingleton(otpOptions);
builder.Services.AddSingleton(sessionOptions);
builder.Services.AddSingleton(passwordOptions);
builder.Services.AddSingleton(securityOptions);
builder.Services.AddSingleton(adminOptions);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(smsOptions);

// MediatR + Validation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// JSON enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// SMS sender (sms.ir)
if (!string.IsNullOrWhiteSpace(smsOptions.ApiKey))
{
    builder.Services.AddScoped<ISmsSender>(sp =>
    {
        var opts = sp.GetRequiredService<SmsIrOptions>();
        return new SmsIrSender(opts);
    });
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ISmsSender, DevSmsSender>();
}
else
{
    throw new InvalidOperationException("SmsIr configuration is missing (SmsIr:ApiKey).");
}

// JWT token service + auth
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(15)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
const string DefaultCorsPolicy = "default";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(DefaultCorsPolicy, p =>
    {
        if (builder.Environment.IsDevelopment())
        {
            p.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5174",
                "http://localhost:5175",
                "https://localhost:5175"
            )
             .AllowAnyHeader()
             .AllowAnyMethod();
        }
        else if (allowedOrigins is { Length: > 0 })
        {
            p.WithOrigins(allowedOrigins)
             .AllowAnyHeader()
             .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

await EnsureSuperAdminAsync(app.Services);

app.UseCors(DefaultCorsPolicy);

// Global error handling (like Pricing/Catalog)
var env = app.Services.GetRequiredService<IHostEnvironment>();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (ValidationException ex)
    {
        logger.LogWarning(ex, "Validation failed for request {Path}", ctx.Request.Path);

        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        await Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(ctx);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Bad request (InvalidOperation) for {Path}", ctx.Request.Path);
        var detail = env.IsDevelopment() ? ex.Message : null;
        await Results.Problem(title: "Bad Request", detail: detail, statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(ctx);
    }
    catch (DbUpdateException ex) when (
        ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627)
    )
    {
        logger.LogWarning(ex, "Duplicate key error ({SqlNumber}) for {Path}", sql.Number, ctx.Request.Path);
        var detail = env.IsDevelopment() ? sql.Message : null;
        await Results.Problem(title: "Duplicate key", detail: detail, statusCode: StatusCodes.Status409Conflict).ExecuteAsync(ctx);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception for request {Path}", ctx.Request.Path);
        var detail = env.IsDevelopment() ? ex.Message : null;
        await Results.Problem(title: "Internal Server Error", detail: detail, statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(ctx);
    }
});

app.UseAuthentication();
app.UseAuthorization();

var identity = app.MapGroup("/api/identity").DisableAntiforgery();
identity.MapGet("/auth/check", () => Results.NoContent());
identity.MapAuthEndpoints();
identity.MapUserEndpoints();
identity.MapAdminEndpoints();

app.Run();

static async Task EnsureSuperAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var phone = config.GetSection("Admin")["SuperAdminPhone"];
    if (string.IsNullOrWhiteSpace(phone)) return;

    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var normalized = Identity.Domain.Users.PhoneNumber.Normalize(phone);
    var user = await db.Users.FirstOrDefaultAsync(x => x.PhoneNumber == normalized);
    if (user is null) return;

    var admin = await db.AdminAccounts.FirstOrDefaultAsync(x => x.UserId == user.Id);
    var now = DateTime.UtcNow;
    if (admin is null)
    {
        admin = Identity.Domain.Admin.AdminAccount.Create(user.Id, true, Identity.Domain.Admin.PermissionUiPolicy.Disable, now);
        db.AdminAccounts.Add(admin);
    }
    else if (!admin.IsSuperAdmin)
    {
        admin.PromoteToSuper(now);
    }

    await db.SaveChangesAsync();
}
