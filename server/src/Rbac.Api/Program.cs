using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Rbac.Api.Authentication;
using Rbac.Api.Authorization;
using Rbac.Api.Common;
using Rbac.Api.Data;
using Rbac.Api.Data.Seeding;
using Rbac.Api.Domain;
using Rbac.Api.Features.Auth;
using Rbac.Api.Features.Invoices;
using Rbac.Api.Features.Roles;
using Rbac.Api.Features.Users;
using Rbac.Api.Realtime;

var builder = WebApplication.CreateBuilder(args);

// ---------- Web API ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddCors(options => options.AddPolicy("Client", policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Configuration.GetValue("RateLimiting:AuthenticationPermitLimit", 10);
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Authentication, context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1) }));
});

// ---------- Data and identity ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// ---------- Authentication (who are you?) ----------
builder.Services.AddJwtAuthentication();
builder.Services.AddSingleton<ITokenService, TokenService>();

// ---------- Authorization (what may you do?) ----------
builder.Services.AddAuthorization();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<PrivilegeEscalationGuard>();

// ---------- Real-time ----------
builder.Services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
builder.Services.AddScoped<IPermissionChangeNotifier, PermissionChangeNotifier>();

// ---------- Features ----------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:SeedOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // EnsureCreated keeps the sample zero-setup. Use EF Core migrations in a real application.
    await dbContext.Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Order matters: routing -> CORS -> rate limiting -> authentication -> authorization -> endpoints.
app.UseRouting();
app.UseCors("Client");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationsHub>(NotificationsHub.Path);

app.Run();

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program
{
}
