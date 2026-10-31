# BÁO CÁO KỸ THUẬT VÀ TÀI LIỆU BÀN GIAO HỆ THỐNG
## DỰ ÁN 1: THƯ VIỆN MẪU VĂN BẢN (`DocumentTemplateSystem`)
## DỰ ÁN 2: THƯ VIỆN MẪU TRÌNH CHIẾU (`PresentationTemplateSystem`)

---

### GHI CHÚ RÀ SOÁT & KHẮC PHỤC (Code Review Changelog)

Các lỗi được sửa trong lần cập nhật này:

1. **`DocxExportHelper.cs` — Lỗi rò rỉ văn bản thô khi tài liệu chứa bảng (`<table>`)**: Bộ phân tích HTML trước đó chỉ nhận diện các thẻ `h1-h6, p, li, div`; thẻ `<table>/<tr>/<td>` không nằm trong danh sách này nên bị xử lý sai — dấu `<` của các thẻ `<td>` bị bỏ qua nhưng phần còn lại của thẻ (`td style="...">`) lại bị đọc nhầm thành văn bản hiển thị, dẫn đến file Word xuất ra chứa đoạn text rác thay vì bảng. Vì **cả 5/5 mẫu văn bản seed đều có bảng ký tên** ở cuối, lỗi này ảnh hưởng tới mọi lượt xuất DOCX. Đã khắc phục bằng cách: (a) dựng bảng Word OpenXML thật (`Table/TableRow/TableCell`) từ thẻ `<table>`, và (b) bổ sung cơ chế loại bỏ an toàn mọi thẻ HTML không được nhận diện trước khi tách nội dung, để tránh rò rỉ thuộc tính HTML ra văn bản trong mọi trường hợp tương tự về sau. Đã bổ sung unit test `GenerateDocx_WithHtmlTable_ShouldRenderRealTableWithoutLeakingRawTagText` để kiểm chứng.
2. **Lưu trữ file qua MinIO**: File DOCX/PPTX được lưu trong bucket riêng tư; PostgreSQL chỉ giữ object key (ví dụ `storage/templates/DOC-001.docx`). Khi tạo bản mới, ứng dụng đọc template từ MinIO và ghi bản sao mới vào MinIO. Nếu object template seed chưa tồn tại, ứng dụng tạo DOCX/PPTX từ nội dung HTML/slide trong PostgreSQL rồi lưu vào MinIO. Thumbnail SVG và ảnh preview vẫn nằm trong `wwwroot/uploads/`.
3. **Xuất PPTX**: Template nguồn có master, layout và theme; export ghi nội dung và speaker notes vào package. Khi số slide thay đổi, package được dựng lại theo màu chủ đạo. OpenXML được kiểm tra tự động; chưa mở xác nhận bằng PowerPoint hoặc LibreOffice.
4. **Tái cấu trúc và chuẩn hóa dữ liệu/API**: Hai ứng dụng dùng chung `TemplateManagement.Core` cho database manager, logger và API response; service được đăng ký qua DI. Các API REST được ưu tiên trong mã giao diện, URL cũ vẫn là alias. Tên danh mục được lấy từ bảng category thay vì lưu lặp trong bảng template.

---

### MỤC LỤC
1. [Tổng Quan Kiến Trúc & Công Nghệ](#1-tổng-quan-kiến-trúc--công-nghệ)
2. [Cấu Trúc Thư Mục & Kiến Trúc Hệ Thống](#2-cấu-trúc-thư-mục--kiến-trúc-hệ-thống)
3. [Thiết Kế Cơ Sở Dữ Liệu PostgreSQL Thật](#3-thiết-kế-cơ-sở-dữ-liệu-postgresql-thật)
4. [Nguyên Tắc Thiết Kế Cô Lập Template (Bảo Vệ File Mẫu Gốc)](#4-nguyên-tắc-thiết-kế-cô-lập-template-bảo-vệ-file-mẫu-gốc)
5. [Định Dạng Văn Bản & Xuất File (DOCX & PPTX Chuẩn OpenXML)](#5-định-dạng-văn-bản--xuất-file-docx--pptx-chuẩn-openxml)
6. [Tài Liệu Cấu Trúc API RESTful](#6-tài-liệu-cấu-trúc-api-restful)
7. [Thư Viện Bên Thứ Ba & Giấy Phép (License)](#7-thư-viện-bên-thứ-ba--giấy-phép-license)
8. [Hướng Dẫn Cài Đặt & Chạy Thử Thực Tế](#8-hướng-dẫn-cài-đặt--chạy-thử-thực-tế)
9. [Kết Quả Kiểm Thử Tự Động (Unit & Integration Tests)](#9-kết-quả-kiểm-thử-tự-động-unit--integration-tests)
10. [Danh Sách Hoàn Thành, Chưa Hoàn Thành & Hướng Mở Rộng](#10-danh-sách-hoàn-thành-chưa-hoàn-thành--hướng-mở-rộng)

---

## 1. TỔNG QUAN KIẾN TRÚC & CÔNG NGHỆ

Hệ thống là bản demo quản lý thư viện template và tài liệu Office:
- **Sản phẩm 1 (`DocumentTemplateSystem`)**: Thư viện mẫu văn bản cho phép tra cứu, lọc danh mục, xem trước mẫu trên khổ giấy A4 chuẩn in ấn, khởi tạo tài liệu mới độc lập, tích hợp trình soạn thảo WYSIWYG trực quan, lưu vào CSDL và xuất file Microsoft Word (`.docx`) giữ nguyên định dạng.
- **Sản phẩm 2 (`PresentationTemplateSystem`)**: Thư viện mẫu trình chiếu PowerPoint cho phép tìm kiếm, phân loại theo chủ đề, xem trước bộ slide (Slide Gallery Carousel duyệt qua từng slide), khởi tạo bản trình chiếu riêng biệt, biên tập slide trực tuyến (canvas 16:9, thêm/xóa/sửa slide, speaker notes) và xuất file PowerPoint (`.pptx`) bản địa.

Hai ứng dụng vẫn chạy độc lập, dùng chung các thành phần nền tảng trong `TemplateManagement.Core`; services và controllers nhận dependency qua ASP.NET Core DI.

### Công nghệ sử dụng:
| Tầng | Công nghệ / Framework | Vai trò / Tính năng nổi bật |
| :--- | :--- | :--- |
| **Backend** | .NET 8.0 C# (ASP.NET Core MVC & RESTful Web API) | Xử lý logic nghiệp vụ, định tuyến, kiểm tra đầu vào và API |
| **Cơ sở dữ liệu** | PostgreSQL | Lưu trữ danh mục, mẫu văn bản, bộ slide, tài liệu cá nhân |
| **Kết nối CSDL** | `Npgsql 8.0.5` | Driver PostgreSQL hiệu năng cao, parameterized query chống SQLi |
| **Lưu trữ file** | MinIO (`Minio` SDK 6.0.4) | Lưu file DOCX/PPTX bên ngoài source; database giữ object key |
| **Xử lý Office** | `DocumentFormat.OpenXml 3.1.1` | Chuẩn OpenXML chính thức từ Microsoft, xuất file DOCX & PPTX bản địa |
| **Frontend** | Razor Views, HTML5/CSS3, Bootstrap 5.3, Bootstrap Icons, jQuery | Giao diện hiện đại, responsive, trải nghiệm WYSIWYG mượt mà |
| **Kiểm thử** | NUnit 3, Microsoft.NET.Test.Sdk | Kiểm thử OpenXML, object storage giả lập trong bộ nhớ và luồng cô lập dữ liệu |

---

## 2. CẤU TRÚC THƯ MỤC & KIẾN TRÚC HỆ THỐNG

Hệ thống kế thừa chuẩn mực mô hình kiến trúc phân tầng (N-Tier Architecture):

```
TemplateManagementSystem\
├── TemplateManagementSystem.sln
├── TemplateManagement.Core\                    # API response, logging, database access dùng chung
├── Database\                                    # Schema, seed data và migration
├── DocumentTemplateSystem\                      # Ứng dụng thư viện văn bản (port 5001)
│   ├── Queries\                                 # SQL queries cho văn bản
│   ├── Services\                                # Nghiệp vụ template và tài liệu người dùng
│   ├── Controllers\                             # MVC và REST API
│   └── wwwroot\                                 # Assets, CSS và JavaScript
├── PresentationTemplateSystem\                  # Ứng dụng thư viện trình chiếu (port 5002)
│   ├── Queries\                                 # SQL queries cho slide và deck
│   ├── Services\                                # Nghiệp vụ template và deck người dùng
│   ├── Controllers\                             # MVC và REST API
│   └── wwwroot\                                 # Assets, CSS và JavaScript
└── TemplateSystem.Tests\                        # Unit và integration tests
```

---

## 3. THIẾT KẾ CƠ SỞ DỮ LIỆU POSTGRESQL THẬT

Hệ thống dùng PostgreSQL; `template_library_db` là tên cơ sở dữ liệu mẫu trong scripts:

### 3.1 Bảng Dữ Liệu Sản Phẩm 1: Mẫu Văn Bản
1. `doc_categories`: Danh mục mẫu văn bản (`id`, `code`, `name`, `icon`, `description`, `display_order`).
2. `doc_templates`: Mẫu văn bản gốc (`id`, `code`, `title`, `category_id`, `description`, `thumbnail_url`, `file_path`, `content_html`, `tags`, `usage_count`, `is_active`). Tên danh mục được lấy từ `doc_categories`, không lưu lặp.
3. `doc_user_documents`: Tài liệu người dùng tạo ra từ mẫu (`id`, `template_id`, `document_title`, `content_html`, `file_path`, `author_name`, `status`, `created_at`, `updated_at`).
4. Template DOCX và tài liệu người dùng được lưu trong bucket MinIO; database lưu object key trong `file_path`. Thumbnail SVG được tạo trong `DocumentTemplateSystem/wwwroot/uploads/thumbnails/`.

### 3.2 Bảng Dữ Liệu Sản Phẩm 2: Mẫu Trình Chiếu PowerPoint
1. `presentation_categories`: Phân loại mẫu trình chiếu (`id`, `code`, `name`, `icon`, `description`, `display_order`).
2. `presentation_templates`: Bộ mẫu PowerPoint gốc (`id`, `code`, `title`, `category_id`, `description`, `thumbnail_url`, `total_slides`, `tags`, `usage_count`). Tên danh mục được lấy từ `presentation_categories`, không lưu lặp.
3. `presentation_slides`: Danh sách slide xem trước và nội dung trong bộ template (`id`, `template_id`, `slide_number`, `slide_title`, `subtitle`, `content_body`, `notes`, `layout_type`, `image_url`).
4. `presentation_user_decks`: Bản trình chiếu người dùng tạo ra (`id`, `template_id`, `deck_title`, `author_name`, `file_path`, `deck_data_json`, `status`, `created_at`, `updated_at`).
5. Template PPTX và deck người dùng được lưu trong cùng bucket MinIO; database lưu object key trong `file_path`. Thumbnail và ảnh preview slide SVG nằm dưới `PresentationTemplateSystem/wwwroot/uploads/`.

### 3.3 Bộ Dữ Liệu Khởi Tạo Thử Nghiệm (Seed Data)
- **5 Mẫu văn bản chuẩn nghiệp vụ doanh nghiệp**:
  1. `DOC-001`: Đơn Xin Nghỉ Phép Tiêu Chuẩn (Nhân sự & Lao động)
  2. `DOC-002`: Hợp Đồng Lao Động Xác Định Thời Hạn (Pháp lý & Hợp đồng)
  3. `DOC-003`: Biên Bản Cuộc Họp Nội Bộ / Dự Án (Hành chính & Văn phòng)
  4. `DOC-004`: Thư Mời Hợp Tác Kinh Doanh & Giới Thiệu Giải Pháp (Kinh doanh & Đối tác)
  5. `DOC-005`: Báo Cáo Tiến Độ Dự Án Định Kỳ Tuần/Tháng (Báo cáo & Kế hoạch)
- **3 Bộ mẫu PowerPoint chuẩn đầy đủ slide xem trước**:
  1. `PPT-001`: Startup Investment Pitch Deck 2026 (5 slide: Bìa, Vấn đề, Giải pháp, Mô hình, Kêu gọi vốn)
  2. `PPT-002`: IT Project Sprint Review & Status Report (4 slide: Bìa, Kết quả Sprint, Kiến trúc, Kế hoạch phát hành)
  3. `PPT-003`: Digital Marketing Strategy & Growth Plan (4 slide: Bìa, Khách hàng mục tiêu, Kênh truyền thông, KPI)

---

## 4. NGUYÊN TẮC THIẾT KẾ CÔ LẬP TEMPLATE (BẢO VỆ FILE MẪU GỐC)

Một trong những yêu cầu sống còn của bài toán là: **"Việc tạo tài liệu mới không được làm thay đổi file template gốc"**.

### Giải pháp kỹ thuật được triển khai:
1. **Phân tách tầng dữ liệu độc lập**:
   - Bảng `doc_templates` và `presentation_templates` đóng vai trò là **Read-Only Blueprint** (Kho bản thiết kế chỉ đọc).
   - Khi người dùng bấm "Sử dụng mẫu này", tầng Service thực hiện nhân bản nội dung (`ContentHtml` hoặc `Slides`) sang một bản ghi mới trong bảng `doc_user_documents` / `presentation_user_decks` với một `Id` hoàn toàn mới.
   - File DOCX/PPTX template có sẵn trong repository và không được tạo lại khi chạy; ứng dụng tạo bản sao GUID riêng cho từng tài liệu/deck người dùng.
2. **Bảo vệ Template gốc**:
   - Thao tác duy nhất được phép ghi vào bảng template là tăng trường `usage_count` (+1 lượt sử dụng).
   - Mọi thao tác biên tập, lưu bản nháp, sửa đổi tiêu đề, xóa trang trong Editor chỉ được cập nhật trên bản ghi của tài liệu người dùng (`doc_user_documents`).
   - DOCX được sao chép thành file người dùng riêng; khi lưu/xuất chỉ file người dùng được cập nhật. PPTX được mở từ file template và các slide người dùng được cập nhật trong một gói mới, không ghi vào template.
3. **Đã kiểm thử tự động**:
   - Bài test `Product1_TemplateIsolation_CreatingUserDocument_MustNotModifyOriginalTemplate` và `Product2_PresentationDeckIsolation_CreatingUserDeck_MustNotModifyOriginalTemplate` tự động xác nhận rằng sau khi chỉnh sửa tài liệu người dùng, nội dung và tiêu đề của mẫu gốc vẫn không hề thay đổi.

---

## 5. ĐỊNH DẠNG VĂN BẢN & XUẤT FILE (DOCX & PPTX CHUẨN OPENXML)

### 5.1 Giữ nguyên định dạng khi xuất file Word (`.docx`):
- Hệ thống sử dụng thư viện chính thức của Microsoft: `DocumentFormat.OpenXml`.
- Module `DocxExportHelper.cs` phân tích cấu trúc HTML được biên tập:
  + Chuyển đổi các thẻ heading `h1`, `h2`, `h3` thành OpenXML Heading Paragraph với kích thước phông chữ và màu sắc tương ứng.
  + Bảo toàn định dạng in đậm (`strong`/`b`), in nghiêng (`em`/`i`), gạch chân (`u`).
  + Bảo toàn căn lề văn bản: Căn trái, Căn giữa, Căn phải, Căn đều hai bên (`JustificationValues`).
  + Hỗ trợ tạo bảng biểu (`Table`, `TableRow`, `TableCell`) và đường phân cách ngang (`hr`).
  + Hỗ trợ in chuẩn A4 và xuất PDF trực tiếp từ trình duyệt (`window.print()`).

### 5.2 Sinh file Microsoft PowerPoint (`.pptx`):
- File template PPTX được vật chất hóa từ các slide đã lưu trong CSDL. `PptxExportHelper.cs` tạo đủ thành phần OpenXML:
  + Thiết lập chuẩn tỷ lệ màn hình rộng **16:9** (9,144,000 x 5,143,500 EMUs).
  + Đăng ký `SlideMasterPart`, `SlideLayoutPart`, `ThemePart`, quan hệ slide và ghi chú diễn giả.
  + Khi số slide không đổi, cập nhật nội dung trong file template để giữ nguyên theme/layout/style; nếu thêm hoặc xóa slide, tái tạo package theo màu chủ đạo của bộ mẫu.
  + Bài test dùng `OpenXmlValidator` để kiểm tra cấu trúc package và notes. Hiệu ứng animation/transition không được hứa bảo toàn.

---

## 6. TÀI LIỆU CẤU TRÚC API RESTFUL

Các API JSON thường trả về định dạng:
```json
{
  "status": 200,
  "data": { ... },
  "message": "Thông điệp phản hồi"
}
```

### 6.1 API Sản Phẩm 1: `DocTemplateApiController`
| Phương thức | Endpoint | Mô tả |
| :--- | :--- | :--- |
| `GET` | `/api/templates?categoryId={id}&keyword={kw}` | Tìm kiếm và lọc danh mục mẫu văn bản |
| `GET` | `/api/templates/{id}` | Lấy chi tiết mẫu văn bản để xem trước |
| `POST` | `/api/templates` | Thêm mẫu văn bản mới vào thư viện |
| `POST` | `/api/templates/{id}/documents` | Khởi tạo tài liệu mới từ mẫu |
| `PUT` | `/api/documents/{id}` | Lưu nội dung tài liệu người dùng |
| `GET` | `/api/documents/{id}/file` | Xuất file Microsoft Word (.docx) |
| `DELETE` | `/api/documents/{id}` | Xóa tài liệu người dùng |

### 6.2 API Sản Phẩm 2: `PresentationApiController`
| Phương thức | Endpoint | Mô tả |
| :--- | :--- | :--- |
| `GET` | `/api/presentations/templates?categoryId={id}&keyword={kw}` | Tìm kiếm và lọc bộ template |
| `GET` | `/api/presentations/templates/{id}` | Lấy chi tiết bộ template và các slide |
| `POST` | `/api/presentations/templates` | Thêm bộ template slide |
| `POST` | `/api/presentations/templates/{id}/decks` | Tạo bản trình chiếu độc lập |
| `PUT` | `/api/presentations/decks/{id}` | Lưu các thay đổi trong trình biên tập |
| `GET` | `/api/presentations/decks/{id}/file` | Xuất file Microsoft PowerPoint (.pptx) |
| `DELETE` | `/api/presentations/decks/{id}` | Xóa bản trình chiếu |

Các URL API cũ vẫn được duy trì làm alias để tương thích ngược; giao diện hiện dùng các endpoint REST ở trên.
Khi `[ApiController]` tự động từ chối model không hợp lệ, ASP.NET Core có thể trả về `ValidationProblemDetails`. Xem [openapi.yaml](./openapi.yaml) cho hợp đồng endpoint hiện tại. API hiện chưa có xác thực hoặc phân quyền người dùng.

---

## 7. THƯ VIỆN BÊN THỨ BA & GIẤY PHÉP (LICENSE)

| Thư viện | Phiên bản | Mục đích sử dụng | Giấy phép (License) |
| :--- | :--- | :--- | :--- |
| **Npgsql** | 8.0.5 | Kết nối và thực thi truy vấn PostgreSQL | PostgreSQL License (Open Source) |
| **DocumentFormat.OpenXml** | 3.1.1 | Tạo và chỉnh sửa file Word (.docx) và PowerPoint (.pptx) | MIT License (Microsoft Open Source) |
| **Bootstrap** | 5.3.3 | Thiết kế giao diện responsive hiện đại | MIT License |
| **Bootstrap Icons** | 1.11.3 | Bộ icon đồ họa giao diện | MIT License |
| **jQuery** | 3.7.1 | Thao tác DOM và gọi AJAX RESTful API | MIT License |
| **NUnit** | 3.14.0 | Framework viết và chạy unit/integration test | MIT License |

---

## 8. HƯỚNG DẪN CÀI ĐẶT & CHẠY THỬ THỰC TẾ

### Bước 1: Khởi tạo Cơ sở dữ liệu PostgreSQL
Đảm bảo PostgreSQL đang chạy. Tạo database bằng tài khoản quản trị PostgreSQL của bạn; không dùng mật khẩu mặc định được ghi cứng trong tài liệu.
Chạy 2 file script có sẵn trong thư mục `Database`:
```powershell
# 1. Tạo database
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U <postgres-user> -p 5432 -c "CREATE DATABASE template_library_db;"

# 2. Tạo/cập nhật cấu trúc bảng và nạp dữ liệu mẫu
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U <postgres-user> -d template_library_db -p 5432 -f "Database\01_CREATE_SCHEMA.sql"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U <postgres-user> -d template_library_db -p 5432 -f "Database\02_SEED_DATA.sql"
```

### Bước 2: Cấu hình MinIO và chuyển file hiện có

Cả hai ứng dụng dùng MinIO/AIStor object storage trên `localhost:9000` và cùng bucket riêng tư `template-management`. Trong hướng dẫn này AIStor chạy trực tiếp trên Windows, độc lập với source. License và dữ liệu được lưu dưới `%LOCALAPPDATA%\TemplateManagementSystem`, ngoài thư mục source.

```powershell
# Khai báo cặp credentials mạnh; cả server và ứng dụng đọc từ User Secrets
dotnet user-secrets set "ObjectStorage:AccessKey" "<access-key>" --project DocumentTemplateSystem\DocumentTemplateSystem.csproj
dotnet user-secrets set "ObjectStorage:SecretKey" "<secret-key>" --project DocumentTemplateSystem\DocumentTemplateSystem.csproj

# Mở terminal này và giữ chạy để AIStor hoạt động
powershell -ExecutionPolicy Bypass -File "Database\Start-AIStor.ps1"
```

Mở terminal thứ hai, từ thư mục gốc solution:
```powershell
# Đẩy các template và file storage cũ lên đúng object key
powershell -ExecutionPolicy Bypass -File "Database\Upload-StorageToMinio.ps1"
```

Ứng dụng tự tạo bucket nếu chưa tồn tại. Script upload các file Office còn sót từ `storage/`, **không xóa** file nguồn và xác minh nội dung bằng SHA-256 sau mỗi lần tải. Với database cũ, chạy lại `Database\01_CREATE_SCHEMA.sql` để chuẩn hóa các `file_path` thành object key dạng `storage/templates/...`; script không xóa dữ liệu. Trong repository hiện không còn file Office dưới `storage/`; các template seed được tự sinh từ dữ liệu PostgreSQL khi cần. Không bật public access cho bucket; ứng dụng tải file qua backend sau khi kiểm tra quyền sở hữu.

Nếu `minio.exe` không nằm trong `Downloads`, đặt biến `MINIO_SERVER_EXE` thành đường dẫn đầy đủ trước khi chạy script. Server cần chạy liên tục; nhấn Ctrl+C để dừng và chạy lại script sau khi khởi động lại máy. Với triển khai production, cần quản lý license, TLS, tài khoản dịch vụ và sao lưu theo tài liệu AIStor.

Configure the connection string outside `appsettings.json`. Both applications and tests share the User Secrets ID `template-management-local`:
```powershell
dotnet user-secrets set "ConnectionStrings:PostgresDb" "Host=localhost;Port=5432;Database=template_library_db;Username=<db-user>;Password=<db-password>" --project DocumentTemplateSystem\DocumentTemplateSystem.csproj
```
For deployment, use the `ConnectionStrings__PostgresDb` environment variable or a secrets manager. Replace `<postgres-user>` and credentials with values for your local PostgreSQL installation.

`01_CREATE_SCHEMA.sql` tạo cấu trúc cho database mới hoặc cập nhật database hiện có. Script không xóa bảng/dữ liệu; nó cũng chuẩn hóa cột danh mục và thêm bảng tài khoản cùng quyền sở hữu. Chạy script này một lần trước khi khởi động ứng dụng. Tài khoản đăng ký được lưu trong `app_users`; tài liệu/deck mới được gắn với chủ sở hữu. Bản ghi cũ chưa có `owner_user_id` sẽ không hiển thị cho tài khoản nào.

### Đăng nhập và quyền sở hữu
Đăng ký tài khoản tại `/account/register` hoặc đăng nhập tại `/account/login`. Mật khẩu tối thiểu 12 ký tự được lưu bằng ASP.NET Core PasswordHasher; các trang/API tài liệu cá nhân yêu cầu cookie đăng nhập. API từ client ngoài giao diện cần giữ cookie và gửi anti-forgery token qua header `X-CSRF-TOKEN`. Tính năng xác minh email, khôi phục mật khẩu và giới hạn tốc độ đăng nhập chưa được triển khai.

### Bước 3: Chạy Sản Phẩm 1 - Thư Viện Mẫu Văn Bản (Port 5001)
Mở một cửa sổ Terminal:
```powershell
dotnet run --project DocumentTemplateSystem\DocumentTemplateSystem.csproj --urls "http://localhost:5001"
```
👉 Truy cập trên trình duyệt: **`http://localhost:5001`**

### Bước 4: Chạy Sản Phẩm 2 - Thư Viện Mẫu Trình Chiếu (Port 5002)
Mở cửa sổ Terminal thứ hai:
```powershell
dotnet run --project PresentationTemplateSystem\PresentationTemplateSystem.csproj --urls "http://localhost:5002"
```
👉 Truy cập trên trình duyệt: **`http://localhost:5002`**

> Có thể khởi động hoặc cập nhật cả hai ứng dụng cùng lúc bằng `.\Start-TemplateManagementSystem.ps1` tại thư mục gốc. Script chỉ khởi động lại tiến trình trên cổng 5001/5002 nếu tiến trình đó thuộc đúng ứng dụng trong dự án; tiến trình khác sẽ không bị dừng. Đảm bảo PostgreSQL và MinIO/AIStor đã được cấu hình trước khi chạy.

*(Cả hai sản phẩm có thanh menu trên cùng cho phép chuyển đổi qua lại chỉ với 1 cú click!)*

### Bước 5: Chạy Toàn Bộ Bộ Kiểm Thử Tự Động (Unit & Integration Tests)
```powershell
dotnet test TemplateSystem.Tests\TemplateSystem.Tests.csproj
```

---

OpenAPI contract: [openapi.yaml](./openapi.yaml). An OpenAPI file is provided for tooling; the apps do not host a Swagger UI.

## 9. KIỂM THỬ TỰ ĐỘNG

Chạy toàn bộ suite bằng `dotnet test TemplateSystem.Tests\TemplateSystem.Tests.csproj`. `DatabaseIntegrationTests` cần PostgreSQL đã được khởi tạo và connection string được cấu hình như trên; các unit tests về xuất Office chạy độc lập. DOCX/PPTX được kiểm tra bằng `OpenXmlValidator`. Đây không phải xác nhận mở bằng Microsoft Office/LibreOffice.

---

## 10. DANH SÁCH HOÀN THÀNH, CHƯA HOÀN THÀNH & HƯỚNG MỞ RỘNG

### 10.1 Các chức năng đã triển khai:
- [x] **Sản phẩm 1 - Thư viện mẫu văn bản**:
  - Giao diện lưới (Card Grid) và danh sách (Table view), badge danh mục, số lượt dùng, thumbnail.
  - Tìm kiếm từ khóa realtime kết hợp lọc danh mục.
  - Modal xem trước chi tiết nội dung mẫu trên khổ giấy A4.
  - Luồng sử dụng mẫu: Tạo tài liệu độc lập với mẫu gốc.
  - Trình soạn thảo văn bản tích hợp WYSIWYG: Thanh công cụ tiêu đề H1/H2/H3, in đậm, in nghiêng, gạch chân, căn lề, danh sách, chèn bảng biểu, đổi màu, hỗ trợ điền nhanh placeholder tag (`{{HoTen}}`, `{{NgayThang}}`).
  - Lưu tài liệu vào CSDL PostgreSQL, tự động lưu bản nháp sau mỗi 60 giây.
  - Xuất và tải xuống file Microsoft Word (`.docx`) giữ nguyên định dạng chuẩn OpenXML, hỗ trợ in / xuất PDF.
  - Thêm mẫu văn bản mới linh hoạt vào hệ thống mà không cần viết lại giao diện.
- [x] **Sản phẩm 2 - Thư viện mẫu trình chiếu**:
  - Giao diện hiển thị danh sách các bộ template PowerPoint với thumbnail 16:9.
  - Tìm kiếm từ khóa và phân loại theo danh mục.
  - Modal Slide Gallery Carousel duyệt qua từng slide xem trước của bộ template.
  - Tạo bản trình chiếu mới độc lập với bộ mẫu gốc.
  - Trình biên tập slide trực quan: Sidebar danh sách thumbnails, Slide canvas 16:9 trung tâm, chỉnh sửa tiêu đề, phụ đề, nội dung bullet points, speaker notes, thêm slide mới, xóa slide.
  - Lưu bản trình chiếu vào CSDL PostgreSQL.
  - Xuất file Microsoft PowerPoint (`.pptx`) theo OpenXML; hiện xác thực package bằng OpenXmlValidator, chưa kiểm tra thủ công bằng MS PowerPoint/Google Slides.
  - Thêm bộ template slide mới vào hệ thống qua Modal form.

### 10.2 Giới hạn kỹ thuật hiện tại:
- Trình soạn thảo văn bản hiện tại tập trung vào các định dạng chuẩn phổ biến nhất trong văn bản hành chính (headings, paragraph, font style, alignment, tables, bullet points). Các đối tượng nâng cao như biểu đồ động (dynamic charts) hay công thức toán học phức tạp (MathType) chưa được render tự động ra OpenXML DrawingML.
- Các PPTX trong repository là tài sản mẫu đơn giản, không phải bộ thiết kế chuyên nghiệp. Khi số slide thay đổi, package được dựng lại; animation, transition, media nhúng và hiệu ứng không được bảo toàn. Việc kiểm tra hiện tại dùng OpenXML validator, chưa kiểm tra hiển thị bằng ứng dụng Office.
- Ứng dụng không gọi dịch vụ AI và không tự sinh nội dung bằng AI; nội dung mẫu đến từ seed data, còn biên tập do người dùng thực hiện.
- Thumbnail/slide preview vẫn được sinh dưới `wwwroot/uploads/`; chưa có cơ chế dọn các file preview mồ côi. File Office được lưu tập trung trong MinIO và database giữ object key.
- HTTPS được cấu hình ở reverse proxy/deployment. Local run examples dùng HTTP; không triển khai public qua HTTP.
- Chưa có video demo trong repository; `DEMO_GUIDE.md` là hướng dẫn để tự quay và kiểm thử.

### 10.3 Hướng mở rộng trong tương lai:
1. **Tích hợp Office 365 WOPI / PowerPoint Online**: Cho phép nhúng trực tiếp Microsoft Office for the Web vào iframe để có trải nghiệm chỉnh sửa trực tuyến nâng cao của Microsoft.
2. **AI Document & Slide Generation**: Kết hợp Gemini API để tự động sinh toàn bộ nội dung tài liệu hoặc dàn bài slide dựa trên bản tóm tắt (prompt) của người dùng.
3. **Phân quyền và Phê duyệt luồng tài liệu**: Bổ sung quy trình phê duyệt (Approval Workflow: Trưởng phòng -> Ban giám đốc) trước khi chính thức ban hành văn bản.
