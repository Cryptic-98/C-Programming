using System.ComponentModel.DataAnnotations;

namespace MyPracticeApp.Models
{
    public class Patient : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First Name is required.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Full Name must be between 5 and 100 characters.")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Phone Number is required.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone Number must be between 10 and 15 characters.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
    
        public string PhoneNumber { get; set; } = "";

        [Required(ErrorMessage = "Date of Birth is required.")]
        public DateTime? DateOfBirth { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateOfBirth > DateTime.Now)
            {
                yield return new ValidationResult("Date of Birth cannot be in the future.", new[] { nameof(DateOfBirth) });
            }
        }

        [StringLength(20, ErrorMessage = "Medical Aid Number cannot exceed 20 characters.")]
        public string MedicalAidNumber { get; set; } = "";
    }
}