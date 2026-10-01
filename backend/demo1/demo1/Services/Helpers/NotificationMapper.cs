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

        // Auto fallback extraction for older records in DB if structured fields are null or message is missing or truncated (e.g. '265 ngày')
        if (string.IsNullOrWhiteSpace(actionBadge) || string.IsNullOrWhiteSpace(message) || Regex.IsMatch(message ?? "", @"^\d+\s*ngày$", RegexOptions.IgnoreCase))
        {
            EnrichLegacyMetadata(n.Title, content, category, ref actionBadge, ref badgeVariant, ref actorName, ref message, ref targetName);
        }

        return new NotificationDto
        {
            Id = n.Id,
            Link = n.Link,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            Category = category,
            ActorName = actorName,
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
                actionBadge = "Đã duyệt";
                badgeVariant = "success";
                message = "đã phê duyệt yêu cầu quyền truy cập của bạn tại";
            }
            else if (content.Contains("Bị từ chối", StringComparison.OrdinalIgnoreCase) || title.Contains("Bị từ chối", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Từ chối";
                badgeVariant = "destructive";
                message = "đã từ chối yêu cầu quyền truy cập của bạn tại";
            }
            else if (content.Contains("thu hồi", StringComparison.OrdinalIgnoreCase) || title.Contains("Thu hồi", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Thu hồi";
                badgeVariant = "destructive";
                var matchPerm = Regex.Match(content, @"quyền\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                var permText = matchPerm.Success ? $" '{matchPerm.Groups[1].Value.Trim()}'" : "";
                message = $"đã thu hồi quyền{permText} của bạn tại";
            }
            else if (content.Contains("cập nhật", StringComparison.OrdinalIgnoreCase) || title.Contains("Cập nhật", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Cập nhật";
                badgeVariant = "info";
                var matchPerm = Regex.Match(content, @"thành\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                var permText = matchPerm.Success ? $" thành '{matchPerm.Groups[1].Value.Trim()}'" : "";
                message = $"đã cập nhật quyền của bạn{permText} tại";
            }
            else if (content.Contains("cấp quyền", StringComparison.OrdinalIgnoreCase) || title.Contains("Cấp quyền", StringComparison.OrdinalIgnoreCase))
            {
                var matchPerm = Regex.Match(content, @"quyền\s+['""]?([^'""]+)['""]?\s+trên dự án\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                if (matchPerm.Success)
                {
                    actionBadge = "Cấp quyền";
                    targetName = matchPerm.Groups[2].Value.Trim();
                    message = $"đã cấp quyền '{matchPerm.Groups[1].Value.Trim()}' cho bạn tại";
                    badgeVariant = "success";
                }
                else
                {
                    actionBadge = "Cấp quyền";
                    badgeVariant = "success";
                    message = "đã cấp quyền truy cập cho bạn tại";
                }
            }
            return;
        }

        // 2. Dự án
        if (category == "Dự án" || title.Contains("Dự án", StringComparison.OrdinalIgnoreCase))
        {
            var matchProj = Regex.Match(content, @"dự án:\s*([^()]+)", RegexOptions.IgnoreCase);
            if (matchProj.Success && string.IsNullOrWhiteSpace(targetName))
            {
                targetName = matchProj.Groups[1].Value.Trim();
            }

            if (content.Contains("thôi giữ chức vụ", StringComparison.OrdinalIgnoreCase) || content.Contains("chuyển giao", StringComparison.OrdinalIgnoreCase) || content.Contains("thôi", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Bàn giao";
                badgeVariant = "destructive";
                message = "đã chuyển giao quyền Chủ dự án của";
            }
            else
            {
                actionBadge = "Bổ nhiệm";
                badgeVariant = "info";
                message = "đã phân công bạn làm Chủ dự án của";
            }
            return;
        }

        // 3. Hợp đồng / License / Đợt thanh toán
        if (category == "Hợp đồng" || title.Contains("Hợp đồng") || title.Contains("License") || title.Contains("thanh toán") || title.Contains("Đợt"))
        {
            var isDotThanhToan = title.Contains("thanh toán", StringComparison.OrdinalIgnoreCase) || content.Contains("Đợt thanh toán", StringComparison.OrdinalIgnoreCase);
            var isLicense = title.Contains("License", StringComparison.OrdinalIgnoreCase) || content.Contains("License", StringComparison.OrdinalIgnoreCase);
            var entityType = isDotThanhToan ? "Đợt thanh toán" : (isLicense ? "Bản quyền (License)" : "Hợp đồng");

            var nameMatch = Regex.Match(content, @"(?:Hợp đồng|License|Đợt thanh toán)\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
            if (nameMatch.Success && string.IsNullOrWhiteSpace(targetName))
            {
                targetName = nameMatch.Groups[1].Value.Trim();
            }

            var dateMatch = Regex.Match(content, @"(?:hạn|ngày hết hạn|Hạn thanh toán):\s*(\d{1,2}/\d{1,2}(?:/\d{4})?)", RegexOptions.IgnoreCase);
            var dateSuffix = dateMatch.Success ? $" (hạn: {dateMatch.Groups[1].Value})" : "";

            if (content.Contains("quá hạn", StringComparison.OrdinalIgnoreCase) || content.Contains("đã hết hạn", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Đã quá hạn";
                badgeVariant = "destructive";
                var daysMatch = Regex.Match(content, @"(?:quá hạn|hết hạn)\s+(\d+)\s*ngày", RegexOptions.IgnoreCase);
                var daysText = daysMatch.Success ? $" đã quá hạn {daysMatch.Groups[1].Value} ngày" : " đã quá hạn";
                message = $"{entityType}{daysText}{dateSuffix}:";
            }
            else if (content.Contains("hôm nay", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Hôm nay";
                badgeVariant = "warning";
                message = $"{entityType} hết hạn hôm nay{dateSuffix}:";
            }
            else if (content.Contains("sắp hết hạn", StringComparison.OrdinalIgnoreCase) || content.Contains("sắp đến hạn", StringComparison.OrdinalIgnoreCase) || content.Contains("còn", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Sắp hết hạn";
                badgeVariant = "warning";
                var daysMatch = Regex.Match(content, @"còn\s+(\d+)\s*ngày", RegexOptions.IgnoreCase);
                var daysText = daysMatch.Success ? $" sắp hết hạn (còn {daysMatch.Groups[1].Value} ngày{dateSuffix})" : $" sắp hết hạn{dateSuffix}";
                message = $"{entityType} {daysText}:";
            }
            return;
        }

        // 4. Công việc / Bình luận
        if (category == "Công việc" || title.Contains("Công việc") || title.Contains("Bình luận") || title.Contains("Xác nhận") || title.Contains("Nhắc nhở"))
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
                }
                message = "đã nhắc đến bạn trong công việc";
            }
            else if (title.Contains("Phản hồi", StringComparison.OrdinalIgnoreCase) || content.Contains("đã trả lời bình luận", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Trả lời";
                badgeVariant = "info";
                var match = Regex.Match(content, @"^(.*?)\s+đã trả lời bình luận của bạn trong\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    actorName = match.Groups[1].Value.Trim();
                    targetName = match.Groups[2].Value.Trim();
                }
                message = "đã trả lời bình luận của bạn trong công việc";
            }
            else if (title.Contains("Xác nhận", StringComparison.OrdinalIgnoreCase) || content.Contains("đã xác nhận", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Xác nhận";
                badgeVariant = "success";
                var match = Regex.Match(content, @"(?:Thành viên\s+)?(.*?)\s+đã xác nhận công việc\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    actorName = match.Groups[1].Value.Trim();
                    targetName = match.Groups[2].Value.Trim();
                }
                message = "đã xác nhận công việc";
            }
            else if (title.Contains("Giao việc", StringComparison.OrdinalIgnoreCase) || content.Contains("thêm làm người liên quan", StringComparison.OrdinalIgnoreCase) || content.Contains("thêm bạn vào", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Gán công việc";
                badgeVariant = "info";
                var match = Regex.Match(content, @"công việc\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    targetName = match.Groups[1].Value.Trim();
                }
                message = "đã thêm bạn vào danh sách người liên quan của công việc";
            }
            else if (title.Contains("Loại bỏ", StringComparison.OrdinalIgnoreCase) || content.Contains("gỡ bỏ", StringComparison.OrdinalIgnoreCase) || content.Contains("gỡ bạn", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Thay đổi";
                badgeVariant = "secondary";
                var match = Regex.Match(content, @"công việc\s+['""]?([^'""]+)['""]?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    targetName = match.Groups[1].Value.Trim();
                }
                message = "đã gỡ bạn khỏi danh sách người liên quan của công việc";
            }
            else if (title.Contains("Quá hạn", StringComparison.OrdinalIgnoreCase) || content.Contains("quá hạn", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Đã quá hạn";
                badgeVariant = "destructive";
                var matchMember = Regex.Match(content, @"Thành viên\s+(.*?)\s+đã quá hạn", RegexOptions.IgnoreCase);
                if (matchMember.Success)
                {
                    actorName = matchMember.Groups[1].Value.Trim();
                    message = "đã quá hạn xác nhận công việc";
                }
                else
                {
                    message = "Bạn đã quá hạn xác nhận công việc";
                }
            }
            else if (title.Contains("Nhắc nhở", StringComparison.OrdinalIgnoreCase) || content.Contains("Nhắc nhở", StringComparison.OrdinalIgnoreCase))
            {
                actionBadge = "Nhắc nhở";
                badgeVariant = "warning";
                message = "Nhắc nhở xác nhận công việc";
            }
        }
    }
}
