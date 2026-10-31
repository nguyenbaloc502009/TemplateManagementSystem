# TÀI LIỆU CẤU TRÚC DỮ LIỆU & API CHI TIẾT
## HỆ THỐNG QUẢN LÝ MẪU VĂN BẢN & TRÌNH CHIẾU

Hợp đồng OpenAPI máy đọc được có tại [openapi.yaml](./openapi.yaml). Ứng dụng không host Swagger UI. Danh sách/xem template là công khai; các API tạo template và thao tác tài liệu/deck yêu cầu đăng nhập bằng cookie. Yêu cầu thay đổi dữ liệu phải gửi anti-forgery token trong header `X-CSRF-TOKEN`. Một số lỗi validation do `[ApiController]` trả theo `ValidationProblemDetails`; lỗi nghiệp vụ do controller trả theo `{status,data,message}`.

---

## 1. CẤU TRÚC DỮ LIỆU CƠ SỞ DỮ LIỆU POSTGRESQL

Các cột `file_path` là object key tương đối trong bucket MinIO riêng tư, không phải đường dẫn ổ đĩa hoặc URL công khai. Backend sử dụng key để tải file sau khi xác thực quyền truy cập.

### 1.0 Bảng `app_users` (Tài khoản)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | UUID | PRIMARY KEY | Định danh tài khoản |
| `email` | VARCHAR(320) | UNIQUE không phân biệt hoa/thường | Email đăng nhập |
| `display_name` | VARCHAR(100) | NOT NULL | Tên hiển thị và tác giả mặc định |
| `password_hash` | TEXT | NOT NULL | Mật khẩu đã băm bằng ASP.NET Core PasswordHasher |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Thời điểm đăng ký |

### 1.1 Bảng `doc_categories` (Danh mục văn bản)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | SERIAL | PRIMARY KEY | Khóa chính tự tăng |
| `code` | VARCHAR(50) | NOT NULL, UNIQUE | Mã định danh danh mục (VD: `HR`, `LEGAL`) |
| `name` | VARCHAR(100) | NOT NULL | Tên hiển thị danh mục |
| `icon` | VARCHAR(50) | DEFAULT 'bi-folder' | Biểu tượng icon Bootstrap |
| `description` | TEXT | NULL | Mô tả phạm vi danh mục |
| `display_order` | INT | DEFAULT 0 | Thứ tự sắp xếp hiển thị |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Thời điểm tạo |

### 1.2 Bảng `doc_templates` (Mẫu văn bản gốc)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | SERIAL | PRIMARY KEY | Khóa chính tự tăng |
| `code` | VARCHAR(50) | NOT NULL, UNIQUE | Mã mẫu văn bản (VD: `DOC-001`) |
| `title` | VARCHAR(255) | NOT NULL | Tên mẫu văn bản |
| `category_id` | INT | REFERENCES `doc_categories(id)` | Khóa ngoại danh mục |
| `description` | TEXT | NULL | Tóm tắt mục đích mẫu |
| `thumbnail_url`| VARCHAR(500) | NULL | Đường dẫn ảnh thumbnail |
| `file_path` | VARCHAR(500) | NULL | Object key của file DOCX trong bucket MinIO (ví dụ `storage/templates/DOC-001.docx`) |
| `content_html` | TEXT | NOT NULL | Khung nội dung HTML chuẩn in ấn |
| `tags` | VARCHAR(255) | NULL | Từ khóa tìm kiếm |
| `usage_count` | INT | DEFAULT 0 | Số lượt người dùng đã khởi tạo tài liệu từ mẫu |
| `is_active` | BOOLEAN | DEFAULT TRUE | Trạng thái sử dụng |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Ngày tạo mẫu |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Ngày cập nhật gần nhất |

### 1.3 Bảng `doc_user_documents` (Tài liệu người dùng)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | SERIAL | PRIMARY KEY | Khóa chính tài liệu người dùng |
| `owner_user_id` | UUID | REFERENCES `app_users(id)` | Chủ sở hữu; truy vấn chỉ trả dữ liệu thuộc tài khoản đang đăng nhập |
| `template_id` | INT | REFERENCES `doc_templates(id)` | Khóa ngoại tham chiếu mẫu gốc |
| `document_title`| VARCHAR(255)| NOT NULL | Tiêu đề tài liệu cá nhân |
| `content_html` | TEXT | NOT NULL | Nội dung người dùng đã chỉnh sửa |
| `file_path` | VARCHAR(500) | NULL | Object key file DOCX trong MinIO |
| `author_name` | VARCHAR(100) | DEFAULT 'Người dùng' | Tên người soạn thảo |
| `status` | VARCHAR(50) | DEFAULT 'Draft' | Trạng thái: `Draft`, `Saved`, `Exported` |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Thời điểm tạo |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Thời điểm lưu gần nhất |

### 1.4 Bảng `presentation_templates` (Bộ mẫu trình chiếu gốc)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | SERIAL | PRIMARY KEY | Khóa chính bộ slide |
| `code` | VARCHAR(50) | NOT NULL, UNIQUE | Mã bộ template (VD: `PPT-001`) |
| `title` | VARCHAR(255) | NOT NULL | Tên bộ slide |
| `category_id` | INT | REFERENCES `presentation_categories(id)` | Khóa ngoại danh mục |
| `description` | TEXT | NULL | Mô tả chủ đề bài thuyết trình |
| `file_path` | VARCHAR(500) | NULL | Object key file PPTX trong MinIO |
| `total_slides` | INT | DEFAULT 1 | Tổng số trang slide trong bộ |
| `tags` | VARCHAR(255) | NULL | Thẻ tìm kiếm |
| `usage_count` | INT | DEFAULT 0 | Số lượt sử dụng bộ template |

### 1.5 Bảng `presentation_slides` (Các trang slide trong bộ template)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | SERIAL | PRIMARY KEY | Khóa chính slide |
| `template_id` | INT | REFERENCES `presentation_templates(id)` ON DELETE CASCADE | Khóa ngoại bộ mẫu |
| `slide_number` | INT | NOT NULL | Thứ tự trang slide (1, 2, 3...) |
| `slide_title` | VARCHAR(255) | NOT NULL | Tiêu đề chính của slide |
| `subtitle` | VARCHAR(255) | NULL | Phụ đề slide |
| `content_body` | TEXT | NULL | Nội dung chi tiết / Bullet points |
| `notes` | TEXT | NULL | Ghi chú của diễn giả |
| `layout_type` | VARCHAR(50) | DEFAULT 'TitleAndContent' | Bố cục: `TitleSlide`, `TitleAndContent`, `TwoColumn` |
| `image_url` | VARCHAR(500) | NULL | Ảnh minh họa xem trước |

### 1.6 Bảng `presentation_user_decks` (Bản trình chiếu người dùng)
| Tên cột | Kiểu dữ liệu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | SERIAL | PRIMARY KEY | Khóa chính bản trình chiếu |
| `owner_user_id` | UUID | REFERENCES `app_users(id)` | Chủ sở hữu; truy vấn chỉ trả dữ liệu thuộc tài khoản đang đăng nhập |
| `template_id` | INT | REFERENCES `presentation_templates(id)` | Tham chiếu bộ template gốc |
| `deck_title` | VARCHAR(255) | NOT NULL | Tiêu đề bài thuyết trình cá nhân |
| `author_name` | VARCHAR(100) | DEFAULT 'Người dùng' | Tên tác giả |
| `file_path` | VARCHAR(500) | NULL | Object key file PPTX trong MinIO |
| `deck_data_json`| TEXT | NOT NULL | JSON lưu trữ mảng các slide đã biên tập |
| `status` | VARCHAR(50) | DEFAULT 'Draft' | Trạng thái: `Draft`, `Saved` |
| `created_at` | TIMESTAMP | DEFAULT NOW() | Ngày khởi tạo |
| `updated_at` | TIMESTAMP | DEFAULT NOW() | Ngày lưu gần nhất |

---

## 2. TÀI LIỆU RESTFUL API CHI TIẾT

### 2.1 API SẢN PHẨM 1: MẪU VĂN BẢN (`http://localhost:5001/api`)

#### 1. Lấy danh sách mẫu văn bản (Tra cứu & Lọc)
- **Method**: `GET`
- **URL**: `/api/templates`
- **Query Parameters**:
  - `categoryId` (tùy chọn, int): ID danh mục cần lọc (1: Nhân sự, 2: Hành chính, 3: Pháp lý, ...)
  - `keyword` (tùy chọn, string): Từ khóa tìm kiếm theo tên hoặc mô tả
- **Response (200 OK)**:
```json
{
  "status": 200,
  "data": [
    {
      "id": 1,
      "code": "DOC-001",
      "title": "Đơn Xin Nghỉ Phép Tiêu Chuẩn",
      "categoryId": 1,
      "categoryName": "Nhân sự & Lao động",
      "description": "Mẫu đơn xin nghỉ phép chuẩn áp dụng cho cán bộ nhân viên...",
      "thumbnailUrl": "/uploads/thumbnails/DOC-001.svg",
      "contentHtml": "<div>...</div>",
      "tags": "Nhân sự, Nghỉ phép, Đơn từ",
      "usageCount": 20
    }
  ],
  "message": "Tìm thấy 5 mẫu văn bản"
}
```

#### 2. Lấy thông tin chi tiết mẫu văn bản để xem trước (Preview)
- **Method**: `GET`
- **URL**: `/api/templates/{id}`
- **Response (200 OK)**:
```json
{
  "status": 200,
  "data": {
    "id": 1,
    "code": "DOC-001",
    "title": "Đơn Xin Nghỉ Phép Tiêu Chuẩn",
    "contentHtml": "<div style=\"font-family: Arial...\">...</div>"
  },
  "message": "Thành công"
}
```

#### 3. Tạo tài liệu mới từ mẫu (Bảo vệ mẫu gốc)
- **Method**: `POST`
- **URL**: `/api/templates/{id}/documents`
- **Request Body**:
```json
{
  "templateId": 1,
  "customTitle": "Đơn Xin Nghỉ Phép - Nguyễn Văn A",
  "authorName": "Nguyễn Văn A"
}
```
- **Response (201 Created)**:
```json
{
  "status": 201,
  "data": {
    "id": 15,
    "templateId": 1,
    "documentTitle": "Đơn Xin Nghỉ Phép - Nguyễn Văn A",
    "contentHtml": "<div>...</div>",
    "status": "Draft"
  },
  "message": "Khởi tạo tài liệu mới thành công"
}
```

#### 4. Lưu nội dung tài liệu người dùng đã chỉnh sửa
- **Method**: `PUT`
- **URL**: `/api/documents/{id}`
- **Request Body**:
```json
{
  "id": 15,
  "documentTitle": "Đơn Xin Nghỉ Phép - Nguyễn Văn A (Đã cập nhật)",
  "contentHtml": "<div>Nội dung sau khi sửa trong trình soạn thảo...</div>",
  "status": "Saved"
}
```
- **Response (200 OK)**:
```json
{
  "status": 200,
  "data": { "id": 15 },
  "message": "Đã lưu tài liệu thành công vào cơ sở dữ liệu"
}
```

#### 5. Tải xuống file Microsoft Word (.docx) bản địa
- **Method**: `GET`
- **URL**: `/api/documents/{id}/file`
- **Response**: Trả về stream nhị phân (Binary Stream) với Header:
  - `Content-Type`: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
  - `Content-Disposition`: `attachment; filename="Don_Xin_Nghi_Phep_20261030.docx"`

---

### 2.2 API SẢN PHẨM 2: MẪU TRÌNH CHIẾU (`http://localhost:5002/api`)

#### 1. Lấy danh sách bộ mẫu slide
- **Method**: `GET`
- **URL**: `/api/presentations/templates`
- **Query Parameters**:
  - `categoryId` (int, tùy chọn): Phân loại (1: Khởi nghiệp, 2: Dự án, 3: Marketing, 4: Tài chính)
  - `keyword` (string, tùy chọn): Từ khóa tìm kiếm
- **Response (200 OK)**: Trả về danh sách bộ slide kèm theo mảng `slides` con của từng bộ.

#### 2. Lấy chi tiết bộ slide xem trước (Gallery Carousel)
- **Method**: `GET`
- **URL**: `/api/presentations/templates/{id}`
- **Response (200 OK)**: Trả về thông tin bộ template và toàn bộ danh sách slide chi tiết.

#### 3. Khởi tạo bản trình chiếu độc lập từ bộ mẫu
- **Method**: `POST`
- **URL**: `/api/presentations/templates/{id}/decks`
- **Request Body**:
```json
{
  "templateId": 1,
  "customTitle": "Bản Thuyết Trình Gọi Vốn - Công Ty NewTech",
  "authorName": "Đội Ngũ Sáng Lập"
}
```

#### 4. Lưu bản trình chiếu người dùng
- **Method**: `PUT`
- **URL**: `/api/presentations/decks/{id}`
- **Request Body**:
```json
{
  "id": 8,
  "deckTitle": "Bản Thuyết Trình Gọi Vốn - Công Ty NewTech",
  "slides": [
    {
      "slideNumber": 1,
      "slideTitle": "NEWTECH AI PLATFORM",
      "subtitle": "Kêu gọi vốn vòng Seed",
      "contentBody": "Trình bày bởi Founder A",
      "notes": "Nhấn mạnh thị trường"
    },
    {
      "slideNumber": 2,
      "slideTitle": "VẤN ĐỀ VÀ THÁCH THỨC",
      "contentBody": "• Doanh nghiệp tốn 70% thời gian soạn tài liệu\n• Thiếu giải pháp tập trung"
    }
  ],
  "status": "Saved"
}
```

#### 5. Tải xuống file Microsoft PowerPoint (.pptx) bản địa
- **Method**: `GET`
- **URL**: `/api/presentations/decks/{id}/file`
- **Response**: Trả về stream nhị phân với Header:
  - `Content-Type`: `application/vnd.openxmlformats-officedocument.presentationml.presentation`
  - `Content-Disposition`: `attachment; filename="Slide_Goi_Von_NewTech.pptx"`

### 2.3 Tương thích URL cũ

Các URL cũ như `/api/DocTemplateApi/list`, `/api/DocTemplateApi/create-document`,
`/api/PresentationApi/list` và `/api/PresentationApi/create-deck` vẫn được giữ làm alias.
Các ứng dụng web và tài liệu mới nên dùng các endpoint REST ở trên.
