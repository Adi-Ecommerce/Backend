// DTOs/CategoryUpdateDto.cs
namespace Backend.Model.DTOs
{
    public class CategoryUpdateDto
    {
        public int Id { get; set; }          // For route matching
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
    }
}