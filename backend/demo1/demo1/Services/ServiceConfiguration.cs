using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.DTOs.Common;
using demo1.Logging;
using demo1.Mapper;
using demo1.Middleware;
using demo1.Providers;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace demo1.Services;

public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        services.AddControllers(options =>
        {
            options.Filters.Add<AuditLogActionFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        services.Configure<ApiBehaviorOptions>(options =>
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

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "CoopBank Contract Management API",
                Version = "v1",
                Description = "Hệ thống API đặc tả & Quản lý Hợp đồng, Gói thầu, Dự án - Ngân hàng Hợp tác xã Việt Nam (CoopBank)",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "CoopBank IT Team",
                    Email = "support@coopbank.vn"
                }
            });

            c.CustomSchemaIds(type => type.FullName ?? type.Name);

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header sử dụng chuẩn Bearer. Ví dụ: \"Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }
        });

        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "Chuoi_Bi_Mat_Sieu_Manh_Voi_Do_Dai_Toi_Thieu_32_Ky_Tu_!!!";
        services.AddAuthentication(options =>
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

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCors", policy =>
            {
                var allowedOrigins = configuration
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

        // Configure Hangfire with PostgreSQL Storage
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
            {
                SchemaName = "public",
                PrepareSchemaIfNecessary = true
            }));

        services.AddHangfireServer();
        services.AddScoped<CongViecReminderHangfireService>();

        services.AddScoped<IDuAnService, DuAnService>();
        services.AddScoped<IDoiTacService, DoiTacService>();
        services.AddScoped<IGoiThauService, GoiThauService>();
        services.AddScoped<ICongViecGoiThauService, CongViecGoiThauService>();
        services.AddScoped<ICommentCongViecGoiThauService, CommentCongViecGoiThauService>();
        services.AddScoped<ILicenseService, LicenseService>();

        services.AddScoped<IHopDongService, HopDongService>();
        services.AddScoped<IDotThanhToanService, DotThanhToanService>();
        services.AddScoped<IResolutionService, ResolutionService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<INhomDuAnService, NhomDuAnService>();
        services.AddScoped<IPhanLoaiDuAnService, PhanLoaiDuAnService>();
        services.AddScoped<IWarningService, WarningService>();
        services.AddScoped<IUserService, UserService>();
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IPhongBanService, PhongBanService>();
        services.AddScoped<IChucVuService, ChucVuService>();
        services.AddScoped<IDonViService, DonViService>();
        services.AddScoped<IXuatXuService, XuatXuService>();
        services.AddScoped<IDonViTinhService, DonViTinhService>();
        services.AddScoped<IHangSanXuatService, HangSanXuatService>();
        services.AddScoped<IHangHoaService, HangHoaService>();
        services.AddScoped<IDichVuService, DichVuService>();
        services.AddScoped<ILicenseLineService, LicenseLineService>();
        services.AddScoped<IHangHoaDichVuService, HangHoaDichVuService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddHttpClient();
        services.AddScoped<IOnlyOfficeService, OnlyOfficeService>();
        services.AddSingleton<TotpService>();
        services.Configure<RadiusSettings>(configuration.GetSection("Radius"));
        services.AddSingleton<RadiusClient>();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHostedService<AuditLogRetentionWorker>();
        services.AddHostedService<ContractScanWorker>();
        services.AddHostedService<StakeholderConfirmationCheckWorker>();
        services.AddHostedService<DotThanhToanScanWorker>();
        services.AddSignalR();
        services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, CustomUserIdProvider>();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
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
                var loginPermitLimit = configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 5);
                var loginWindowMinutes = configuration.GetValue<int>("RateLimiting:Login:WindowMinutes", 1);

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

        return services;
    }
}
