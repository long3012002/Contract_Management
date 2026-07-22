using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Hubs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class CommentCongViecGoiThauService : ICommentCongViecGoiThauService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IMapper _mapper;
    private readonly CongViecReminderHangfireService _reminderService;

    public CommentCongViecGoiThauService(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IHubContext<NotificationHub> hubContext,
        IMapper mapper,
        CongViecReminderHangfireService reminderService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _hubContext = hubContext;
        _mapper = mapper;
        _reminderService = reminderService;
    }

    public async Task<IEnumerable<CommentCongViecGoiThauDto>> GetCommentsByCongViecIdAsync(Guid idCongViec)
    {
        var comments = await _context.CommentCongViecGoiThaus
            .Include(c => c.User)
            .Include(c => c.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .Where(c => c.CongViecGoiThauId == idCongViec)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        var commentDtos = _mapper.Map<List<CommentCongViecGoiThauDto>>(comments);

        // Build tree: root comments with nested replies
        var rootComments = commentDtos.Where(c => c.ParentCommentId == null).ToList();
        var repliesLookup = commentDtos.Where(c => c.ParentCommentId != null)
                                       .ToLookup(c => c.ParentCommentId!.Value);

        foreach (var root in rootComments)
        {
            PopulateReplies(root, repliesLookup);
        }

        return rootComments;
    }

    public async Task<CommentCongViecGoiThauDto> CreateCommentAsync(CreateCommentCongViecGoiThauDto dto)
    {
        var username = _currentUserService.GetUsername();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Người dùng không hợp lệ.");
        }

        var congViec = await _context.CongViecGoiThaus.FirstOrDefaultAsync(c => c.Id == dto.ParentId);
        if (congViec == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy công việc gói thầu id={dto.ParentId}");
        }

        var comment = new CommentCongViecGoiThau
        {
            Id = Guid.NewGuid(),
            CongViecGoiThauId = dto.ParentId,
            UserId = user.Id,
            Content = dto.Content,
            ParentCommentId = dto.ParentCommentId,
            CreatedAt = DateTime.UtcNow,
            IsEdited = false,
            IsDeleted = false
        };

        _context.CommentCongViecGoiThaus.Add(comment);

        // Tự động xác nhận cho người liên quan khi gửi bình luận
        var stakeholderRecord = await _context.CongViecNguoiLienQuans
            .FirstOrDefaultAsync(n => n.CongViecGoiThauId == dto.ParentId && n.UserId == user.Id);

        if (stakeholderRecord != null && stakeholderRecord.TrangThaiXacNhan == "Pending")
        {
            stakeholderRecord.TrangThaiXacNhan = "Commented";
            stakeholderRecord.XacNhanAt = DateTime.UtcNow;
            stakeholderRecord.LoaiXacNhan = "Comment";
            stakeholderRecord.UpdatedAt = DateTime.UtcNow;

            _reminderService.CancelReminders(stakeholderRecord);
        }

        // Xử lý Mention người dùng
        var notificationsToPush = new List<(string TargetUsername, Notification NotificationPayload)>();

        if (dto.MentionedUserIds != null && dto.MentionedUserIds.Any())
        {
            var mentionedUsers = await _context.Users
                .Where(u => dto.MentionedUserIds.Contains(u.Id) && u.Id != user.Id)
                .ToListAsync();

            foreach (var mUser in mentionedUsers)
            {
                comment.Mentions.Add(new CommentMention
                {
                    Id = Guid.NewGuid(),
                    CommentId = comment.Id,
                    MentionedUserId = mUser.Id,
                    CreatedAt = DateTime.UtcNow
                });

                // Tạo thông báo cho người dùng được tag
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Bạn được nhắc đến trong một bình luận",
                    Content = $"{user.FullName} đã nhắc đến bạn trong công việc gói thầu '{congViec.TenTaiLieu}'",
                    Link = $"/goi-thau/cong-viec/{congViec.Id}",
                    UserId = mUser.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                notificationsToPush.Add((mUser.Username, notification));
            }
        }

        // Nếu là phản hồi bình luận cha, gửi thông báo cho tác giả bình luận cha
        if (dto.ParentCommentId.HasValue)
        {
            var parentComment = await _context.CommentCongViecGoiThaus
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == dto.ParentCommentId.Value);

            if (parentComment != null && parentComment.UserId != user.Id && !notificationsToPush.Any(n => n.TargetUsername == parentComment.User.Username))
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Có câu trả lời cho bình luận của bạn",
                    Content = $"{user.FullName} đã trả lời bình luận của bạn trong công việc gói thầu '{congViec.TenTaiLieu}'",
                    Link = $"/goi-thau/cong-viec/{congViec.Id}",
                    UserId = parentComment.UserId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                notificationsToPush.Add((parentComment.User.Username, notification));
            }
        }

        await _context.SaveChangesAsync();

        // Reload comment với Navigation Properties cho AutoMapper
        var createdComment = await _context.CommentCongViecGoiThaus
            .Include(c => c.User)
            .Include(c => c.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .FirstAsync(c => c.Id == comment.Id);

        var resultDto = _mapper.Map<CommentCongViecGoiThauDto>(createdComment);

        // 1. Broadcast SignalR Real-time Event to Group "CongViec_{idCongViec}"
        var groupName = $"CongViec_{dto.ParentId}";
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveComment", resultDto);

        // 2. Push Real-time Notification to Tagged / Notified Users
        foreach (var notif in notificationsToPush)
        {
            await _hubContext.Clients.User(notif.TargetUsername).SendAsync("ReceiveNotification", new
            {
                id = notif.NotificationPayload.Id,
                title = notif.NotificationPayload.Title,
                content = notif.NotificationPayload.Content,
                link = notif.NotificationPayload.Link,
                isRead = notif.NotificationPayload.IsRead,
                createdAt = notif.NotificationPayload.CreatedAt
            });
        }

        return resultDto;
    }

    public async Task<CommentCongViecGoiThauDto?> UpdateCommentAsync(Guid id, UpdateCommentCongViecGoiThauDto dto)
    {
        var username = _currentUserService.GetUsername();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Người dùng không hợp lệ.");
        }

        var comment = await _context.CommentCongViecGoiThaus
            .Include(c => c.User)
            .Include(c => c.Mentions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null || comment.IsDeleted) return null;

        // Chỉ tác giả hoặc System Admin mới được sửa
        if (comment.UserId != user.Id && !user.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền sửa bình luận này.");
        }

        comment.Content = dto.Content;
        comment.IsEdited = true;
        comment.UpdatedAt = DateTime.UtcNow;

        // Cập nhật Mentions
        _context.CommentMentions.RemoveRange(comment.Mentions);
        comment.Mentions.Clear();

        if (dto.MentionedUserIds != null && dto.MentionedUserIds.Any())
        {
            var mentionedUsers = await _context.Users
                .Where(u => dto.MentionedUserIds.Contains(u.Id))
                .ToListAsync();

            foreach (var mUser in mentionedUsers)
            {
                comment.Mentions.Add(new CommentMention
                {
                    Id = Guid.NewGuid(),
                    CommentId = comment.Id,
                    MentionedUserId = mUser.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        var updatedComment = await _context.CommentCongViecGoiThaus
            .Include(c => c.User)
            .Include(c => c.Mentions)
                .ThenInclude(m => m.MentionedUser)
            .FirstAsync(c => c.Id == comment.Id);

        var resultDto = _mapper.Map<CommentCongViecGoiThauDto>(updatedComment);

        // Broadcast Real-time Comment Update to Group
        var groupName = $"CongViec_{comment.CongViecGoiThauId}";
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveCommentUpdate", resultDto);

        return resultDto;
    }

    public async Task<bool> DeleteCommentAsync(Guid id)
    {
        var username = _currentUserService.GetUsername();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Người dùng không hợp lệ.");
        }

        var comment = await _context.CommentCongViecGoiThaus.FirstOrDefaultAsync(c => c.Id == id);
        if (comment == null) return false;

        if (comment.UserId != user.Id && !user.IsSystemAdmin)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xóa bình luận này.");
        }

        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Broadcast Real-time Comment Delete to Group
        var groupName = $"CongViec_{comment.CongViecGoiThauId}";
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveCommentDelete", id);

        return true;
    }

    public async Task<IEnumerable<UserMentionDto>> GetMentionSuggestionsAsync(string? search)
    {
        var query = _context.Users.Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(searchPattern) ||
                                     u.FullName.ToLower().Contains(searchPattern));
        }

        var users = await query.Take(10).ToListAsync();
        return _mapper.Map<IEnumerable<UserMentionDto>>(users);
    }

    private static void PopulateReplies(CommentCongViecGoiThauDto parent, ILookup<Guid, CommentCongViecGoiThauDto> repliesLookup)
    {
        var childReplies = repliesLookup[parent.Id].ToList();
        parent.Replies = childReplies;

        foreach (var child in childReplies)
        {
            PopulateReplies(child, repliesLookup);
        }
    }
}
