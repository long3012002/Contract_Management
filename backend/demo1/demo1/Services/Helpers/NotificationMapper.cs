using System;
using System.Text.RegularExpressions;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Services.Helpers;

public static class NotificationMapper
{
    public static NotificationDto MapToDto(Notification n)
    {
        var category = ResolveCategory(n.FeatureCode, n.EntityName);
        var content = CleanNotificationContent(n.Content);

        var actionBadge = n.ActionBadgeText;
        var badgeVariant = n.ActionBadgeVariant;
        var actorName = n.ActorName;
        var message = n.Message;
        var targetName = n.TargetName;

        // Auto fallback extraction for older records in DB if structured fields are null
        if (string.IsNullOrWhiteSpace(actionBadge) && string.IsNullOrWhiteSpace(message))
        {
            EnrichLegacyMetadata(n.Title, content, category, ref actionBadge, ref badgeVariant, ref actorName, ref message, ref targetName);
        }

        return new NotificationDto
        {
            Id = n.Id,
            Title = n.Title,
            Content = content,
            Link = n.Link,
            FeatureCode = n.FeatureCode,
            EntityName = n.EntityName,
            EntityId = n.EntityId,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            Category = category,
            ActorName = actorName,
            ActionBadgeText = actionBadge,
            ActionBadgeVariant = badgeVariant,
            Message = message ?? content,
            TargetName = targetName
        };
    }

    public static string ResolveCategory(string? featureCode, string? entityName)
    {
        var code = (featureCode ?? "").Trim().ToUpperInvariant();
        var entity = (entityName ?? "").Trim().ToUpperInvariant();

        if (code.Contains("PROJECT") || code.Contains("DU_AN") || entity.Contains("DUAN"))
            return "Dự án";
        if (code.Contains("CONTRACT") || code.Contains("HOP_DONG") || code.Contains("LICENSE") || entity.Contains("HOPDONG") || entity.Contains("LICENSE"))
            return "Hợp đồng";
        if (code.Contains("TASK") || code.Contains("CONG_VIEC") || code.Contains("BID_PACKAGE") || entity.Contains("GOITHAU") || entity.Contains("CONGVIEC"))
            return "Công việc";
        if (code.Contains("PERMISSION") || entity.Contains("PERMISSION"))
            return "Phân quyền";

        return "Hệ thống";
    }

    public static string CleanNotificationContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var cleaned = Regex.Replace(content, @"^\[[^\]]+\]:?\s*", string.Empty);
        return cleaned.Trim();
    }

    private static void EnrichLegacyMetadata(
        string title,
        string content,
        string category,
        ref string? actionBadge,
        ref string? badgeVariant,
        ref string? actorName,
        ref string? message,
        ref string? targetName)
    {
        // 1. Phân quyền
        if (category == "Phân quyền" || title.Contains("Quyền") || title.Contains("Phân quyền"))
        {
            if (content.Contains("Được duyệt", StringComparison.OrdinalIgnoreCase) || title.Contains("Được duyệt", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Được duyệt";
                badgeVariant = "info";
                message = "Yêu cầu quyền truy cập đã được duyệt";
            }
            else if (content.Contains("Bị từ chối", StringComparison.OrdinalIgnoreCase) || title.Contains("Bị từ chối", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Từ chối";
                badgeVariant = "destructive";
                message = "Yêu cầu quyền truy cập đã bị từ chối";
            }
            else if (content.Contains("cấp quyền", StringComparison.OrdinalIgnoreCase))
            {
                var matchPerm = Regex.Match(content, @"quyền\s+['""]?([^'""]+)['""]?\s+trên dự án\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                if (matchPerm.Success)
                {
                    actionBadge = matchPerm.Groups[1].Value.Trim();
                    targetName = matchPerm.Groups[2].Value.Trim();
                    message = "Bạn được cấp quyền tại";
                    badgeVariant = "info";
                }
                else
                {
                    actionBadge = "Xem";
                    badgeVariant = "info";
                    message = "Bạn được cấp quyền truy cập";
                }
            }
            return;
        }

        // 2. Hợp đồng / Quá hạn / Sắp hết hạn
        if (category == "Hợp đồng" || title.Contains("Hợp đồng") || title.Contains("License"))
        {
            var nameMatch = Regex.Match(content, @"(?:Hợp đồng|License)\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
            if (nameMatch.Success)
            {
                targetName = nameMatch.Groups[1].Value.Trim();
            }

            if (content.Contains("quá hạn", StringComparison.OrdinalIgnoreCase) || content.Contains("đã hết hạn", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Đã quá hạn";
                badgeVariant = "destructive";
                var daysMatch = Regex.Match(content, @"(?:quá hạn|hết hạn)\s+(\d+)\s*ngày", RegexOptions.IgnoreCase);
                message = daysMatch.Success ? $"{daysMatch.Groups[1].Value} ngày" : "đã quá hạn";
            }
            else if (content.Contains("sắp hết hạn", StringComparison.OrdinalIgnoreCase) || content.Contains("còn", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = null;
                var daysMatch = Regex.Match(content, @"còn\s+(\d+)\s*ngày", RegexOptions.IgnoreCase);
                var dateMatch = Regex.Match(content, @"hạn:\s*(\d{1,2}/\d{1,2}(?:/\d{4})?)", RegexOptions.IgnoreCase);
                var daysPart = daysMatch.Success ? $"còn {daysMatch.Groups[1].Value} ngày" : "sắp hết hạn";
                var datePart = dateMatch.Success ? $" (hạn {dateMatch.Groups[1].Value})" : "";
                message = $"{daysPart}{datePart}";
            }
            return;
        }

        // 3. Công việc / Bình luận
        if (category == "Công việc" || title.Contains("Công việc") || title.Contains("Bình luận"))
        {
            if (title.Contains("nhắc tên", StringComparison.OrdinalIgnoreCase) || content.Contains("đã nhắc đến bạn", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Nhắc tên";
                badgeVariant = "info";
                var match = Regex.Match(content, @"^(.*?)\s+đã nhắc đến bạn trong\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    actorName = match.Groups[1].Value.Trim();
                    targetName = match.Groups[2].Value.Trim();
                    message = "đã nhắc đến bạn trong";
                }
            }
            else if (title.Contains("Xác nhận", StringComparison.OrdinalIgnoreCase) || content.Contains("đã xác nhận", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Xác nhận";
                badgeVariant = "success";
                var match = Regex.Match(content, @"Thành viên\s+(.*?)\s+đã xác nhận công việc\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    actorName = match.Groups[1].Value.Trim();
                    targetName = match.Groups[2].Value.Trim();
                    message = "đã xác nhận công việc";
                }
            }
            else if (title.Contains("Quá hạn", StringComparison.OrdinalIgnoreCase) || content.Contains("quá hạn", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Đã quá hạn";
                badgeVariant = "destructive";
                message = "quá hạn xác nhận công việc";
            }
        }
    }
}
