using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using EverestRemit.Api.Data;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using EverestRemit.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using EverestRemit.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentityCore<ApplicationUser>(o =>
{
    o.User.RequireUniqueEmail = true; o.Password.RequiredLength = 10; o.Password.RequireDigit = true;
    o.Password.RequireUppercase = true; o.Password.RequireLowercase = true; o.Password.RequireNonAlphanumeric = true;
    o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager();
var secret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(secret) || Encoding.UTF8.GetByteCount(secret) < 32) throw new InvalidOperationException("Set Jwt:Secret using user-secrets/environment variables; minimum 32 UTF-8 bytes.");
var issuer = builder.Configuration["Jwt:Issuer"] ?? "EverestRemit";
var audience = builder.Configuration["Jwt:Audience"] ?? "EverestRemitUsers";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = audience, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30) };
});
builder.Services.AddAuthorization(o => { o.AddPolicy("AdminOnly", p => p.RequireRole("Admin")); o.AddPolicy("CustomerOnly", p => p.RequireRole("User")); });
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddFixedWindowLimiter("api", x => { x.PermitLimit = 120; x.Window = TimeSpan.FromMinutes(1); x.QueueLimit = 0; }); });
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddScoped<IBeneficiaryService, BeneficiaryService>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddScoped<IDepositService, DepositService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<DepositFileStorage>();
builder.Services.AddScoped<KycFileStorage>();
builder.Services.AddControllers(); builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EverestRemit API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => { if (origins.Length > 0) p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod(); else if (builder.Environment.IsDevelopment()) p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod(); }));
var app = builder.Build(); 
//if (app.Environment.IsDevelopment()) 
//{ 
    app.UseSwagger(); 
    app.UseSwaggerUI();
//}
app.UseHttpsRedirection();
await RoleSeeder.SeedAsync(app.Services);
app.UseCors("Frontend"); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers().RequireRateLimiting("api");
using (var scope = app.Services.CreateScope()) { var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(); foreach (var role in new[] { "User", "Admin" }) if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role)); var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>(); var email = cfg["BootstrapAdmin:Email"]; var password = cfg["BootstrapAdmin:Password"]; if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)) { var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var admin = await um.FindByEmailAsync(email); if (admin is null) { admin = new ApplicationUser { UserName = email, Email = email, FullName = "System Administrator", EmailConfirmed = true }; var r = await um.CreateAsync(admin, password); if (r.Succeeded) await um.AddToRoleAsync(admin, "Admin"); else app.Logger.LogError("Admin bootstrap failed: {Errors}", string.Join("; ", r.Errors.Select(e => e.Description))); } else if (!await um.IsInRoleAsync(admin, "Admin")) await um.AddToRoleAsync(admin, "Admin"); } }
app.Run();