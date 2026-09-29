using System;

namespace demo1.DTOs
{
    /// <summary>
    /// DTO thông tin tệp đính kèm phân cấp trong toàn bộ cây Dự án (Project Document Hub)
    /// </summary>
    public class ProjectFileAttachmentDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Phân loại thực thể: DU_AN, GOI_THAU, QUAN_LY_HOP_DONG, CONG_VIEC_GOI_THAU
        /// </summary>
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }

        /// <summary>
        /// Tên / Tiêu đề của đối tượng chứa file (Ví dụ: "Hợp đồng Mua sắm máy chủ", "Gói thầu số 01")
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// Mã định danh đối tượng nếu có (Code)
        /// </summary>
        public string? EntityCode { get; set; }

        /// <summary>
        /// ID Gói thầu liên quan (nếu thuộc gói thầu hoặc hợp đồng/công việc trong gói)
        /// </summary>
        public Guid? GoiThauId { get; set; }
        public string? GoiThauName { get; set; }

        /// <summary>
        /// Cấp bậc phân loại: "DU_AN" | "GOI_THAU" | "HOP_DONG" | "CONG_VIEC"
        /// </summary>
        public string CategoryLevel { get; set; } = "DU_AN";
    }
}
