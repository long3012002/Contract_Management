using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.Services;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Mapper;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

builder.Services.AddControllers();
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
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IBidPackageService, BidPackageService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IResolutionService, ResolutionService>();
builder.Services.AddScoped<IWarningService, WarningService>();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
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
                CanCreate = true,
                CanUpdate = true,
                CanDelete = true
            });

            // Manager: Access, Create, Update
            context.RolePermissions.Add(new demo1.Entity.RolePermission
            {
                RoleId = managerRole.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                CanCreate = true,
                CanUpdate = true,
                CanDelete = false
            });

            // Staff: Access, Create
            context.RolePermissions.Add(new demo1.Entity.RolePermission
            {
                RoleId = staffRole.Id,
                FeatureId = feature.Id,
                CanAccess = true,
                CanCreate = true,
                CanUpdate = false,
                CanDelete = false
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
    }
}

app.Run();
