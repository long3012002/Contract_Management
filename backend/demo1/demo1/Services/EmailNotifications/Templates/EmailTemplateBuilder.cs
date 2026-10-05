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
        var deadlineFormatted = deadlineDate.ToString("dd/MM/yyyy");
        var statusSentence = daysRemaining < 0
            ? $"đã <span style=\"color:{DangerColor};font-weight:bold;\">quá hạn {Math.Abs(daysRemaining)} ngày</span> <span style=\"white-space:nowrap;\">(hạn&nbsp;chót:&nbsp;<strong>{deadlineFormatted}</strong>)</span>."
            : daysRemaining == 0
                ? $"<span style=\"color:{DangerColor};font-weight:bold;\">hết hạn HÔM NAY</span> <span style=\"white-space:nowrap;\">(ngày&nbsp;<strong>{deadlineFormatted}</strong>)</span>."
                : $"sẽ <span style=\"color:{WarningColor};font-weight:bold;\">hết hạn sau {daysRemaining} ngày</span> <span style=\"white-space:nowrap;\">(hạn&nbsp;chót:&nbsp;<strong>{deadlineFormatted}</strong>)</span>.";

        var plainStatusSentence = daysRemaining < 0
            ? $"đã quá hạn {Math.Abs(daysRemaining)} ngày (hạn chót: {deadlineFormatted})."
            : daysRemaining == 0
                ? $"hết hạn HÔM NAY (ngày {deadlineFormatted})."
                : $"sẽ hết hạn sau {daysRemaining} ngày (hạn chót: {deadlineFormatted}).";

        var previewText = $"{entityType} \"{entityName}\" {plainStatusSentence}";

        var sb = new StringBuilder();
        sb.Append($@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{System.Net.WebUtility.HtmlEncode(title)}</title>
</head>
<body style=""margin:0;padding:20px;background:#ffffff;font-family:Segoe UI,Tahoma,Arial,Helvetica,sans-serif;font-size:14px;line-height:1.6;color:#242424;"">

  <!-- Preheader cho hộp thư đến Outlook để đọc nhanh mà không bị lặp tiêu đề -->
  <div style=""display:none;font-size:1px;color:#ffffff;line-height:1px;max-height:0px;max-width:0px;opacity:0;overflow:hidden;"">
    {System.Net.WebUtility.HtmlEncode(previewText)}
  </div>

  <div style=""max-width:700px;text-align:left;"">
    <p style=""margin:0 0 16px 0;"">
      Kính gửi Ông/Bà <strong>{System.Net.WebUtility.HtmlEncode(recipientName)}</strong>,
    </p>

    <p style=""margin:0 0 16px 0;"">
      Hệ thống Quản lý Dự án xin thông báo {entityType.ToLower()} <strong>{System.Net.WebUtility.HtmlEncode(entityName)}</strong> {statusSentence}
    </p>

    <table cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;margin:16px 0 20px 0;width:100%;max-width:650px;border-left:3px solid {AccentColor};background:#f8f9fa;"">
      <tr>
        <td style=""padding:12px 16px;"">
          <div style=""font-size:12px;color:#666666;text-transform:uppercase;font-weight:600;margin-bottom:4px;"">
            {System.Net.WebUtility.HtmlEncode(entityType)}
          </div>
          <div style=""font-size:15px;font-weight:bold;color:#111827;"">
            {System.Net.WebUtility.HtmlEncode(entityName)}
          </div>
          <div style=""font-size:13px;color:#444444;margin-top:6px;"">
            Thời hạn: <strong style=""color:#111827;"">{deadlineDate:dd/MM/yyyy}</strong>
            {(daysRemaining < 0
              ? $@" (<span style=""color:{DangerColor};font-weight:bold;white-space:nowrap;"">Đã quá hạn {Math.Abs(daysRemaining)} ngày</span>)"
              : daysRemaining == 0
                ? $@" (<span style=""color:{DangerColor};font-weight:bold;white-space:nowrap;"">Hết hạn hôm nay</span>)"
                : $@" (<span style=""color:{WarningColor};font-weight:bold;white-space:nowrap;"">Còn {daysRemaining} ngày</span>)")}
          </div>
          {(extraInfo != null ? $@"<div style=""font-size:13px;color:#444444;margin-top:4px;"">{System.Net.WebUtility.HtmlEncode(extraInfo)}</div>" : "")}
        </td>
      </tr>
    </table>

    {(detailUrl != null ? $@"
    <p style=""margin:0 0 24px 0;"">
      👉 <a href=""{detailUrl}"" style=""color:{AccentColor};font-weight:bold;text-decoration:underline;"">Bấm vào đây để xem chi tiết trên hệ thống</a>
    </p>" : "")}

    <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0 16px 0;"" />

    <p style=""margin:0;font-size:12px;color:#6b7280;line-height:1.5;"">
      Email này được gửi tự động từ <strong>Hệ thống Quản lý Dự án</strong>. Vui lòng không trả lời thư này.
    </p>
  </div>

</body>
</html>");
        return sb.ToString();
    }
}
