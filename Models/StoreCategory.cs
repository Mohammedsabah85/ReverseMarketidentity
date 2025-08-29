namespace ReverseMarket.Models
{
    public class StoreCategory
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CategoryId { get; set; }

        public int? SubCategory1Id { get; set; }

        public int? SubCategory2Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual User User { get; set; }
        public virtual Category Category { get; set; }
        public virtual SubCategory1? SubCategory1 { get; set; }
        public virtual SubCategory2? SubCategory2 { get; set; }
    }
}
