using DocumentTemplateSystem.Models.DB;

namespace DocumentTemplateSystem.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Thư viện Mẫu Văn Bản
    /// </summary>
    public class DocTemplateListViewModel
    {
        public List<DocCategoryEntity> Categories { get; set; } = new List<DocCategoryEntity>();
        public List<DocTemplateEntity> Templates { get; set; } = new List<DocTemplateEntity>();
        public int? SelectedCategoryId { get; set; }
        public string? Keyword { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// ViewModel cho màn hình Trình soạn thảo văn bản tích hợp
    /// </summary>
    public class DocEditorViewModel
    {
        public DocUserDocumentEntity Document { get; set; } = new DocUserDocumentEntity();
        public DocTemplateEntity? OriginalTemplate { get; set; }
        public bool IsNewDocument { get; set; }
    }
}

