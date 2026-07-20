using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.Services;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Mapper;
using demo1.Middleware;
using demo1.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using demo1.Logging;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFile(Path.Combine(builder.Environment.ContentRootPath, "debug.log"));


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
}

var mysqlServerVersion = Version.Parse(builder.Configuration["Database:ServerVersion"] ?? "8.0.36");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(mysqlServerVersion)));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<demo1.Middleware.AuditLogActionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                item => item.Key,
                item => item.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new ApiErrorResponse
        {
            Message = "Validation failed.",
            Errors = errors
        });
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "Chuoi_Bi_Mat_Sieu_Manh_Voi_Do_Dai_Toi_Thieu_32_Ky_Tu_!!!";
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
        ValidIssuer = jwtSettings["Issuer"] ?? "ContractManagementBackend",
        ValidAudience = jwtSettings["Audience"] ?? "ContractManagementFrontend",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hub/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(origin => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

    builder.Services.AddScoped<IDuAnService, DuAnService>();
    builder.Services.AddScoped<IDoiTacService, DoiTacService>();
    builder.Services.AddScoped<IGoiThauService, GoiThauService>();
    builder.Services.AddScoped<ICongViecGoiThauService, CongViecGoiThauService>();
    builder.Services.AddScoped<IHopDongService, HopDongService>();
    builder.Services.AddScoped<IResolutionService, ResolutionService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<INhomDuAnService, NhomDuAnService>();
    builder.Services.AddScoped<IPhanLoaiDuAnService, PhanLoaiDuAnService>();
    builder.Services.AddScoped<IWarningService, WarningService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.Configure<demo1.DTOs.Common.EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPhongBanService, PhongBanService>();
builder.Services.AddScoped<IChucVuService, ChucVuService>();
builder.Services.AddSingleton<TotpService>();
builder.Services.AddSingleton<RadiusClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var server = config["Radius:Server"] ?? "127.0.0.1";
    var port = int.TryParse(config["Radius:Port"], out var parsedPort) ? parsedPort : 1812;
    var sharedSecret = config["Radius:SharedSecret"] ?? string.Empty;
    var timeout = int.TryParse(config["Radius:Timeout"], out var parsedTimeout) ? parsedTimeout : 3000;

    return new RadiusClient(server, port, sharedSecret, timeout);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHostedService<AuditLogRetentionWorker>();
builder.Services.AddHostedService<ContractScanWorker>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, demo1.Providers.CustomUserIdProvider>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Message = "Quá nhiều yêu cầu đăng nhập. Vui lòng thử lại sau 1 phút.",
            Detail = "Too many login attempts. Please try again after 1 minute."
        }, token);
    };

    options.AddPolicy("LoginPolicy", httpContext =>
    {
        var loginPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 5);
        var loginWindowMinutes = builder.Configuration.GetValue<int>("RateLimiting:Login:WindowMinutes", 1);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = loginPermitLimit,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(loginWindowMinutes)
            });
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("DefaultCors");
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<demo1.Hubs.NotificationHub>("/hub/notifications");

if (app.Configuration.GetValue<bool>("Database:AutoMigrate") ||
    app.Configuration.GetValue<bool>("Database:SeedSampleData"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // context.Database.EnsureDeleted(); // Removed to prevent wiping data on restart
    context.Database.EnsureCreated();

    if (!context.Features.Any())
    {
        // 1. Seed Features
        var features = new List<demo1.Entity.Feature>
        {
            new() { Code = "PROJECT", Name = "Quản lý dự án", Description = "Chức năng xem, thêm, sửa, xoá dự án" },
            new() { Code = "BID_PACKAGE", Name = "Quản lý gói thầu", Description = "Chức năng xem, thêm, sửa, xoá gói thầu" },
            new() { Code = "CONTRACT", Name = "Quản lý hợp đồng", Description = "Chức năng xem, thêm, sửa, xoá hợp đồng" },
            new() { Code = "PARTNER", Name = "Quản lý đối tác", Description = "Chức năng xem, thêm, sửa, xoá đối tác" },
            new() { Code = "RESOLUTION", Name = "Quản lý nghị quyết/văn bản", Description = "Chức năng xem, thêm, sửa, xoá nghị quyết" }
        };
        context.Features.AddRange(features);
        context.SaveChanges();

        // 2. Seed Roles
        var adminRole = new demo1.Entity.Role { Name = "Admin", Description = "Quyền quản trị toàn hệ thống" };
        var managerRole = new demo1.Entity.Role { Name = "Manager", Description = "Quản lý dự án, hợp đồng" };
        var staffRole = new demo1.Entity.Role { Name = "Staff", Description = "Nhân viên xem và cập nhật thông tin" };
        context.Roles.AddRange(adminRole, managerRole, staffRole);
        context.SaveChanges();

        // 3. Seed RolePermissions
        foreach (var feature in features)
        {
            // Admin: Full permissions
            context.RolePermissions.Add(new demo1.Entity.RolePermission
            {
                RoleId = adminRole.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "Create;Update;Delete"
            });

            // Manager: Access, Create, Update
            context.RolePermissions.Add(new demo1.Entity.RolePermission
            {
                RoleId = managerRole.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "Create;Update"
            });

            // Staff: Access, Create
            context.RolePermissions.Add(new demo1.Entity.RolePermission
            {
                RoleId = staffRole.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                Permissions = "Create"
            });
        }
        context.SaveChanges();

        // 4. Seed Admin User
        var adminUser = new demo1.Entity.User
        {
            Username = "admin",
            FullName = "System Administrator",
            IsActive = true,
            IsSystemAdmin = true
        };
        var normalUser = new demo1.Entity.User
        {
            Username = "quangmd",
            FullName = "Mai Duy Quang",
            IsActive = true,
            IsSystemAdmin = true // also system admin for testing
        };
        context.Users.AddRange(adminUser, normalUser);
        context.SaveChanges();

        // Assign Admin role to normalUser (though system admin bypasses permission check)
        context.UserRoles.Add(new demo1.Entity.UserRole
        {
            UserId = normalUser.Id,
            RoleId = adminRole.Id
        });
        context.SaveChanges();

        // Check and seed user anhld2 if not exists
        if (!context.Users.Any(u => u.Username == "anhld2"))
        {
            var anhldUser = new demo1.Entity.User
            {
                Id = Guid.NewGuid(),
                Username = "anhld2",
                FullName = "Lê Đức Anh",
                IsActive = true,
                IsSystemAdmin = true,
                IsTwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(anhldUser);
            context.SaveChanges();
        }
    }

    if (app.Configuration.GetValue<bool>("Database:SeedSampleData"))
    {
        await DatabaseSeeder.SeedAsync(context);
    }
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS `CongViecGoiThaus` (
                `Id` char(36) COLLATE ascii_general_ci NOT NULL,
                `GoiThauId` char(36) COLLATE ascii_general_ci NOT NULL,
                `Stt` int NOT NULL,
                `TenTaiLieu` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                `NgayKy` datetime(6) NULL,
                `LoaiVanBan` varchar(100) CHARACTER SET utf8mb4 NULL,
                `TinhTrang` varchar(100) CHARACTER SET utf8mb4 NULL,
                `GhiChu` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `Code` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
                `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
                `Description` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `IsActive` tinyint(1) NOT NULL,
                `CreatedAt` datetime(6) NOT NULL,
                `UpdatedAt` datetime(6) NULL,
                CONSTRAINT `PK_CongViecGoiThaus` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_CongViecGoiThaus_GoiThaus_GoiThauId` FOREIGN KEY (`GoiThauId`) REFERENCES `GoiThaus` (`Id`) ON DELETE CASCADE
            ) CHARACTER SET=utf8mb4;
        ");
        try { context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX `IX_CongViecGoiThaus_Code` ON `CongViecGoiThaus` (`Code`);"); } catch { }
        try { context.Database.ExecuteSqlRaw("CREATE INDEX `IX_CongViecGoiThaus_GoiThauId` ON `CongViecGoiThaus` (`GoiThauId`);"); } catch { }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Migration] CongViecGoiThaus table init error: {ex.Message}");
    }
}

app.Run();
