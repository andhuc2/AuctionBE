using System;
using System.Collections.Generic;

namespace API.Models
{
    public partial class User
    {
        public User()
        {
            Bids = new HashSet<Bid>();
            Items = new HashSet<Item>();
            Notifications = new HashSet<Notification>();
            RatingRatees = new HashSet<Rating>();
            RatingRaters = new HashSet<Rating>();
        }

        public int Id { get; set; }
        public string? Username { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string? Password { get; set; } = null!;
        public string? FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int? Role { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Token { get; set; }
        public decimal? Credit { get; set; } = 0;

        public virtual ICollection<Bid> Bids { get; set; }
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Rating> RatingRatees { get; set; }
        public virtual ICollection<Rating> RatingRaters { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
    }
}
