using System;
using System.Collections.Generic;

namespace API.Models
{
    public partial class Rating
    {
        public int? Id { get; set; }
        public int? ItemId { get; set; }
        public int? RaterId { get; set; }
        public int? RateeId { get; set; }
        public int? RatingValue { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual Item? Item { get; set; } = null!;
        public virtual User? Ratee { get; set; } = null!;
        public virtual User? Rater { get; set; } = null!;
    }
}
