using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MyApp.Models
{
    public class Student : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Age is required")]
        [Range(16, 120, ErrorMessage = "Age must be between 16 and 120.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = "";


        public string Course { get; set; } = "";
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Course == "Advanced Programming" && Age < 18)
            {
                yield return new ValidationResult("Students in Advanced Programming must be at least 18 years old.", new[] { nameof(Age) });
            }
        }
    }
}