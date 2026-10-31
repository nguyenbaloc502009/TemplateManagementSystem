-- ========================================================
-- DATABASE SCHEMA AND MIGRATIONS: Template Management System (PostgreSQL)
-- Dự án 1: DocumentTemplateSystem (Thư viện mẫu văn bản)
-- Dự án 2: PresentationTemplateSystem (Thư viện mẫu trình chiếu)
-- Safe to run on a new database or an existing database; does not drop data.
-- ========================================================

BEGIN;

-- --------------------------------------------------------
-- 1. BẢNG DÀNH CHO DỰ ÁN 1: THƯ VIỆN MẪU VĂN BẢN
-- --------------------------------------------------------

-- 1.1 Danh mục mẫu văn bản
CREATE TABLE IF NOT EXISTS doc_categories (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    icon VARCHAR(50) DEFAULT 'bi-folder',
    description TEXT,
    display_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS app_users (
    id UUID PRIMARY KEY,
    email VARCHAR(320) NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_app_users_email_normalized ON app_users (lower(email));

-- 1.2 Mẫu văn bản (Template gốc - KHÔNG ĐƯỢC THAY ĐỔI KHI TẠO TÀI LIỆU MỚI)
CREATE TABLE IF NOT EXISTS doc_templates (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(255) NOT NULL,
    category_id INT REFERENCES doc_categories(id) ON DELETE SET NULL,
    description TEXT,
    thumbnail_url VARCHAR(500),
    file_path VARCHAR(500), -- MinIO object key, e.g. storage/templates/DOC-001.docx
    content_html TEXT NOT NULL,
    tags VARCHAR(255),
    usage_count INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 1.3 Tài liệu người dùng tạo ra từ mẫu (Độc lập với mẫu gốc)
CREATE TABLE IF NOT EXISTS doc_user_documents (
    id SERIAL PRIMARY KEY,
    owner_user_id UUID REFERENCES app_users(id) ON DELETE CASCADE,
    template_id INT REFERENCES doc_templates(id) ON DELETE SET NULL,
    document_title VARCHAR(255) NOT NULL,
    content_html TEXT NOT NULL,
    file_path VARCHAR(500), -- MinIO object key
    author_name VARCHAR(100) DEFAULT 'Người dùng',
    status VARCHAR(50) DEFAULT 'Draft',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- --------------------------------------------------------
-- 2. BẢNG DÀNH CHO DỰ ÁN 2: THƯ VIỆN MẪU TRÌNH CHIẾU (POWERPOINT)
-- --------------------------------------------------------

-- 2.1 Danh mục mẫu trình chiếu
CREATE TABLE IF NOT EXISTS presentation_categories (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    icon VARCHAR(50) DEFAULT 'bi-easel',
    description TEXT,
    display_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2.2 Bộ mẫu trình chiếu PowerPoint (Template gốc)
CREATE TABLE IF NOT EXISTS presentation_templates (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    title VARCHAR(255) NOT NULL,
    category_id INT REFERENCES presentation_categories(id) ON DELETE SET NULL,
    description TEXT,
    thumbnail_url VARCHAR(500),
    file_path VARCHAR(500), -- MinIO object key, e.g. storage/templates/PPT-001.pptx
    total_slides INT DEFAULT 1,
    tags VARCHAR(255),
    usage_count INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2.3 Các slide xem trước và nội dung trong bộ template
CREATE TABLE IF NOT EXISTS presentation_slides (
    id SERIAL PRIMARY KEY,
    template_id INT NOT NULL REFERENCES presentation_templates(id) ON DELETE CASCADE,
    slide_number INT NOT NULL,
    slide_title VARCHAR(255) NOT NULL,
    subtitle VARCHAR(255),
    content_body TEXT,
    notes TEXT,
    layout_type VARCHAR(50) DEFAULT 'TitleAndContent',
    image_url VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2.4 Bản trình chiếu người dùng tạo ra từ template (Độc lập với template gốc)
CREATE TABLE IF NOT EXISTS presentation_user_decks (
    id SERIAL PRIMARY KEY,
    owner_user_id UUID REFERENCES app_users(id) ON DELETE CASCADE,
    template_id INT REFERENCES presentation_templates(id) ON DELETE SET NULL,
    deck_title VARCHAR(255) NOT NULL,
    author_name VARCHAR(100) DEFAULT 'Người dùng',
    file_path VARCHAR(500), -- MinIO object key
    deck_data_json TEXT NOT NULL,
    status VARCHAR(50) DEFAULT 'Draft',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tạo Index tăng tốc tìm kiếm
CREATE INDEX IF NOT EXISTS idx_doc_templates_title ON doc_templates(title);
CREATE INDEX IF NOT EXISTS idx_doc_templates_category ON doc_templates(category_id);
CREATE INDEX IF NOT EXISTS idx_pres_templates_title ON presentation_templates(title);
CREATE INDEX IF NOT EXISTS idx_pres_templates_category ON presentation_templates(category_id);
CREATE INDEX IF NOT EXISTS idx_pres_slides_template ON presentation_slides(template_id, slide_number);

-- Migrate databases created before account ownership was added.
ALTER TABLE doc_user_documents
    ADD COLUMN IF NOT EXISTS owner_user_id UUID REFERENCES app_users(id) ON DELETE CASCADE;

ALTER TABLE presentation_user_decks
    ADD COLUMN IF NOT EXISTS owner_user_id UUID REFERENCES app_users(id) ON DELETE CASCADE;

CREATE INDEX IF NOT EXISTS idx_doc_user_documents_owner
    ON doc_user_documents(owner_user_id, updated_at DESC);

CREATE INDEX IF NOT EXISTS idx_presentation_user_decks_owner
    ON presentation_user_decks(owner_user_id, updated_at DESC);

-- Remove denormalized category names from older database schemas.
ALTER TABLE doc_templates DROP COLUMN IF EXISTS category_name;
ALTER TABLE presentation_templates DROP COLUMN IF EXISTS category_name;

UPDATE doc_templates
SET file_path = 'storage/templates/' || code || '.docx'
WHERE file_path IS DISTINCT FROM 'storage/templates/' || code || '.docx';

UPDATE presentation_templates
SET file_path = 'storage/templates/' || code || '.pptx'
WHERE file_path IS DISTINCT FROM 'storage/templates/' || code || '.pptx';

COMMIT;
