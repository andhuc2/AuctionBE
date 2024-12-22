using System;
using System.Collections.Generic;

namespace API.Models
{
    public partial class Item
    {
        public Item()
        {
            Bids = new HashSet<Bid>();
            Ratings = new HashSet<Rating>();
        }

        public int? Id { get; set; }
        public string? Title { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public int? SellerId { get; set; }
        public int? CategoryId { get; set; }
        public string? ImagePath { get; set; }
        public string? DocumentPath { get; set; }
        public decimal? MinimumBid { get; set; }
        public decimal? BidIncrement { get; set; }
        public string? BidStatus { get; set; }
        public DateTime? BidStartDate { get; set; }
        public DateTime? BidEndDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Category? Category { get; set; } = null!;
        public virtual User? Seller { get; set; } = null!;
        public virtual ICollection<Bid>? Bids { get; set; }
        public virtual ICollection<Rating>? Ratings { get; set; }
    }
}
