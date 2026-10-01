using System;
using System.Text;

namespace demo1.Services.EmailNotifications.Templates;

/// <summary>
/// Tạo HTML body cho email notification.
/// Tất cả handler dùng chung builder này để đảm bảo giao diện nhất quán.
/// </summary>
public static class EmailTemplateBuilder
{
    private const string AccentColor = "#005B99";   // Xanh Co-opBank
    private const string DangerColor = "#DC2626";   // Đỏ – quá hạn
    private const string WarningColor = "#D97706";  // Cam – sắp hết hạn

    /// <summary>
    /// Tạo HTML email thông báo hạn chót (HopDong / License / DotThanhToan).
    /// </summary>
    /// <param name="recipientName">Tên người nhận để greeting.</param>
    /// <param name="title">Tiêu đề chính của email (ngắn gọn).</param>
    /// <param name="entityType">Loại đối tượng (VD: "Hợp đồng", "License", "Đợt thanh toán").</param>
    /// <param name="entityName">Tên/mã định danh của đối tượng.</param>
    /// <param name="daysRemaining">
    ///   Số ngày còn lại: âm = quá hạn, 0 = hôm nay, dương = còn hạn.
    /// </param>
    /// <param name="deadlineDate">Ngày đến hạn.</param>
    /// <param name="detailUrl">URL đầy đủ để mở chi tiết (có thể null).</param>
    /// <param name="extraInfo">Thông tin bổ sung tuỳ loại (VD: số tiền thanh toán).</param>
    public static string BuildExpiryEmail(
        string recipientName,
        string title,
        string entityType,
        string entityName,
        int daysRemaining,
        DateTime deadlineDate,
        string? detailUrl = null,
        string? extraInfo = null)
    {
        var badgeColor = daysRemaining < 0 ? DangerColor : WarningColor;
        var badgeText = daysRemaining < 0
            ? $"Đã quá hạn {Math.Abs(daysRemaining)} ngày"
            : daysRemaining == 0
                ? "Hết hạn hôm nay"
                : $"Còn {daysRemaining} ngày";

        var statusSentence = daysRemaining < 0
            ? $"đã <strong>quá hạn {Math.Abs(daysRemaining)} ngày</strong> (ngày hết hạn: {deadlineDate:dd/MM/yyyy})."
            : daysRemaining == 0
                ? $"<strong>hết hạn hôm nay</strong> ({deadlineDate:dd/MM/yyyy})."
                : $"sẽ <strong>hết hạn sau {daysRemaining} ngày</strong> (ngày hết hạn: {deadlineDate:dd/MM/yyyy}).";

        var sb = new StringBuilder();
        sb.Append($@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{title}</title>
</head>
<body style=""margin:0;padding:0;background:#f5f5f5;font-family:Arial,Helvetica,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#f5f5f5;padding:32px 0;"">
    <tr>
      <td align=""center"">
        <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08);"">

          <!-- Header -->
          <tr>
            <td style=""background:{AccentColor};padding:24px 32px;"">
              <p style=""margin:0;color:#ffffff;font-size:13px;opacity:.85;"">Hệ thống quản lý hợp đồng</p>
              <h1 style=""margin:4px 0 0;color:#ffffff;font-size:20px;font-weight:700;"">{title}</h1>
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:32px 32px 24px;"">
              <p style=""margin:0 0 16px;color:#374151;font-size:15px;"">Xin chào <strong>{System.Net.WebUtility.HtmlEncode(recipientName)}</strong>,</p>

              <!-- Badge -->
              <p style=""margin:0 0 20px;"">
                <span style=""display:inline-block;background:{badgeColor};color:#fff;font-size:12px;font-weight:700;padding:4px 12px;border-radius:20px;"">{badgeText}</span>
              </p>

              <!-- Info card -->
              <table width=""100%"" cellpadding=""0"" cellspacing=""0""
                     style=""background:#f9fafb;border:1px solid #e5e7eb;border-radius:6px;margin-bottom:20px;"">
                <tr>
                  <td style=""padding:16px 20px;"">
                    <p style=""margin:0 0 8px;color:#6b7280;font-size:12px;text-transform:uppercase;letter-spacing:.5px;"">{entityType}</p>
                    <p style=""margin:0;color:#111827;font-size:16px;font-weight:700;"">{System.Net.WebUtility.HtmlEncode(entityName)}</p>
                    {(extraInfo != null ? $@"<p style=""margin:6px 0 0;color:#374151;font-size:14px;"">{System.Net.WebUtility.HtmlEncode(extraInfo)}</p>" : "")}
                  </td>
                </tr>
              </table>

              <p style=""margin:0 0 24px;color:#374151;font-size:15px;line-height:1.6;"">
                {entityType} trên {statusSentence}
                Vui lòng kiểm tra và xử lý kịp thời.
              </p>

              {(detailUrl != null ? $@"
              <table cellpadding=""0"" cellspacing=""0"">
                <tr>
                  <td style=""background:{AccentColor};border-radius:6px;"">
                    <a href=""{detailUrl}""
                       style=""display:inline-block;padding:12px 28px;color:#ffffff;font-size:14px;font-weight:700;text-decoration:none;"">
                      Xem chi tiết →
                    </a>
                  </td>
                </tr>
              </table>" : "")}
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background:#f9fafb;border-top:1px solid #e5e7eb;padding:16px 32px;"">
              <p style=""margin:0;color:#9ca3af;font-size:12px;"">
                Đây là email tự động từ <strong>Hệ thống quản lý hợp đồng Co-opBank</strong>.
                Vui lòng không trả lời email này.
              </p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
");
        return sb.ToString();
    }
}
