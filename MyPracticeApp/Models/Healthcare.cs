using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPracticeApp.Models
{
    public abstract class HealthcareWorker
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
        [Column(TypeName = "date")]
        public DateTime? DateOfBirth { get; set; }
    }

    public class Doctor : HealthcareWorker, IValidatableObject
    {
        [Required(ErrorMessage = "Specialization is required.")]
        [StringLength(50, ErrorMessage = "Specialization cannot exceed 50 characters.")]
        public string Specialization { get; set; } = "";

        [Required(ErrorMessage = "Medical license number is required.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Medical license number must be exactly 8 digits.")]
        public string MedicalLicenseNumber { get; set; } = "";
    }
}