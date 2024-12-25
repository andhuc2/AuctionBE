using System;
using System.Collections.Generic;

namespace API.Models
{
    public partial class Bid
    {
        public int? Id { get; set; }
        public int? ItemId { get; set; }
        public int? BidderId { get; set; }
        public decimal? BidAmount { get; set; }
        public DateTime? BidDate { get; set; }

        public virtual User? Bidder { get; set; } = null!;
        public virtual Item? Item { get; set; } = null!;
    }
}
