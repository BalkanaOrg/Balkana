using Balkana.Data.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Balkana.Models.Article
{
    public class ArticleFormModel
    {
        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public string ThumbnailUrl { get; set; }

        [Display(Name = "Team Logo")]
        public IFormFile? File { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public DateTime? PublishedAt { get; set; }

        // Draft, PendingReview, Published, Archived
        public string Status { get; set; } = "Draft";

        // Relation to User (Author)
        [ValidateNever]
        public string AuthorId { get; set; }

        [ValidateNever]
        public ApplicationUser Author { get; set; }
    }
}
