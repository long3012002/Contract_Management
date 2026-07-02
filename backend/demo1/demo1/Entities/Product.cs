using System;

namespace demo1.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
