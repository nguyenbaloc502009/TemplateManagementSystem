using PresentationTemplateSystem.Models.DB;
using PresentationTemplateSystem.Models.DTO;

namespace PresentationTemplateSystem.Models.ViewModels
{
    public class PresentationListViewModel
    {
        public List<PresentationCategoryEntity> Categories { get; set; } = new List<PresentationCategoryEntity>();
        public List<PresentationTemplateEntity> Templates { get; set; } = new List<PresentationTemplateEntity>();
        public int? SelectedCategoryId { get; set; }
        public string? Keyword { get; set; }
        public int TotalCount { get; set; }
    }

    public class PresentationEditorViewModel
    {
        public PresentationUserDeckEntity Deck { get; set; } = new PresentationUserDeckEntity();
        public PresentationTemplateEntity? OriginalTemplate { get; set; }
        public List<SlideItemDto> Slides { get; set; } = new List<SlideItemDto>();
    }
}

