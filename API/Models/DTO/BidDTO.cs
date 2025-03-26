namespace API.Models.DTO
{
    public class BidDTO
    {
        public int? Id { get; set; }
        public int? ItemId { get; set; }
        public int? BidderId { get; set; }
        public decimal? BidAmount { get; set; }
        public DateTime? BidDate { get; set; }
    }
}
