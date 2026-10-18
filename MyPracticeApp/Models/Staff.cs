using System.ComponentModel.DataAnnotations;

namespace MyPracticeApp.Models
{
    public class StaffMember
    {
        public int StaffId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Full Name must be between 5 and 100 characters.")]
        public string FullName { get; set; } = "";

        [Required (ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Phone Number is required.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone Number must be between 10 and 15 characters.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; } = "";

        [Required(ErrorMessage = "Hired Date is required.")]
        [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
        public DateTime? HiredDate { get; set; }
    }

    public class Nurse : StaffMember
    {
        [Required(ErrorMessage = "Department is required.")]
        [StringLength(50, ErrorMessage = "Department cannot exceed 50 characters.")]
        public string Department { get; set; } = "";

        [Required(ErrorMessage = "License number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "License number must be exactly 10 digits.")]
        public string LicenseNumber { get; set; } = "";

        [Required(ErrorMessage = "Shift is required.")]
        [RegularExpression(@"^(Day|Night)$", ErrorMessage = "Shift must be either 'Day' or 'Night'.")]
        public string Shift { get; set; } = "";
    }
}