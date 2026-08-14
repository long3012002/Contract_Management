using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using demo1.Hubs;

namespace demo1.Services.Workers
{
    public class ContractScanWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContractScanWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ContractScanWorker(
            IServiceProvider serviceProvider,
            ILogger<ContractScanWorker> logger,
            IConfiguration configuration,
            IHubContext<NotificationHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ContractScanWorker started.");

            // Run initial scan immediately upon startup
            try
            {
                await ScanAndNotifyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during initial contract expiration scan on startup.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1); // Next midnight (0h)
                var delay = nextRun - now;
                if (delay.TotalMilliseconds <= 0)
                {
                    delay = TimeSpan.FromHours(24);
                }

                _logger.LogInformation("Next contract scan scheduled at {NextRun} (in {DelayHours:F2} hours).", nextRun, delay.TotalHours);

                try
                {
                    var testIntervalMinutes = _configuration.GetValue<int?>("ContractScan:TestIntervalMinutes");
                    if (testIntervalMinutes.HasValue && testIntervalMinutes.Value > 0)
                    {
                        _logger.LogInformation("Testing mode enabled: running contract scan every {Minutes} minutes.", testIntervalMinutes.Value);
                        await Task.Delay(TimeSpan.FromMinutes(testIntervalMinutes.Value), stoppingToken);
                        await ScanAndNotifyAsync();
                        continue;
                    }
                    
                    await Task.Delay(delay, stoppingToken);
                    await ScanAndNotifyAsync();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("ContractScanWorker is stopping.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during contract expiration scan.");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task ScanAndNotifyAsync()
        {
            _logger.LogInformation("Starting contract expiration scan...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var today = DateTime.Today;

            var warnDaysBefore = _configuration.GetValue<int>("ContractScan:WarnDaysBefore", 30);
            var intervalDays = _configuration.GetValue<int>("ContractScan:NotificationIntervalDays", 1);

            // Fetch active contracts with related GoiThau & DuAn
            var expiringContracts = await dbContext.HopDongs
                .Include(h => h.DuAn)
                .Include(h => h.GoiThau)
                    .ThenInclude(g => g!.DuAn)
                .Where(h => h.IsActive && h.ExpiredDate.HasValue)
                .ToListAsync();

            // Filter contracts expiring in <= warnDaysBefore days OR already expired (< 0)
            var contractsToWarn = expiringContracts
                .Where(h =>
                {
                    var daysRemaining = (h.ExpiredDate!.Value.Date - today).Days;
                    return daysRemaining <= warnDaysBefore;
                })
                .ToList();

            var notificationsToPush = new List<(string Username, Notification Notification)>();

            if (contractsToWarn.Any())
            {
                _logger.LogInformation("Found {Count} contracts expiring soon (threshold: {WarnDays} days) or already expired.", contractsToWarn.Count, warnDaysBefore);

                foreach (var contract in contractsToWarn)
                {
                    var daysRemaining = (contract.ExpiredDate!.Value.Date - today).Days;
                    _logger.LogInformation("[ContractScan] Phát hiện hợp đồng: Mã={Code}, Tên={Name}, Hạn dùng={ExpiredDate:dd/MM/yyyy}, Số ngày còn lại={DaysRemaining}", contract.Code, contract.Name, contract.ExpiredDate.Value, daysRemaining);
                    
                    string title;
                    string content;

                    if (daysRemaining < 0)
                    {
                        var daysOverdue = Math.Abs(daysRemaining);
                        title = "Hợp đồng: Đã hết hạn";
                        content = $"Hợp đồng '{contract.Name}' (Mã: {contract.Code}) đã hết hạn {daysOverdue} ngày (ngày hết hạn: {contract.ExpiredDate.Value:dd/MM/yyyy}).";
                    }
                    else if (daysRemaining == 0)
                    {
                        title = "Hợp đồng: Hết hạn hôm nay";
                        content = $"Hợp đồng '{contract.Name}' (Mã: {contract.Code}) hết hạn hôm nay ({contract.ExpiredDate.Value:dd/MM/yyyy}).";
                    }
                    else
                    {
                        title = "Hợp đồng: Sắp hết hạn";
                        content = $"Hợp đồng '{contract.Name}' (Mã: {contract.Code}) sắp hết hạn (còn {daysRemaining} ngày, hạn: {contract.ExpiredDate.Value:dd/MM/yyyy}).";
                    }

                    var link = $"/contracts/{contract.Id}";

                    // Get target users (Creators, Modifiers, Viewers/Editors, Project Owner, System Admins)
                    var targetUsers = await GetTargetUsersForContractAsync(dbContext, contract);

                    foreach (var user in targetUsers)
                    {
                        bool alreadyNotified;
                        if (intervalDays <= 0)
                        {
                            alreadyNotified = await dbContext.Notifications
                                .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title);
                        }
                        else
                        {
                            var cutoffDate = DateTime.UtcNow.Date.AddDays(-(intervalDays - 1));
                            alreadyNotified = await dbContext.Notifications
                                .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title && n.CreatedAt >= cutoffDate);
                        }

                        if (alreadyNotified)
                        {
                            continue;
                        }

                        _logger.LogInformation("[ContractScan] Đang tạo thông báo hệ thống cho user {Username} về hợp đồng {Code}", user.Username, contract.Code);

                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = title,
                            Content = content,
                            Link = link,
                            FeatureCode = "QUAN_LY_HOP_DONG",
                            EntityName = "HopDong",
                            EntityId = contract.Id.ToString(),
                            UserId = user.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Notifications.Add(notification);
                        notificationsToPush.Add((user.Username, notification));
                    }
                }
            }

            // Scan Licenses expiring soon or already expired
            var activeLicenses = await dbContext.Licenses
                .Include(l => l.DuAn)
                .Where(l => l.IsActive && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue)
                .ToListAsync();

            var licensesToWarn = activeLicenses
                .Where(l =>
                {
                    var daysRemaining = (l.NgayKetThuc!.Value.Date - today).Days;
                    return daysRemaining <= l.CanhBaoTruocNgay;
                })
                .ToList();

            if (licensesToWarn.Any())
            {
                _logger.LogInformation("Found {Count} licenses expiring or already expired.", licensesToWarn.Count);
                foreach (var license in licensesToWarn)
                {
                    var daysRemaining = (license.NgayKetThuc!.Value.Date - today).Days;
                    _logger.LogInformation("[LicenseScan] Phát hiện License: Mã={Code}, Tên={Name}, Hạn dùng={ExpiredDate:dd/MM/yyyy}, Số ngày còn lại={DaysRemaining}", license.Code, license.Name, license.NgayKetThuc.Value, daysRemaining);

                    string title;
                    string content;

                    if (daysRemaining < 0)
                    {
                        var daysOverdue = Math.Abs(daysRemaining);
                        title = "License: Đã hết hạn";
                        content = $"License '{license.Name}' (Mã: {license.Code}) đã hết hạn {daysOverdue} ngày (ngày hết hạn: {license.NgayKetThuc.Value:dd/MM/yyyy}).";
                    }
                    else if (daysRemaining == 0)
                    {
                        title = "License: Hết hạn hôm nay";
                        content = $"License '{license.Name}' (Mã: {license.Code}) hết hạn hôm nay ({license.NgayKetThuc.Value:dd/MM/yyyy}).";
                    }
                    else
                    {
                        title = "License: Sắp hết hạn";
                        content = $"License '{license.Name}' (Mã: {license.Code}) sắp hết hạn (còn {daysRemaining} ngày).";
                    }

                    var link = $"/licenses/{license.Id}";

                    // Determine target users for License (Project Owner, System Admins, Viewers/Editors)
                    var targetUsers = await GetTargetUsersForLicenseAsync(dbContext, license);

                    foreach (var user in targetUsers)
                    {
                        var alreadyNotified = await dbContext.Notifications
                            .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title);

                        if (alreadyNotified) continue;

                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = title,
                            Content = content,
                            Link = link,
                            FeatureCode = "LICENSE",
                            EntityName = "License",
                            EntityId = license.Id.ToString(),
                            UserId = user.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Notifications.Add(notification);
                        notificationsToPush.Add((user.Username, notification));
                    }
                }
            }

            // Track IDs of linked Licenses to prevent duplicate notifications
            var processedLicenseIds = new HashSet<Guid>(activeLicenses.Select(l => l.Id));

            // Scan HangHoaDichVu lines of type License that are NOT linked to a dedicated License entity (IdLicense == null or not in processedLicenseIds)
            var activeHangHoaLicenses = await dbContext.HangHoaDichVus
                .Where(h => h.IsActive && h.Loai == LoaiHangHoaDichVu.License && h.NgayKetThuc.HasValue && (h.IdLicense == null || !processedLicenseIds.Contains(h.IdLicense.Value)))
                .ToListAsync();

            var hangHoaLicensesToWarn = activeHangHoaLicenses
                .Where(h =>
                {
                    var daysRemaining = (h.NgayKetThuc!.Value.Date - today).Days;
                    return daysRemaining <= warnDaysBefore;
                })
                .ToList();

            if (hangHoaLicensesToWarn.Any())
            {
                _logger.LogInformation("Found {Count} unlinked HangHoaDichVu licenses expiring or already expired.", hangHoaLicensesToWarn.Count);

                var parentHopDongIds = hangHoaLicensesToWarn.Select(h => h.IdParent).Distinct().ToList();
                var parentHopDongs = await dbContext.HopDongs
                    .Include(h => h.DuAn)
                    .Include(h => h.GoiThau)
                        .ThenInclude(g => g!.DuAn)
                    .Where(h => parentHopDongIds.Contains(h.Id))
                    .ToDictionaryAsync(h => h.Id);

                foreach (var hhh in hangHoaLicensesToWarn)
                {
                    var daysRemaining = (hhh.NgayKetThuc!.Value.Date - today).Days;
                    var licenseName = !string.IsNullOrWhiteSpace(hhh.TenDichVu)
                        ? hhh.TenDichVu
                        : (!string.IsNullOrWhiteSpace(hhh.DanhMucHangHoa) ? hhh.DanhMucHangHoa : hhh.KyMaHieu ?? "License Hợp đồng");

                    _logger.LogInformation("[HangHoaLicenseScan] Phát hiện License Hợp đồng: ID={Id}, Tên={Name}, Hạn dùng={ExpiredDate:dd/MM/yyyy}, Số ngày còn lại={DaysRemaining}", hhh.Id, licenseName, hhh.NgayKetThuc.Value, daysRemaining);

                    string title;
                    string content;

                    if (daysRemaining < 0)
                    {
                        var daysOverdue = Math.Abs(daysRemaining);
                        title = "License Hợp đồng: Đã hết hạn";
                        content = $"License '{licenseName}' thuộc Hợp đồng đã hết hạn {daysOverdue} ngày (ngày hết hạn: {hhh.NgayKetThuc.Value:dd/MM/yyyy}).";
                    }
                    else if (daysRemaining == 0)
                    {
                        title = "License Hợp đồng: Hết hạn hôm nay";
                        content = $"License '{licenseName}' thuộc Hợp đồng hết hạn hôm nay ({hhh.NgayKetThuc.Value:dd/MM/yyyy}).";
                    }
                    else
                    {
                        title = "License Hợp đồng: Sắp hết hạn";
                        content = $"License '{licenseName}' thuộc Hợp đồng sắp hết hạn (còn {daysRemaining} ngày, hạn: {hhh.NgayKetThuc.Value:dd/MM/yyyy}).";
                    }

                    var link = $"/contracts/{hhh.IdParent}";

                    List<User> targetUsers;
                    if (parentHopDongs.TryGetValue(hhh.IdParent, out var contract))
                    {
                        targetUsers = await GetTargetUsersForContractAsync(dbContext, contract);
                    }
                    else
                    {
                        targetUsers = await dbContext.Users.AsNoTracking().Where(u => u.IsActive && u.IsSystemAdmin).ToListAsync();
                    }

                    foreach (var user in targetUsers)
                    {
                        bool alreadyNotified;
                        if (intervalDays <= 0)
                        {
                            alreadyNotified = await dbContext.Notifications
                                .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title);
                        }
                        else
                        {
                            var cutoffDate = DateTime.UtcNow.Date.AddDays(-(intervalDays - 1));
                            alreadyNotified = await dbContext.Notifications
                                .AnyAsync(n => n.UserId == user.Id && n.Link == link && n.Title == title && n.CreatedAt >= cutoffDate);
                        }

                        if (alreadyNotified) continue;

                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            Title = title,
                            Content = content,
                            Link = link,
                            FeatureCode = "QUAN_LY_HOP_DONG",
                            EntityName = "HangHoaDichVu",
                            EntityId = hhh.Id.ToString(),
                            UserId = user.Id,
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        dbContext.Notifications.Add(notification);
                        notificationsToPush.Add((user.Username, notification));
                    }
                }
            }

            if (notificationsToPush.Any())
            {
                await dbContext.SaveChangesAsync();

                foreach (var item in notificationsToPush)
                {
                    _logger.LogInformation("[ContractScan] Đang push realtime thông báo cho user {Username} qua SignalR", item.Username);
                    await _hubContext.Clients.User(item.Username).SendAsync("ReceiveNotification", new
                    {
                        id = item.Notification.Id,
                        title = item.Notification.Title,
                        content = item.Notification.Content,
                        link = item.Notification.Link,
                        isRead = item.Notification.IsRead,
                        createdAt = item.Notification.CreatedAt
                    });
                }
            }

            _logger.LogInformation("Finished contract & license expiration scan. Generated {Count} notifications.", notificationsToPush.Count);
        }

        public static async Task<List<User>> GetTargetUsersForContractAsync(AppDbContext dbContext, HopDong contract)
        {
            var targetUserIds = new HashSet<Guid>();

            // 1. System Admins
            var adminIds = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsSystemAdmin)
                .Select(u => u.Id)
                .ToListAsync();
            foreach (var id in adminIds) targetUserIds.Add(id);

            // Get associated DuAnId if available
            Guid? duAnId = contract.DuAnId;
            if (!duAnId.HasValue && contract.GoiThau != null)
            {
                duAnId = contract.GoiThau.DuAnId;
            }

            // 2. Project Owner / Creator
            if (duAnId.HasValue && duAnId.Value != Guid.Empty)
            {
                var duAn = await dbContext.DuAns.AsNoTracking().FirstOrDefaultAsync(d => d.Id == duAnId.Value);
                if (duAn?.CreatedByUserId != null && duAn.CreatedByUserId.Value != Guid.Empty)
                {
                    targetUserIds.Add(duAn.CreatedByUserId.Value);
                }
            }

            // 3. Creators / Modifiers from AuditLogs for this contract
            var contractIdStr = contract.Id.ToString();
            var auditUserStrIds = await dbContext.AuditLogs
                .AsNoTracking()
                .Where(a => (a.TableName == "HopDongs" || a.TableName == "HopDong") && a.EntityId == contractIdStr && a.UserId != null)
                .Select(a => a.UserId!)
                .Distinct()
                .ToListAsync();
            foreach (var uidStr in auditUserStrIds)
            {
                if (Guid.TryParse(uidStr, out var parsedGuid))
                {
                    targetUserIds.Add(parsedGuid);
                }
            }

            // 4. Users with explicit permissions in UserPermissions for this contract or project
            var permissionUserIds = await dbContext.UserPermissions
                .AsNoTracking()
                .Where(up => (duAnId.HasValue && up.DuAnId == duAnId.Value) || 
                             (up.EntityName == "HopDong" && up.EntityId == contractIdStr))
                .Select(up => up.UserId)
                .Distinct()
                .ToListAsync();
            foreach (var id in permissionUserIds) targetUserIds.Add(id);

            // 5. Related users (stakeholders) on tasks of the project
            if (duAnId.HasValue && duAnId.Value != Guid.Empty)
            {
                var stakeholderUserIds = await dbContext.CongViecNguoiLienQuans
                    .AsNoTracking()
                    .Where(n => n.CongViecGoiThau != null && n.CongViecGoiThau.GoiThau != null && n.CongViecGoiThau.GoiThau.DuAnId == duAnId.Value)
                    .Select(n => n.UserId)
                    .Distinct()
                    .ToListAsync();
                foreach (var id in stakeholderUserIds) targetUserIds.Add(id);
            }

            var targetUsers = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.IsActive && targetUserIds.Contains(u.Id))
                .ToListAsync();

            // Fallback: If no specific target users found (e.g. minimal seed data), notify active users
            if (!targetUsers.Any())
            {
                targetUsers = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive)
                    .ToListAsync();
            }

            return targetUsers;
        }

        private static async Task<List<User>> GetTargetUsersForLicenseAsync(AppDbContext dbContext, License license)
        {
            var targetUserIds = new HashSet<Guid>();

            // 1. System Admins
            var adminIds = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsSystemAdmin)
                .Select(u => u.Id)
                .ToListAsync();
            foreach (var id in adminIds) targetUserIds.Add(id);

            // 2. Project Owner
            if (license.DuAnId != Guid.Empty)
            {
                var duAn = await dbContext.DuAns.AsNoTracking().FirstOrDefaultAsync(d => d.Id == license.DuAnId);
                if (duAn?.CreatedByUserId != null && duAn.CreatedByUserId.Value != Guid.Empty)
                {
                    targetUserIds.Add(duAn.CreatedByUserId.Value);
                }
            }

            // 3. AuditLog Creators / Modifiers
            var licenseIdStr = license.Id.ToString();
            var auditUserStrIds = await dbContext.AuditLogs
                .AsNoTracking()
                .Where(a => (a.TableName == "Licenses" || a.TableName == "License") && a.EntityId == licenseIdStr && a.UserId != null)
                .Select(a => a.UserId!)
                .Distinct()
                .ToListAsync();
            foreach (var uidStr in auditUserStrIds)
            {
                if (Guid.TryParse(uidStr, out var parsedGuid))
                {
                    targetUserIds.Add(parsedGuid);
                }
            }

            // 4. UserPermissions
            var permissionUserIds = await dbContext.UserPermissions
                .AsNoTracking()
                .Where(up => up.DuAnId == license.DuAnId || (up.EntityName == "License" && up.EntityId == licenseIdStr))
                .Select(up => up.UserId)
                .Distinct()
                .ToListAsync();
            foreach (var id in permissionUserIds) targetUserIds.Add(id);

            var targetUsers = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.IsActive && targetUserIds.Contains(u.Id))
                .ToListAsync();

            if (!targetUsers.Any())
            {
                targetUsers = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive)
                    .ToListAsync();
            }

            return targetUsers;
        }
    }
}
