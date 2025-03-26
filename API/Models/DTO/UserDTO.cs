using API.Models;
using API.Models.DTO;

namespace NET_base.Models.DTO
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int Role { get; set; }
        public string? Token { get; set; }
        public decimal? Credit { get; set; } = 0;

        public virtual ICollection<BidDTO> Bids { get; set; }
        public virtual ICollection<ItemDTO> Items { get; set; }
    }
}
