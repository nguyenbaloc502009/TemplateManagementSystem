namespace PresentationTemplateSystem.Models.DB
{
    public class PresentationCategoryEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-easel";
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PresentationTemplateEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? FilePath { get; set; }
        public int TotalSlides { get; set; } = 1;
        public string? Tags { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<PresentationSlideEntity> Slides { get; set; } = new List<PresentationSlideEntity>();
    }

    public class PresentationSlideEntity
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int SlideNumber { get; set; }
        public string SlideTitle { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? ContentBody { get; set; }
        public string? Notes { get; set; }
        public string LayoutType { get; set; } = "TitleAndContent";
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PresentationUserDeckEntity
    {
        public int Id { get; set; }
        public int? TemplateId { get; set; }
        public string DeckTitle { get; set; } = string.Empty;
        public string AuthorName { get; set; } = "Người dùng";
        public string? FilePath { get; set; }
        public string DeckDataJson { get; set; } = "[]";
        public string Status { get; set; } = "Draft";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? TemplateTitle { get; set; }
        public string? TemplateFilePath { get; set; }
        public string? CategoryName { get; set; }
    }
}
