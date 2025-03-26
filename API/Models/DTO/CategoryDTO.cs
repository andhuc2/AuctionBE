namespace API.Models.DTO
{
    public class CategoryDTO
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = null!;
        public int? ParentCategoryId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
