namespace DocumentTemplateSystem.Models.DB
{
    /// <summary>
    /// Thực thể Tài Liệu Người Dùng Tạo Ra (Độc lập hoàn toàn với Template gốc)
    /// </summary>
    public class DocUserDocumentEntity
    {
        public int Id { get; set; }
        public int? TemplateId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public string ContentHtml { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string AuthorName { get; set; } = "Người dùng";
        public string Status { get; set; } = "Draft"; // Draft, Completed, Exported
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Bổ sung thông tin mẫu gốc để hiển thị
        public string? TemplateTitle { get; set; }
        public string? CategoryName { get; set; }
    }
}

