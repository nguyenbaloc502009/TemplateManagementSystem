namespace PresentationTemplateSystem.Queries
{
    public static class CategoryQueries
    {
        /// <summary>Lấy toàn bộ danh mục và sắp xếp theo thứ tự hiển thị đã cấu hình.</summary>
        public const string SelectAllCategories = @"
            SELECT 
                id AS Id,
                code AS Code,
                name AS Name,
                icon AS Icon,
                description AS Description,
                display_order AS DisplayOrder,
                created_at AS CreatedAt
            FROM presentation_categories
            ORDER BY display_order ASC, id ASC;
        ";

        /// <summary>Lấy một danh mục theo mã định danh.</summary>
        public const string SelectCategoryById = @"
            SELECT 
                id AS Id,
                code AS Code,
                name AS Name,
                icon AS Icon,
                description AS Description,
                display_order AS DisplayOrder,
                created_at AS CreatedAt
            FROM presentation_categories
            WHERE id = @id::int;
        ";
    }

    public static class TemplateQueries
    {
        /// <summary>Lấy các bộ slide đang hoạt động cùng tên danh mục, có lọc tùy chọn theo danh mục và từ khóa.</summary>
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
                t.total_slides AS TotalSlides,
                t.tags AS Tags,
                t.usage_count AS UsageCount,
                t.is_active AS IsActive,
                t.created_at AS CreatedAt,
                t.updated_at AS UpdatedAt
            FROM presentation_templates t
            LEFT JOIN presentation_categories c ON t.category_id = c.id
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

        /// <summary>Lấy chi tiết một bộ slide theo ID, kèm tên danh mục nếu có.</summary>
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
                t.total_slides AS TotalSlides,
                t.tags AS Tags,
                t.usage_count AS UsageCount,
                t.is_active AS IsActive,
                t.created_at AS CreatedAt,
                t.updated_at AS UpdatedAt
            FROM presentation_templates t
            LEFT JOIN presentation_categories c ON t.category_id = c.id
            WHERE t.id = @id::int;
        ";

        /// <summary>Tạo bộ slide đang hoạt động với tổng số slide và bộ đếm sử dụng ban đầu.</summary>
        public const string InsertTemplate = @"
            INSERT INTO presentation_templates (
                code, title, category_id, description, 
                thumbnail_url, file_path, total_slides, tags, usage_count, is_active, created_at, updated_at
            ) VALUES (
                @code, @title, @categoryId::int, @description, 
                @thumbnailUrl, @filePath, @totalSlides, @tags, 0, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
            ) RETURNING id;
        ";

        /// <summary>Tăng bộ đếm sử dụng của bộ mẫu và cập nhật thời điểm sửa đổi.</summary>
        public const string IncrementUsageCount = @"
            UPDATE presentation_templates
            SET usage_count = usage_count + 1,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id::int;
        ";

        /// <summary>Đồng bộ khóa file PPTX và URL thumbnail với bản ghi bộ slide.</summary>
        public const string UpdateTemplateAssets = @"
            UPDATE presentation_templates
            SET file_path = @filePath, thumbnail_url = @thumbnailUrl, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id::int;
        ";
    }

    public static class SlideQueries
    {
        /// <summary>Lấy các slide thuộc một bộ mẫu theo đúng thứ tự hiển thị.</summary>
        public const string SelectSlidesByTemplateId = @"
            SELECT 
                id AS Id,
                template_id AS TemplateId,
                slide_number AS SlideNumber,
                slide_title AS SlideTitle,
                subtitle AS Subtitle,
                content_body AS ContentBody,
                notes AS Notes,
                layout_type AS LayoutType,
                image_url AS ImageUrl,
                created_at AS CreatedAt
            FROM presentation_slides
            WHERE template_id = @templateId::int
            ORDER BY slide_number ASC;
        ";

        /// <summary>Thêm slide cùng nội dung, ghi chú và ảnh xem trước vào bộ mẫu.</summary>
        public const string InsertSlide = @"
            INSERT INTO presentation_slides (
                template_id, slide_number, slide_title, subtitle, content_body, notes, layout_type, image_url, created_at
            ) VALUES (
                @templateId::int, @slideNumber, @slideTitle, @subtitle, @contentBody, @notes, @layoutType, @imageUrl, CURRENT_TIMESTAMP
            ) RETURNING id;
        ";

        /// <summary>Cập nhật URL ảnh xem trước của một slide.</summary>
        public const string UpdateSlideImageUrl = @"
            UPDATE presentation_slides
            SET image_url = @imageUrl
            WHERE id = @id::int;
        ";
    }

    public static class UserDeckQueries
    {
        /// <summary>Tạo deck riêng, lưu chủ sở hữu và dữ liệu slide dạng JSON.</summary>
        public const string InsertUserDeck = @"
            INSERT INTO presentation_user_decks (
                owner_user_id, template_id, deck_title, author_name, file_path, deck_data_json, status, created_at, updated_at
            ) VALUES (
                @ownerUserId, @templateId::int, @deckTitle, @authorName, @filePath, @deckDataJson, @status, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
            ) RETURNING id;
        ";

        /// <summary>Lấy deck cùng thông tin mẫu gốc, giới hạn theo chủ sở hữu.</summary>
        public const string SelectUserDeckById = @"
            SELECT 
                d.id AS Id,
                d.template_id AS TemplateId,
                d.deck_title AS DeckTitle,
                d.author_name AS AuthorName,
                d.file_path AS FilePath,
                d.deck_data_json AS DeckDataJson,
                d.status AS Status,
                d.created_at AS CreatedAt,
                d.updated_at AS UpdatedAt,
                t.title AS TemplateTitle,
                t.file_path AS TemplateFilePath,
                c.name AS CategoryName
            FROM presentation_user_decks d
            LEFT JOIN presentation_templates t ON d.template_id = t.id
            LEFT JOIN presentation_categories c ON t.category_id = c.id
            WHERE d.id = @id::int AND d.owner_user_id = @ownerUserId;
        ";

        /// <summary>Liệt kê deck của chủ sở hữu, sắp xếp theo thời gian cập nhật mới nhất.</summary>
        public const string SelectAllUserDecks = @"
            SELECT 
                d.id AS Id,
                d.template_id AS TemplateId,
                d.deck_title AS DeckTitle,
                d.author_name AS AuthorName,
                d.file_path AS FilePath,
                d.deck_data_json AS DeckDataJson,
                d.status AS Status,
                d.created_at AS CreatedAt,
                d.updated_at AS UpdatedAt,
                t.title AS TemplateTitle,
                t.file_path AS TemplateFilePath,
                c.name AS CategoryName
            FROM presentation_user_decks d
            LEFT JOIN presentation_templates t ON d.template_id = t.id
            LEFT JOIN presentation_categories c ON t.category_id = c.id
            WHERE d.owner_user_id = @ownerUserId
            ORDER BY d.updated_at DESC, d.id DESC;
        ";

        /// <summary>Cập nhật tiêu đề, dữ liệu slide và trạng thái của deck thuộc chủ sở hữu.</summary>
        public const string UpdateUserDeck = @"
            UPDATE presentation_user_decks
            SET 
                deck_title = @deckTitle,
                deck_data_json = @deckDataJson,
                status = @status,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id::int AND owner_user_id = @ownerUserId;
        ";

        /// <summary>Cập nhật khóa file lưu trữ của deck thuộc chủ sở hữu.</summary>
        public const string UpdateUserDeckFilePath = @"
            UPDATE presentation_user_decks
            SET file_path = @filePath, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id::int AND owner_user_id = @ownerUserId;
        ";

        /// <summary>Xóa deck khi cả ID và chủ sở hữu cùng khớp.</summary>
        public const string DeleteUserDeck = @"
            DELETE FROM presentation_user_decks
            WHERE id = @id::int AND owner_user_id = @ownerUserId;
        ";
    }
}
