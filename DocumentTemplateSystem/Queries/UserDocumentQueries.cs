namespace DocumentTemplateSystem.Queries
{
    /// <summary>
    /// Định nghĩa các câu truy vấn SQL cho Tài liệu Người Dùng Tạo Ra
    /// </summary>
    public static class UserDocumentQueries
    {
        /// <summary>Tạo tài liệu độc lập, gắn chủ sở hữu và thời điểm tạo/cập nhật.</summary>
        public const string InsertUserDocument = @"
            INSERT INTO doc_user_documents (
                owner_user_id, template_id, document_title, content_html, file_path, author_name, status, created_at, updated_at
            ) VALUES (
                @ownerUserId, @templateId, @documentTitle, @contentHtml, @filePath, @authorName, @status, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
            ) RETURNING id;
        ";

        /// <summary>Lấy tài liệu cùng thông tin mẫu gốc và danh mục, giới hạn theo chủ sở hữu.</summary>
        public const string SelectUserDocumentById = @"
            SELECT 
                d.id AS Id,
                d.template_id AS TemplateId,
                d.document_title AS DocumentTitle,
                d.content_html AS ContentHtml,
                d.file_path AS FilePath,
                d.author_name AS AuthorName,
                d.status AS Status,
                d.created_at AS CreatedAt,
                d.updated_at AS UpdatedAt,
                t.title AS TemplateTitle,
                c.name AS CategoryName
            FROM doc_user_documents d
            LEFT JOIN doc_templates t ON d.template_id = t.id
            LEFT JOIN doc_categories c ON t.category_id = c.id
            WHERE d.id = @id AND d.owner_user_id = @ownerUserId;
        ";

        /// <summary>Liệt kê tài liệu của chủ sở hữu, sắp xếp theo thời gian cập nhật mới nhất.</summary>
        public const string SelectAllUserDocuments = @"
            SELECT 
                d.id AS Id,
                d.template_id AS TemplateId,
                d.document_title AS DocumentTitle,
                d.content_html AS ContentHtml,
                d.file_path AS FilePath,
                d.author_name AS AuthorName,
                d.status AS Status,
                d.created_at AS CreatedAt,
                d.updated_at AS UpdatedAt,
                t.title AS TemplateTitle,
                c.name AS CategoryName
            FROM doc_user_documents d
            LEFT JOIN doc_templates t ON d.template_id = t.id
            LEFT JOIN doc_categories c ON t.category_id = c.id
            WHERE d.owner_user_id = @ownerUserId
            ORDER BY d.updated_at DESC, d.id DESC;
        ";

        /// <summary>Cập nhật nội dung, trạng thái và thời điểm sửa của tài liệu thuộc chủ sở hữu.</summary>
        public const string UpdateUserDocument = @"
            UPDATE doc_user_documents
            SET 
                document_title = @documentTitle,
                content_html = @contentHtml,
                status = @status,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND owner_user_id = @ownerUserId;
        ";

        /// <summary>Cập nhật khóa file lưu trữ của tài liệu thuộc chủ sở hữu.</summary>
        public const string UpdateUserDocumentFilePath = @"
            UPDATE doc_user_documents
            SET file_path = @filePath, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND owner_user_id = @ownerUserId;
        ";

        /// <summary>Xóa tài liệu khi cả ID và chủ sở hữu cùng khớp.</summary>
        public const string DeleteUserDocument = @"
            DELETE FROM doc_user_documents
            WHERE id = @id AND owner_user_id = @ownerUserId;
        ";
    }
}
