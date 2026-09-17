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

namespace demo1.Services.Implements.SubServices;

public class DuAnNotificationService : IDuAnNotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public DuAnNotificationService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _hubContext = hubContext;
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

        // 1. Gửi thông báo cho Chủ dự án mới
        var newOwnerNotification = new Notification
        {
            Title = "Được phân công làm Chủ dự án",
            Content = $"Bạn đã được phân công làm Chủ dự án cho dự án: {project.Name}",
            Link = $"/projects/{project.Id}",
            FeatureCode = "DU_AN",
            EntityName = "DuAn",
            EntityId = project.Id.ToString(),
            UserId = newOwnerId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            ActionBadgeText = "Bổ nhiệm",
            ActionBadgeVariant = "info",
            Message = "Bạn được phân công làm Chủ dự án của",
            TargetName = project.Name
        };
        _dbContext.Notifications.Add(newOwnerNotification);
        await _hubContext.Clients.User(newOwner.Username).SendAsync("ReceiveNotification", newOwnerNotification);

        // 2. Gửi thông báo cho Chủ dự án cũ (nếu có và khác chủ dự án mới)
        if (oldOwnerId.HasValue && oldOwnerId.Value != newOwnerId)
        {
            var oldOwner = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == oldOwnerId.Value);
            if (oldOwner != null)
            {
                var oldOwnerNotification = new Notification
                {
                    Title = "Thôi chức vụ Chủ dự án",
                    Content = $"Bạn đã thôi giữ chức vụ Chủ dự án cho dự án: {project.Name}",
                    Link = $"/projects/{project.Id}",
                    FeatureCode = "DU_AN",
                    EntityName = "DuAn",
                    EntityId = project.Id.ToString(),
                    UserId = oldOwnerId.Value,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    ActionBadgeText = "Bàn giao",
                    ActionBadgeVariant = "destructive",
                    Message = "Bạn thôi làm Chủ dự án của",
                    TargetName = project.Name
                };
                _dbContext.Notifications.Add(oldOwnerNotification);
                await _hubContext.Clients.User(oldOwner.Username).SendAsync("ReceiveNotification", oldOwnerNotification);
            }
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }
}
