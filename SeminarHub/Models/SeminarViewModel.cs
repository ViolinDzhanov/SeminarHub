using Microsoft.AspNetCore.Identity;
using SeminarHub.Data.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using static SeminarHub.Common.ModelConstants;

namespace SeminarHub.Models
{
    public class SeminarViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(TopicMaxLength, MinimumLength = TopicMinLength)]
        public string Topic { get; set; } = null!;

        [Required]
        [StringLength(LecturerMaxLength, MinimumLength = LecturerMinLength)]
        public string Lecturer { get; set; } = null!;

        [Required]
        [StringLength(DetailsMaxLength, MinimumLength = DetailsMinLength)]
        public string Details { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{2}/\d{2}/\d{4} \d{2}:\d{2}$")]
        public string DateAndTime { get; set; } = null!;

        [Required]
        [Range(DurationMinValue, DurationMaxValue)]
        public int? Duration { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}
