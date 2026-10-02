using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Hubs;
using demo1.Services.Interfaces;
using demo1.Services.Interfaces.SubServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using demo1.Services.Helpers;

namespace demo1.Services.Implements.SubServices;

public class DuAnNotificationService : IDuAnNotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IEmailService _emailService;
    private readonly ISystemConfigService? _systemConfig;
    private readonly ILogger<DuAnNotificationService> _logger;

    public DuAnNotificationService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IHubContext<NotificationHub> hubContext,
        IEmailService emailService,
        ILogger<DuAnNotificationService> logger,
        ISystemConfigService? systemConfig = null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hubContext = hubContext;
        _emailService = emailService;
        _logger = logger;
        _systemConfig = systemConfig;
    }

    public async Task NotifyProjectOwnerAssignedAsync(DuAn project, Guid newOwnerId, string? actorName = null)
    {
        try
        {
            var newOwner = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == newOwnerId && u.IsActive);
            if (newOwner == null) return;

            // 1. Gửi thông báo trong hệ thống (Notification CSDL & Realtime Hub)
            var displayActor = actorName ?? "Hệ thống";
            var newOwnerNotification = NotificationBuilder.Create()
                .WithTitle("Được phân công làm Chủ dự án")
                .WithContent($"{displayActor} đã phân công bạn làm Chủ dự án '{project.Name}' ({project.Code}).")
                .WithLink($"/du-an/{project.Id}")
                .WithFeatureCode("DU_AN")
                .WithEntity("DuAn", project.Id.ToString())
                .ForUser(newOwnerId)
                .WithActor(displayActor)
                .WithTarget(project.Name)
                .WithBadge("Bổ nhiệm", "info")
                .WithMessage("đã phân công bạn làm Chủ dự án của")
                .Build();

            _dbContext.Notifications.Add(newOwnerNotification);
            await _dbContext.SaveChangesAsync();

            var newOwnerDto = NotificationMapper.MapToDto(newOwnerNotification);
            await _hubContext.Clients.User(newOwner.Username).SendAsync("ReceiveNotification", newOwnerDto);

            // 2. Gửi Email thông báo tới Chủ dự án (nếu không bật chế độ Chỉ gửi cảnh báo đến hạn)
            if (!string.IsNullOrWhiteSpace(newOwner.Email))
            {
                if (_systemConfig != null)
                {
                    var emailEnabled = await _systemConfig.GetBoolAsync("Email:Enabled", true);
                    var onlyExpiry = await _systemConfig.GetBoolAsync("Email:OnlySendExpiryAlerts", true);
                    if (!emailEnabled || onlyExpiry)
                    {
                        return;
                    }
                }

                var subject = $"[CoopBank QLDA] Thông báo phân công Chủ dự án: {project.Name}";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px;'>
                        <h2 style='color: #005a9e; border-bottom: 2px solid #005a9e; padding-bottom: 8px;'>Thông báo Phân công Chủ dự án</h2>
                        <p>Kính gửi <strong>{newOwner.FullName ?? newOwner.Username}</strong>,</p>
                        <p>Bạn vừa được phân công làm <strong>Chủ dự án</strong> cho dự án dưới đây trên Hệ thống Quản lý Hợp đồng & Dự án Co-opBank:</p>
                        <table style='border-collapse: collapse; width: 100%; margin: 15px 0; font-size: 14px;'>
                            <tr style='background-color: #f9f9f9;'>
                                <td style='padding: 10px; font-weight: bold; border: 1px solid #e0e0e0; width: 35%;'>Mã dự án:</td>
                                <td style='padding: 10px; border: 1px solid #e0e0e0; color: #005a9e; font-weight: bold;'>{project.Code}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; font-weight: bold; border: 1px solid #e0e0e0;'>Tên dự án:</td>
                                <td style='padding: 10px; border: 1px solid #e0e0e0;'>{project.Name}</td>
                            </tr>
                            <tr style='background-color: #f9f9f9;'>
                                <td style='padding: 10px; font-weight: bold; border: 1px solid #e0e0e0;'>Người phân công:</td>
                                <td style='padding: 10px; border: 1px solid #e0e0e0;'>{actorName ?? "Hệ thống"}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; font-weight: bold; border: 1px solid #e0e0e0;'>Thời gian thực hiện:</td>
                                <td style='padding: 10px; border: 1px solid #e0e0e0;'>{DateTime.Now:dd/MM/yyyy HH:mm}</td>
                            </tr>
                        </table>
                        <p>Vui lòng đăng nhập hệ thống để theo dõi chi tiết và quản lý tiến độ thực hiện dự án.</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 25px 0 15px;' />
                        <p style='font-size: 12px; color: #777;'>Email này được gửi tự động từ Hệ thống Quản lý Dự án & Hợp đồng - Ngân hàng Hợp tác xã Việt Nam (Co-opBank).</p>
                    </div>";

                await _emailService.SendEmailAsync(newOwner.Email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi gửi thông báo / email phân công Chủ dự án cho ProjectId: {ProjectId}, OwnerId: {OwnerId}", project.Id, newOwnerId);
        }
    }

    public async Task<bool> ChangeOwnerAsync(Guid projectId, Guid newOwnerId)
    {
        var project = await _dbContext.DuAns.FirstOrDefaultAsync(da => da.Id == projectId);
        if (project == null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        var newOwner = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == newOwnerId && u.IsActive);
        if (newOwner == null)
        {
            throw new ArgumentException("Chủ dự án mới không tồn tại hoặc đã bị khóa.");
        }

        var currentUsername = _currentUserService.GetUsername();
        var currentUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Người dùng không hợp lệ hoặc chưa đăng nhập.");
        }

        if (!currentUser.IsSystemAdmin && project.ChuDuAnId != currentUser.Id)
        {
            throw new UnauthorizedAccessException("Chỉ Quản trị viên hệ thống hoặc Chủ dự án hiện tại mới có quyền thực hiện thao tác này.");
        }

        var oldOwnerId = project.ChuDuAnId;
        project.ChuDuAnId = newOwnerId;
        project.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var actorName = currentUser.FullName ?? currentUser.Username;

        // 1. Gửi thông báo và Email cho Chủ dự án mới
        await NotifyProjectOwnerAssignedAsync(project, newOwnerId, actorName);

        // 2. Gửi thông báo cho Chủ dự án cũ (nếu có và khác chủ dự án mới)
        if (oldOwnerId.HasValue && oldOwnerId.Value != newOwnerId)
        {
            var oldOwner = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == oldOwnerId.Value);
            if (oldOwner != null)
            {
                var oldOwnerNotification = NotificationBuilder.Create()
                    .WithTitle("Thôi chức vụ Chủ dự án")
                    .WithContent($"{actorName} đã chuyển giao quyền Chủ dự án '{project.Name}' cho {newOwner.FullName ?? newOwner.Username}.")
                    .WithLink($"/du-an/{project.Id}")
                    .WithFeatureCode("DU_AN")
                    .WithEntity("DuAn", project.Id.ToString())
                    .ForUser(oldOwnerId.Value)
                    .WithActor(actorName)
                    .WithTarget(project.Name)
                    .WithBadge("Bàn giao", "destructive")
                    .WithMessage("đã chuyển giao quyền Chủ dự án của")
                    .Build();
                _dbContext.Notifications.Add(oldOwnerNotification);
                var oldOwnerDto = NotificationMapper.MapToDto(oldOwnerNotification);
                await _hubContext.Clients.User(oldOwner.Username).SendAsync("ReceiveNotification", oldOwnerDto);
                await _dbContext.SaveChangesAsync();
            }
        }

        return true;
    }
}

