using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyPracticeApp.Models
{
    public class Movie
    {
        public int MovieId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(120, ErrorMessage = "Title cannot exceed 120 characters.")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Director is required.")]
        [StringLength(80, ErrorMessage = "Director cannot exceed 80 characters.")]
        public string Director { get; set; } = "";

        [Required(ErrorMessage = "Rating is required.")]
        [Range(typeof(decimal), "0.0", "10.0", ErrorMessage = "Rating must be a number with up to one decimal place.")]
        public decimal Rating { get; set; }

        [Required(ErrorMessage = "Release Date is required.")]
        [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
        [Column(TypeName = "date")]
        public DateTime? ReleaseDate { get; set; }
    }
}