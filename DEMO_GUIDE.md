# KỊCH BẢN KIỂM THỬ THỰC TẾ & HƯỚNG DẪN QUAY VIDEO DEMO
## HỆ THỐNG THƯ VIỆN MẪU VĂN BẢN & MẪU TRÌNH CHIẾU

Tài liệu này hướng dẫn chi tiết các bước thao tác thực tế trên giao diện để kiểm tra toàn bộ yêu cầu của đề bài tuyển dụng hoặc thực hiện video quay màn hình (screen recording).

---

### PHẦN 1: KIỂM THỬ SẢN PHẨM 1 - THƯ VIỆN MẪU VĂN BẢN (`http://localhost:5001`)

#### Bước 1.1: Trải nghiệm Giao diện Thư viện Mẫu Văn bản
1. Mở trình duyệt truy cập: `http://localhost:5001`.
2. Quan sát danh sách mẫu hiển thị dưới dạng **Lưới (Card Grid)**:
   - Mỗi card có đầy đủ: Tên mẫu, Badge danh mục, Biểu tượng văn bản, Mô tả ngắn, Thẻ tag, Số lượt dùng.
   - Thử bấm nút chuyển đổi sang **Dạng Danh Sách (Table View)** trên thanh công cụ để xem cách hiển thị dạng bảng trực quan.
3. **Thử nghiệm Tìm kiếm & Lọc**:
   - Nhập từ khóa: `"Nghỉ phép"` vào ô tìm kiếm -> Bấm **Lọc** -> Kết quả hiển thị đúng mẫu *Đơn Xin Nghỉ Phép Tiêu Chuẩn*.
   - Nhấp vào các huy hiệu danh mục nhanh (Pills): *Hợp đồng*, *Hành chính*, *Kinh doanh* -> Danh sách tự động lọc theo danh mục đã chọn.

#### Bước 1.2: Xem trước Mẫu Văn bản (Preview Modal)
1. Trên card của mẫu bất kỳ (ví dụ: *Đơn Xin Nghỉ Phép*), bấm nút **"Xem trước mẫu"**.
2. Cửa sổ modal hiển thị nội dung tài liệu mẫu đầy đủ trên khung giấy A4 chuẩn mực.
3. Người dùng có thể đọc toàn bộ điều khoản, các biến placeholder (`{{HoTen}}`, `{{TuNgay}}`).

#### Bước 1.3: Khởi tạo & Chỉnh sửa Tài liệu (Tích hợp Trình Soạn Thảo WYSIWYG)
1. Trong modal xem trước hoặc trực tiếp trên card, bấm nút **"Sử dụng mẫu này"**.
2. Hệ thống tự động tạo một bản ghi tài liệu mới độc lập (mẫu gốc được giữ nguyên) và chuyển thẳng vào màn hình **Trình Soạn Thảo Văn Bản (Editor)**:
   - Nhấp vào tiêu đề tài liệu để đổi tên: ví dụ `"Đơn Xin Nghỉ Phép - Nguyễn Văn A"`.
   - Trải nghiệm thanh công cụ soạn thảo:
     + Bôi đen một đoạn văn bản và bấm **In Đậm (B)**, **In Nghiêng (I)**, **Gạch Chân (U)**.
     + Thay đổi căn lề: Căn giữa, Căn đều hai bên.
     + Bấm nút **"Điền Biến Nhanh"** -> Chọn **"Tự động điền ngày hôm nay"** để thay thế biến ngày tháng bằng ngày thực tế.
     + Bấm **"Chèn Bảng"** để bổ sung bảng dữ liệu vào tài liệu.
3. Bấm nút **"Lưu Tài Liệu"**:
   - Hệ thống gọi API lưu trực tiếp vào CSDL PostgreSQL.
   - Huy hiệu trạng thái chuyển sang màu xanh: *"Đã lưu (HH:mm:ss)"*.

#### Bước 1.4: Tải xuống Microsoft Word (.docx) & In ấn
1. Bấm nút **"Tải Word (.docx)"**:
   - Trình duyệt tự động tải xuống file `.docx` thật được tạo bởi thư viện `DocumentFormat.OpenXml`.
   - Nếu có Microsoft Word hoặc LibreOffice, mở file để kiểm tra hiển thị. Automated tests kiểm tra cấu trúc OpenXML; việc hiển thị bằng ứng dụng Office chưa được xác nhận trong repo này.
2. Bấm nút **"In / Xuất PDF"**: Trình duyệt mở hộp thoại Print chuẩn A4 để xuất file PDF hoặc in trực tiếp ra máy in.

#### Bước 1.5: Quản lý Kho Tài Liệu Cá Nhân
1. Bấm vào menu **"Tài Liệu Của Tôi"** trên thanh Navbar:
   - Xem danh sách các tài liệu đã tạo từ mẫu.
   - Bấm **"Sửa"** để mở lại trình soạn thảo tiếp tục chỉnh sửa.
   - Bấm nút **"DOCX"** để tải lại file.

#### Bước 1.6: Bổ sung Mẫu Mới vào Thư viện
1. Bấm nút **"Thêm Mẫu Mới"** màu vàng trên Navbar.
2. Điền thông tin: Mã: `DOC-006`, Tên: `Biên Bản Bàn Giao Tài Sản`, chọn Danh mục và nhập nội dung mẫu.
3. Bấm **"Lưu Mẫu Mới"** -> Mẫu mới xuất hiện ngay lập tức trong thư viện mà **hoàn toàn không cần sửa lại code giao diện**!

---

### PHẦN 2: KIỂM THỬ SẢN PHẨM 2 - THƯ VIỆN MẪU TRÌNH CHIẾU (`http://localhost:5002`)

#### Bước 2.1: Trải nghiệm Thư viện Mẫu PowerPoint
1. Mở trình duyệt truy cập: `http://localhost:5002` (hoặc bấm nút "Sang Thư Viện Slide (SP 2)" trên navbar của Sản phẩm 1).
2. Quan sát giao diện Dark theme sang trọng phong cách trình chiếu chuyên nghiệp.
3. Thử nghiệm lọc theo các chủ đề: *Khởi nghiệp & Gọi vốn*, *Quản lý Dự án & Kỹ thuật*, *Tiếp thị & Chiến lược*.

#### Bước 2.2: Xem trước Bộ Slide (Slide Gallery Carousel)
1. Trên card bộ mẫu *Startup Investment Pitch Deck 2026*, bấm nút **"Xem trước bộ slide (5 trang)"**.
2. Cửa sổ Modal hiển thị Slide Carousel:
   - Bấm các nút mũi tên Tiếp/Trước (hoặc click vào dải thumbnail trang bên dưới) để lướt xem từng slide:
     + Slide 1: Trang bìa gọi vốn
     + Slide 2: Vấn đề thị trường
     + Slide 3: Giải pháp sản phẩm
     + Slide 4: Mô hình doanh thu
     + Slide 5: Kêu gọi vốn và liên hệ
   - Đọc các phần ghi chú của diễn giả (Speaker Notes) bên dưới slide.

#### Bước 2.3: Khởi tạo & Chỉnh sửa Bộ Trình Chiếu Mới
1. Bấm nút **"Tạo bản trình chiếu mới từ bộ này"**.
2. Hệ thống nhân bản bộ slide thành bản trình chiếu độc lập (mẫu gốc giữ nguyên) và chuyển vào **Slide Editor trực quan**:
   - Sidebar bên trái hiển thị danh sách tất cả các slide. Bấm vào từng slide để chuyển canvas.
   - Thử nghiệm sửa:
     + Tiêu đề slide (Slide Title)
     + Phụ đề (Subtitle)
     + Nội dung các bullet points
     + Ghi chú diễn giả (Speaker Notes)
   - Bấm nút **"Thêm"** trên sidebar để bổ sung thêm slide mới vào bộ trình chiếu.
   - Bấm biểu tượng thùng rác màu đỏ để xóa bớt slide nếu muốn.
   - Bấm nút **"Trình Chiếu"** để xem thử ở chế độ Full Screen.
3. Bấm nút **"Lưu Slide"**: Bản trình chiếu được lưu vào CSDL PostgreSQL.

#### Bước 2.4: Tải xuống file Microsoft PowerPoint (.pptx)
1. Bấm nút **"Tải PowerPoint (.pptx)"**:
   - Trình duyệt tải xuống file `.pptx` thật được tạo bởi OpenXML.
   - Nếu có Microsoft PowerPoint hoặc LibreOffice, mở file để kiểm tra hiển thị. Package được xác thực tự động bằng OpenXmlValidator nhưng chưa được mở thủ công trong ứng dụng Office. Khi giữ nguyên số slide, export dựa trên package template; nếu thêm/xóa slide, package được tái tạo theo màu chủ đạo. Animation/transition không được bảo toàn.

> Hướng dẫn này là kịch bản tự quay video; repository không kèm video demo. Chạy ứng dụng qua HTTP chỉ dùng local development. Khi triển khai, cấu hình HTTPS tại reverse proxy/host và dùng secrets ngoài source control.

#### Bước 2.5: Quản lý Bản Trình Chiếu Cá Nhân
1. Bấm vào menu **"Bài Thuyết Trình Của Tôi"** trên Navbar:
   - Hiển thị danh sách các bản trình chiếu người dùng đã tạo.
   - Có thể mở lại để biên tập tiếp hoặc tải lại file PPTX bất kỳ lúc nào.
