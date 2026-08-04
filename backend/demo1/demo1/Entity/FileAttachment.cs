using System;

namespace demo1.Entity
{
    public class FileAttachment : BaseEntity
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
    }
}
