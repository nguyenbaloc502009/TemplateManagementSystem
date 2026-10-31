using PresentationTemplateSystem.Models.DB;
using System.ComponentModel.DataAnnotations;

namespace PresentationTemplateSystem.Models.DTO
{
    public class CreateDeckFromTemplateRequest
    {
        public int TemplateId { get; set; }
        public string? CustomTitle { get; set; }
        public string? AuthorName { get; set; }
    }

    public class SlideItemDto
    {
        [Range(1, 1000)]
        public int SlideNumber { get; set; }

        [Required, StringLength(255)]
        public string SlideTitle { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Subtitle { get; set; }

        [StringLength(10000)]
        public string? ContentBody { get; set; }

        [StringLength(10000)]
        public string? Notes { get; set; }

        [StringLength(50)]
        public string LayoutType { get; set; } = "TitleAndContent";
    }

    public class SaveDeckRequest
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string DeckTitle { get; set; } = string.Empty;

        [Required, MinLength(1), MaxLength(100)]
        public List<SlideItemDto> Slides { get; set; } = new List<SlideItemDto>();

        [StringLength(50)]
        public string Status { get; set; } = "Draft";
    }

    public class CreatePresentationTemplateRequest
    {
        [Required, StringLength(50), RegularExpression(@"^[A-Za-z0-9_-]+$")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        [StringLength(4000)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? Tags { get; set; }

        [Required, MinLength(1), MaxLength(100)]
        public List<SlideItemDto> Slides { get; set; } = new List<SlideItemDto>();
    }
}
