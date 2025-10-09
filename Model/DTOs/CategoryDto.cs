namespace Backend.Model.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        
        public string? Image { get; set; }

        public List<ProductDto>? Products { get; set; }
    }
}
