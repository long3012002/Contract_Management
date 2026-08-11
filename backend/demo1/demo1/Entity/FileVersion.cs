using System;

namespace demo1.Entity
{
    public class FileVersion : BaseEntity
    {
        public Guid FileAttachmentId { get; set; }
        public FileAttachment FileAttachment { get; set; } = null!;

        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;

        public Guid? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? ChangeDescription { get; set; }
    }
}
