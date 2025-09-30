using System.ComponentModel.DataAnnotations;

namespace Backend.Model.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public int StockQuantity { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }
    }
}