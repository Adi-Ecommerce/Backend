// Models/CartItem.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice => (Product?.Price ?? 0) * Quantity;

        public int CartId { get; set; }

        [ForeignKey("CartId")]
        public Cart? Cart { get; set; }
    }
}
