using System.ComponentModel.DataAnnotations;

namespace DocumentTemplateSystem.Models.DTO
{
    /// <summary>
    /// Yêu cầu tạo mới tài liệu từ template
    /// </summary>
    public class CreateDocumentFromTemplateRequest
    {
        public int TemplateId { get; set; }
        public string? CustomTitle { get; set; }
        public string? AuthorName { get; set; }
    }

    /// <summary>
    /// Yêu cầu lưu nội dung tài liệu người dùng đã chỉnh sửa trong editor
    /// </summary>
    public class SaveDocumentRequest
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string DocumentTitle { get; set; } = string.Empty;

        [Required, StringLength(500000)]
        public string ContentHtml { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "Draft";
    }

    /// <summary>
    /// Yêu cầu thêm mẫu văn bản mới vào thư viện (không cần viết lại giao diện)
    /// </summary>
    public class CreateTemplateRequest
    {
        [Required, StringLength(50), RegularExpression(@"^[A-Za-z0-9_-]+$")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        [StringLength(4000)]
        public string? Description { get; set; }

        [Required, StringLength(500000)]
        public string ContentHtml { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Tags { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
