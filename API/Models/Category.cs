using System;
using System.Collections.Generic;

namespace API.Models
{
    public partial class Category
    {
        public Category()
        {
            InverseParentCategory = new HashSet<Category>();
            Items = new HashSet<Item>();
        }

        public int Id { get; set; }
        public string CategoryName { get; set; } = null!;
        public int? ParentCategoryId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> InverseParentCategory { get; set; }
        public virtual ICollection<Item> Items { get; set; }
    }
}
