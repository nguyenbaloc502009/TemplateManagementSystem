namespace DocumentTemplateSystem.Queries
{
    /// <summary>
    /// Định nghĩa các câu truy vấn SQL cho Danh mục Mẫu Văn Bản
    /// </summary>
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
            FROM doc_categories
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
            FROM doc_categories
            WHERE id = @id;
        ";
    }
}
