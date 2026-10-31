namespace DocumentTemplateSystem.Models.DB
{
    /// <summary>
    /// Thực thể Danh mục Mẫu Văn Bản
    /// </summary>
    public class DocCategoryEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-folder";
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

