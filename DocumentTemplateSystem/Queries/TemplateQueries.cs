namespace DocumentTemplateSystem.Queries
{
    /// <summary>
    /// Định nghĩa các câu truy vấn SQL cho Mẫu Văn Bản Gốc
    /// </summary>
    public static class TemplateQueries
    {
        /// <summary>Lấy các mẫu đang hoạt động cùng tên danh mục, có lọc tùy chọn theo danh mục và từ khóa.</summary>
        public const string SelectTemplatesWithFilter = @"
            SELECT 
                t.id AS Id,
                t.code AS Code,
                t.title AS Title,
                t.category_id AS CategoryId,
                c.name AS CategoryName,
                t.description AS Description,
                t.thumbnail_url AS ThumbnailUrl,
                t.file_path AS FilePath,
                t.content_html AS ContentHtml,
                t.tags AS Tags,
                t.usage_count AS UsageCount,
                t.is_active AS IsActive,
                t.created_at AS CreatedAt,
                t.updated_at AS UpdatedAt
            FROM doc_templates t
            LEFT JOIN doc_categories c ON t.category_id = c.id
            WHERE t.is_active = TRUE
                AND (@categoryId::int IS NULL OR t.category_id = @categoryId::int)
                AND (
                    @keyword::text IS NULL 
                    OR @keyword::text = '' 
                    OR t.title ILIKE '%' || @keyword::text || '%' 
                    OR t.description ILIKE '%' || @keyword::text || '%'
                    OR t.tags ILIKE '%' || @keyword::text || '%'
                )
            ORDER BY t.id ASC;
        ";

        /// <summary>Lấy chi tiết mẫu theo ID, kèm tên danh mục nếu có.</summary>
        public const string SelectTemplateById = @"
            SELECT 
                t.id AS Id,
                t.code AS Code,
                t.title AS Title,
                t.category_id AS CategoryId,
                c.name AS CategoryName,
                t.description AS Description,
                t.thumbnail_url AS ThumbnailUrl,
                t.file_path AS FilePath,
                t.content_html AS ContentHtml,
                t.tags AS Tags,
                t.usage_count AS UsageCount,
                t.is_active AS IsActive,
                t.created_at AS CreatedAt,
                t.updated_at AS UpdatedAt
            FROM doc_templates t
            LEFT JOIN doc_categories c ON t.category_id = c.id
            WHERE t.id = @id::int;
        ";

        /// <summary>Tạo mẫu mới với trạng thái hoạt động và bộ đếm sử dụng ban đầu bằng 0.</summary>
        public const string InsertTemplate = @"
            INSERT INTO doc_templates (
                code, title, category_id, description, 
                thumbnail_url, file_path, content_html, tags, usage_count, is_active, created_at, updated_at
            ) VALUES (
                @code, @title, @categoryId::int, @description, 
                @thumbnailUrl, @filePath, @contentHtml, @tags, 0, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
            ) RETURNING id;
        ";

        /// <summary>Tăng bộ đếm sử dụng của mẫu và cập nhật thời điểm sửa đổi.</summary>
        public const string IncrementUsageCount = @"
            UPDATE doc_templates
            SET usage_count = usage_count + 1,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id::int;
        ";

        /// <summary>Đồng bộ khóa file và URL thumbnail với bản ghi mẫu.</summary>
        public const string UpdateTemplateAssets = @"
            UPDATE doc_templates
            SET file_path = @filePath, thumbnail_url = @thumbnailUrl, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id::int;
        ";
    }
}
