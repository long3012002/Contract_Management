using System;

namespace demo1.Entities
{
    public class BaseEntity
    {
        public Guid Oid { get; set; }
        public Guid? IdDonViSuDung { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedUser { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedUser { get; set; }
    }
}
