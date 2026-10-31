namespace DocumentTemplateSystem.Models.DB
{
    /// <summary>
    /// Thực thể Mẫu Văn Bản Gốc (Template)
    /// Đảm bảo nguyên tắc: Mẫu gốc chỉ được đọc và nhân bản, KHÔNG ĐƯỢC THAY ĐỔI khi người dùng tạo tài liệu.
    /// </summary>
    public class DocTemplateEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? FilePath { get; set; }
        public string ContentHtml { get; set; } = string.Empty;
        public string? Tags { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

