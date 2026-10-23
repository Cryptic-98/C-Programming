using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class NewStudent
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = "";

        [Range(16, 100)]
        public int Age { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}