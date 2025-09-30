
using System.ComponentModel.DataAnnotations;

namespace Backend.Model.Entities
{
    public class Category
    {
        internal object Products;

        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }



    }
}

