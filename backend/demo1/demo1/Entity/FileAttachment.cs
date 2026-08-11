using System;
using System.Collections.Generic;

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

        public int CurrentVersion { get; set; } = 1;
        public ICollection<FileVersion> Versions { get; set; } = new List<FileVersion>();
    }
}
