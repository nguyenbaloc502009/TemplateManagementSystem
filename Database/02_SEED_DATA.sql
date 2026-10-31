-- ========================================================
-- SEED DATA: Template Management System (PostgreSQL)
-- Dữ liệu mẫu khởi tạo cho 2 hệ thống
-- ========================================================

-- --------------------------------------------------------
-- 1. SEED DATA DỰ ÁN 1: THƯ VIỆN MẪU VĂN BẢN
-- --------------------------------------------------------

-- 1.1 Danh mục văn bản
INSERT INTO doc_categories (code, name, icon, description, display_order) VALUES
('HR', 'Nhân sự & Lao động', 'bi-people', 'Các mẫu biểu, hợp đồng, quyết định và đơn từ liên quan đến nhân sự', 1),
('ADMIN', 'Hành chính & Văn phòng', 'bi-building', 'Biên bản cuộc họp, thông báo, đề xuất nội bộ', 2),
('LEGAL', 'Pháp lý & Hợp đồng', 'bi-journal-bookmark', 'Các hợp đồng kinh tế, dịch vụ, thỏa thuận bảo mật', 3),
('SALES', 'Kinh doanh & Đối tác', 'bi-briefcase', 'Thư chào giá, thư mời hợp tác, bản đề xuất kinh doanh', 4),
('REPORT', 'Báo cáo & Kế hoạch', 'bi-graph-up-arrow', 'Báo cáo tuần/tháng, báo cáo dự án, kế hoạch tác nghiệp', 5);

-- 1.2 Mẫu văn bản (5 mẫu hoàn chỉnh)
INSERT INTO doc_templates (code, title, category_id, description, thumbnail_url, file_path, content_html, tags, usage_count) VALUES
(
    'DOC-001',
    'Đơn Xin Nghỉ Phép Tiêu Chuẩn',
    1,
    'Mẫu đơn xin nghỉ phép chuẩn áp dụng cho cán bộ nhân viên, bao gồm các lý do nghỉ phép, thời gian và người bàn giao công việc.',
    '/uploads/thumbnails/DOC-001.svg',
    'storage/templates/DOC-001.docx',
    '<div style="font-family: Arial, sans-serif; line-height: 1.6; color: #212529;">
        <div style="text-align: center; margin-bottom: 25px;">
            <p style="margin: 0; font-weight: bold; font-size: 14pt;">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</p>
            <p style="margin: 0; font-weight: bold; font-size: 13pt;">Độc lập - Tự do - Hạnh phúc</p>
            <p style="margin: 5px 0 0 0;">-----------------------</p>
        </div>
        <h2 style="text-align: center; font-size: 18pt; font-weight: bold; margin-bottom: 20px; color: #0d6efd;">ĐƠN XIN NGHỈ PHÉP</h2>
        <p style="margin-left: 50px;"><strong>Kính gửi:</strong></p>
        <ul style="list-style-type: disc; margin-left: 80px;">
            <li>Ban Giám đốc Công ty</li>
            <li>Trưởng phòng Nhân sự</li>
            <li>Trưởng bộ phận: <span style="background-color: #e9ecef; padding: 2px 8px; border-radius: 4px;">{{TenBoPhan}}</span></li>
        </ul>
        <p>Tôi tên là: <strong>{{HoTenNhanVien}}</strong> &nbsp;&nbsp;&nbsp;&nbsp; Mã nhân viên: <strong>{{MaNhanVien}}</strong></p>
        <p>Chức vụ hiện tại: <strong>{{ChucVu}}</strong> - Bộ phận: <strong>{{TenBoPhan}}</strong></p>
        <p>Tôi làm đơn này xin phép được nghỉ phép trong khoảng thời gian:</p>
        <p style="margin-left: 30px;">Từ ngày: <strong>{{TuNgay}}</strong> &nbsp;&nbsp; đến hết ngày: <strong>{{DenNgay}}</strong></p>
        <p style="margin-left: 30px;">Tổng số ngày xin nghỉ: <strong>{{TongSoNgay}}</strong> ngày.</p>
        <p><strong>Lý do xin nghỉ:</strong></p>
        <p style="background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 10px; margin: 10px 0;">{{LyDoNghiPhep}}</p>
        <p><strong>Bàn giao công việc:</strong></p>
        <p>Trong thời gian tôi nghỉ phép, toàn bộ công việc liên quan sẽ được bàn giao cho đồng nghiệp: <strong>{{NguoiNhanBanGiao}}</strong> - SĐT liên hệ: <strong>{{SdtNguoiBanGiao}}</strong>.</p>
        <p>Tôi cam kết sẽ giải quyết các công việc tồn đọng ngay sau khi trở lại làm việc.</p>
        <p style="margin-top: 30px; text-align: right;"><em>Hà Nội, ngày {{NgayLamDon}}</em></p>
        <table style="width: 100%; margin-top: 20px; text-align: center; border-collapse: collapse;">
            <tr>
                <td style="width: 33%; font-weight: bold;">NGƯỜI LÀM ĐƠN<br><br><br><br><em>(Ký và ghi rõ họ tên)</em></td>
                <td style="width: 33%; font-weight: bold;">TRƯỞNG BỘ PHẬN<br><br><br><br><em>(Ý kiến và chữ ký)</em></td>
                <td style="width: 34%; font-weight: bold;">BAN GIÁM ĐỐC DUYỆT<br><br><br><br><em>(Ký duyệt)</em></td>
            </tr>
        </table>
    </div>',
    'Nhân sự, Nghỉ phép, Đơn từ, Hành chính',
    18
),
(
    'DOC-002',
    'Hợp Đồng Lao Động Xác Định Thời Hạn',
    3,
    'Mẫu hợp đồng lao động chuẩn theo quy định của Bộ luật Lao động Việt Nam, bao gồm quyền lợi, nghĩa vụ, tiền lương và bảo hiểm.',
    '/uploads/thumbnails/DOC-002.svg',
    'storage/templates/DOC-002.docx',
    '<div style="font-family: Arial, sans-serif; line-height: 1.6; color: #212529;">
        <div style="text-align: center; margin-bottom: 20px;">
            <p style="margin: 0; font-weight: bold; font-size: 13pt;">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</p>
            <p style="margin: 0; font-weight: bold; font-size: 12pt;">Độc lập - Tự do - Hạnh phúc</p>
            <p style="margin: 3px 0 0 0;">-----------------------</p>
        </div>
        <h2 style="text-align: center; font-size: 16pt; font-weight: bold; margin-bottom: 5px; color: #0d6efd;">HỢP ĐỒNG LAO ĐỘNG</h2>
        <p style="text-align: center; font-size: 10pt; margin-top: 0;">Số: {{SoHopDong}}/HĐLĐ-2026</p>
        <p>Hôm nay, ngày {{NgayKy}}, tại trụ sở Công ty, chúng tôi gồm các bên:</p>
        <p><strong>BÊN SỬ DỤNG LAO ĐỘNG (BÊN A):</strong> <strong>{{TenCongTy}}</strong></p>
        <p>Đại diện bởi: Ông/Bà <strong>{{NguoiDaiDien}}</strong> - Chức vụ: <strong>{{ChucVuDaiDien}}</strong></p>
        <p>Địa chỉ trụ sở: {{DiaChiCongTy}} - Điện thoại: {{SdtCongTy}}</p>
        <hr style="border: 0; border-top: 1px dashed #ccc; margin: 15px 0;">
        <p><strong>BÊN NGƯỜI LAO ĐỘNG (BÊN B):</strong> Ông/Bà <strong>{{HoTenNguoiLaoDong}}</strong></p>
        <p>Sinh ngày: {{NgaySinh}} &nbsp;&nbsp; Số CCCD: {{SoCCCD}} &nbsp;&nbsp; Ngày cấp: {{NgayCapCCCD}}</p>
        <p>Địa chỉ thường trú: {{DiaChiThuongTru}}</p>
        <p>Hai bên cùng thỏa thuận ký kết hợp đồng lao động với các điều khoản sau:</p>
        <p><strong>Điều 1: Công việc và thời hạn hợp đồng</strong></p>
        <p>- Loại hợp đồng: Xác định thời hạn từ ngày <strong>{{NgayBatDau}}</strong> đến ngày <strong>{{NgayKetThuc}}</strong>.</p>
        <p>- Chức danh chuyên môn: <strong>{{ChucDanh}}</strong> tại Phòng/Ban: <strong>{{PhongBan}}</strong>.</p>
        <p><strong>Điều 2: Tiền lương và chế độ đãi ngộ</strong></p>
        <p>- Mức lương cơ bản: <strong>{{MucLuongCoBan}}</strong> VNĐ/tháng.</p>
        <p>- Phụ cấp công việc: <strong>{{PhuCap}}</strong> VNĐ/tháng.</p>
        <p>- Được đóng BHXH, BHYT, BHTN theo quy định pháp luật.</p>
        <p><strong>Điều 3: Cam kết thi hành</strong></p>
        <p>Hợp đồng được lập thành 02 bản có giá trị pháp lý như nhau, mỗi bên giữ 01 bản.</p>
        <table style="width: 100%; margin-top: 30px; text-align: center; border-collapse: collapse;">
            <tr>
                <td style="width: 50%; font-weight: bold;">ĐẠI DIỆN BÊN B<br><br><br><br><em>{{HoTenNguoiLaoDong}}</em></td>
                <td style="width: 50%; font-weight: bold;">ĐẠI DIỆN BÊN A<br><br><br><br><em>{{NguoiDaiDien}}</em></td>
            </tr>
        </table>
    </div>',
    'Hợp đồng, Lao động, Pháp lý, Nhân sự',
    35
),
(
    'DOC-003',
    'Biên Bản Cuộc Họp Nội Bộ / Dự Án',
    2,
    'Mẫu biên bản ghi lại nội dung, các quyết định chính và phân công nhiệm vụ (Action Items) trong cuộc họp ban điều hành hoặc nhóm dự án.',
    '/uploads/thumbnails/DOC-003.svg',
    'storage/templates/DOC-003.docx',
    '<div style="font-family: Arial, sans-serif; line-height: 1.6; color: #212529;">
        <h2 style="text-align: center; font-size: 16pt; font-weight: bold; margin-bottom: 5px; color: #198754;">BIÊN BẢN CUỘC HỌP NỘI BỘ</h2>
        <p style="text-align: center; font-size: 11pt; color: #6c757d; margin-top: 0;">Chủ đề: <strong>{{ChuDeCuocHop}}</strong></p>
        <table style="width: 100%; margin-bottom: 15px; border-collapse: collapse;">
            <tr>
                <td style="width: 50%;"><strong>Thời gian:</strong> {{ThoiGianHop}}</td>
                <td style="width: 50%;"><strong>Địa điểm:</strong> {{DiaDiemHop}}</td>
            </tr>
            <tr>
                <td><strong>Chủ trì cuộc họp:</strong> {{NguoiChuTri}}</td>
                <td><strong>Thư ký cuộc họp:</strong> {{ThuKy}}</td>
            </tr>
        </table>
        <p><strong>I. THÀNH PHẦN THAM DỰ</strong></p>
        <p>- Các thành viên có mặt: {{ThanhVienThamGia}}</p>
        <p>- Vắng mặt (có lý do/không lý do): {{ThanhVienVangMat}}</p>
        <p><strong>II. NỘI DUNG CUỘC HỌP</strong></p>
        <div style="background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 4px; padding: 12px; margin-bottom: 15px;">
            {{NoiDungThaoLuan}}
        </div>
        <p><strong>III. KẾT LUẬN VÀ PHÂN CÔNG NHIỆM VỤ (ACTION ITEMS)</strong></p>
        <table style="width: 100%; border: 1px solid #dee2e6; border-collapse: collapse; margin-bottom: 20px;">
            <thead>
                <tr style="background-color: #e9ecef; text-align: left;">
                    <th style="border: 1px solid #dee2e6; padding: 8px;">STT</th>
                    <th style="border: 1px solid #dee2e6; padding: 8px;">Nội dung công việc</th>
                    <th style="border: 1px solid #dee2e6; padding: 8px;">Người phụ trách</th>
                    <th style="border: 1px solid #dee2e6; padding: 8px;">Hạn hoàn thành (Deadline)</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td style="border: 1px solid #dee2e6; padding: 8px; text-align: center;">1</td>
                    <td style="border: 1px solid #dee2e6; padding: 8px;">Hoàn thiện báo cáo phân tích kiến trúc hệ thống</td>
                    <td style="border: 1px solid #dee2e6; padding: 8px;">Nguyễn Văn A</td>
                    <td style="border: 1px solid #dee2e6; padding: 8px;">05/11/2026</td>
                </tr>
                <tr>
                    <td style="border: 1px solid #dee2e6; padding: 8px; text-align: center;">2</td>
                    <td style="border: 1px solid #dee2e6; padding: 8px;">Kiểm thử tích hợp API và CSDL PostgreSQL</td>
                    <td style="border: 1px solid #dee2e6; padding: 8px;">Trần Thị B</td>
                    <td style="border: 1px solid #dee2e6; padding: 8px;">08/11/2026</td>
                </tr>
            </tbody>
        </table>
        <p>Cuộc họp kết thúc vào lúc {{GioKetThuc}} cùng ngày. Biên bản đã được thông qua các thành viên dự họp.</p>
        <table style="width: 100%; margin-top: 25px; text-align: center;">
            <tr>
                <td style="width: 50%; font-weight: bold;">THƯ KÝ<br><br><br><br><em>(Ký tên)</em></td>
                <td style="width: 50%; font-weight: bold;">CHỦ TRÌ CUỘC HỌP<br><br><br><br><em>(Ký tên)</em></td>
            </tr>
        </table>
    </div>',
    'Biên bản, Cuộc họp, Hành chính, Quản lý dự án',
    24
),
(
    'DOC-004',
    'Thư Mời Hợp Tác Kinh Doanh & Giới Thiệu Giải Pháp',
    4,
    'Thư ngỏ gửi đối tác doanh nghiệp nhằm giới thiệu giải pháp công nghệ, đề xuất cơ hội hợp tác và lịch hẹn làm việc.',
    '/uploads/thumbnails/DOC-004.svg',
    'storage/templates/DOC-004.docx',
    '<div style="font-family: Arial, sans-serif; line-height: 1.6; color: #212529;">
        <div style="border-bottom: 2px solid #0d6efd; padding-bottom: 10px; margin-bottom: 20px;">
            <h3 style="margin: 0; color: #0d6efd;">CÔNG TY CỔ PHẦN CÔNG NGHỆ VÀ DỊCH VỤ SỐ</h3>
            <p style="margin: 3px 0 0 0; font-size: 9pt; color: #6c757d;">Địa chỉ: Tòa nhà TechTower, Quận Cầu Giấy, Hà Nội | Hotline: 1900 6868</p>
        </div>
        <h2 style="text-align: center; font-size: 16pt; font-weight: bold; color: #0d6efd; margin-top: 15px;">THƯ MỜI HỢP TÁC KINH DOANH</h2>
        <p><strong>Kính gửi:</strong> Ban Lãnh đạo Quý Đối tác <strong>{{TenDoiTac}}</strong></p>
        <p>Lời đầu tiên, Ban Giám đốc cùng toàn thể đội ngũ chúng tôi xin gửi tới Quý Doanh nghiệp lời chào trân trọng và lời chúc phát triển thịnh vượng nhất!</p>
        <p>Hiện nay, với sự bùng nổ của chuyển đổi số và tự động hóa quy trình nghiệp vụ, chúng tôi tự hào cung cấp hệ thống <strong>{{TenGiaiPhap}}</strong> - giải pháp quản lý mẫu tài liệu và thuyết trình số 1 cho doanh nghiệp.</p>
        <p><strong>Những lợi thế vượt trội mà giải pháp mang lại cho Quý đơn vị:</strong></p>
        <ul style="line-height: 1.8;">
            <li>Tiết kiệm hơn <strong>70% thời gian</strong> soạn thảo văn bản và chuẩn bị slide thuyết trình.</li>
            <li>Kho mẫu chuẩn hóa, giao diện biên tập trực quan, đồng bộ nhận diện thương hiệu.</li>
            <li>Bảo mật cao cấp, hỗ trợ xuất file Word (DOCX) và PowerPoint (PPTX) bản địa.</li>
        </ul>
        <p>Chúng tôi rất mong muốn có cơ hội được gặp gỡ trực tiếp để trình bày giải pháp cụ thể vào thời gian:</p>
        <p style="background-color: #e7f1ff; padding: 10px; border-radius: 5px; margin: 15px 0;">
            <strong>Thời gian đề xuất:</strong> {{ThoiGianHen}} | <strong>Hình thức:</strong> {{HinhThucGap}}
        </p>
        <p>Rất mong sớm nhận được phản hồi tích cực từ Quý Công ty. Xin trân trọng cảm ơn!</p>
        <p style="text-align: right; margin-top: 30px;">
            <strong>ĐẠI DIỆN PHÁT TRIỂN KINH DOANH</strong><br>
            <em>{{NguoiDaiDien}}</em><br>
            Điện thoại: {{SdtLienHe}} | Email: {{EmailLienHe}}
        </p>
    </div>',
    'Kinh doanh, Thư mời, Đối tác, Hợp tác, Báo giá',
    12
),
(
    'DOC-005',
    'Báo Cáo Tiến Độ Dự Án Định Kỳ (Tuần / Tháng)',
    5,
    'Mẫu báo cáo tiến độ kỹ thuật dành cho Project Manager / Tech Lead tổng hợp khối lượng công việc, rủi ro và kế hoạch tiếp theo.',
    '/uploads/thumbnails/DOC-005.svg',
    'storage/templates/DOC-005.docx',
    '<div style="font-family: Arial, sans-serif; line-height: 1.6; color: #212529;">
        <h2 style="text-align: center; font-size: 16pt; font-weight: bold; color: #0d6efd; margin-bottom: 5px;">BÁO CÁO TIẾN ĐỘ DỰ ÁN</h2>
        <p style="text-align: center; color: #6c757d; margin-top: 0;">Kỳ báo cáo: <strong>{{KyBaoCao}}</strong> (Từ {{TuNgay}} đến {{DenNgay}})</p>
        <table style="width: 100%; border: 1px solid #ced4da; padding: 8px; margin-bottom: 15px; border-collapse: collapse;">
            <tr style="background-color: #f1f3f5;">
                <td style="padding: 8px; border: 1px solid #ced4da; width: 25%;"><strong>Tên dự án:</strong></td>
                <td style="padding: 8px; border: 1px solid #ced4da;">{{TenDuAn}}</td>
                <td style="padding: 8px; border: 1px solid #ced4da; width: 20%;"><strong>Mã dự án:</strong></td>
                <td style="padding: 8px; border: 1px solid #ced4da; width: 20%;">{{MaDuAn}}</td>
            </tr>
            <tr>
                <td style="padding: 8px; border: 1px solid #ced4da;"><strong>Project Manager:</strong></td>
                <td style="padding: 8px; border: 1px solid #ced4da;">{{ProjectManager}}</td>
                <td style="padding: 8px; border: 1px solid #ced4da;"><strong>Trạng thái chung:</strong></td>
                <td style="padding: 8px; border: 1px solid #ced4da; color: #198754; font-weight: bold;">{{TrangThaiTienDo}}</td>
            </tr>
        </table>
        <p><strong>1. CÁC ĐẦU VIỆC ĐÃ HOÀN THÀNH TRONG KỲ:</strong></p>
        <ul style="line-height: 1.7;">
            <li>Xây dựng xong toàn bộ Database Schema trên PostgreSQL 18 cho hệ thống mẫu tài liệu và slide.</li>
            <li>Hoàn thành phát triển REST API và service layer theo kiến trúc của hệ thống.</li>
            <li>Tích hợp bộ công cụ chuyển đổi xuất file định dạng Word DOCX chuẩn xác.</li>
        </ul>
        <p><strong>2. CÁC VẤN ĐỀ TỒN ĐỌNG &amp; RỦI RO (RISKS &amp; ISSUES):</strong></p>
        <div style="background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin-bottom: 15px;">
            {{RuiRoTonDong}}
        </div>
        <p><strong>3. KẾ HOẠCH HÀNH ĐỘNG CHO KỲ TIẾP THEO:</strong></p>
        <p>{{KeHoachKyToi}}</p>
        <table style="width: 100%; margin-top: 30px; text-align: center;">
            <tr>
                <td style="width: 50%; font-weight: bold;">NGƯỜI LẬP BÁO CÁO<br><br><br><br><em>{{NguoiLap}}</em></td>
                <td style="width: 50%; font-weight: bold;">PHÊ DUYỆT BỞI<br><br><br><br><em>{{NguoiDuyet}}</em></td>
            </tr>
        </table>
    </div>',
    'Báo cáo, Tiến độ, Kỹ thuật, Dự án, Sprint',
    41
);

-- --------------------------------------------------------
-- 2. SEED DATA DỰ ÁN 2: THƯ VIỆN MẪU TRÌNH CHIẾU
-- --------------------------------------------------------

-- 2.1 Danh mục mẫu trình chiếu
INSERT INTO presentation_categories (code, name, icon, description, display_order) VALUES
('STARTUP', 'Khởi nghiệp & Gọi vốn', 'bi-rocket-takeoff', 'Các mẫu Pitch Deck trình bày ý tưởng, mô hình kinh doanh và kêu gọi đầu tư', 1),
('PROJECT', 'Quản lý Dự án & Kỹ thuật', 'bi-kanban', 'Báo cáo Sprint review, lộ trình sản phẩm (Roadmap), kiến trúc hệ thống', 2),
('MARKETING', 'Tiếp thị & Chiến lược', 'bi-megaphone', 'Kế hoạch Marketing, phân tích thị trường, chiến dịch truyền thông', 3),
('FINANCE', 'Tài chính & Doanh nghiệp', 'bi-cash-coin', 'Báo cáo tài chính thường niên, tổng kết quý, họp cổ đông', 4);

-- 2.2 Bộ mẫu PowerPoint (3 bộ hoàn chỉnh)
INSERT INTO presentation_templates (code, title, category_id, description, thumbnail_url, file_path, total_slides, tags, usage_count) VALUES
(
    'PPT-001',
    'Startup Investment Pitch Deck 2026',
    1,
    'Bộ slide thuyết trình gọi vốn chuẩn mực cho các startup công nghệ, cấu trúc 5 slide bao gồm: Trang bìa, Vấn đề thị trường, Giải pháp sản phẩm, Mô hình kinh doanh và Kế hoạch tài chính.',
    '/uploads/thumbnails/PPT-001.svg',
    'storage/templates/PPT-001.pptx',
    5,
    'Startup, Pitch Deck, Đầu tư, Gọi vốn, Doanh nghiệp',
    56
),
(
    'PPT-002',
    'IT Project Sprint Review & Status Report',
    2,
    'Mẫu báo cáo tiến độ dự án công nghệ thông tin dành cho Scrum Master / Project Lead: Tổng kết Sprint, Chỉ số hoàn thành, Kiến trúc hệ thống và Kế hoạch phát hành tiếp theo.',
    '/uploads/thumbnails/PPT-002.svg',
    'storage/templates/PPT-002.pptx',
    4,
    'Công nghệ, Báo cáo dự án, Scrum, Sprint, Agile',
    38
),
(
    'PPT-003',
    'Digital Marketing Strategy & Growth Plan',
    3,
    'Kế hoạch chiến dịch Digital Marketing tổng thể: Phân tích đối tượng khách hàng mục tiêu, Kênh truyền thông đa kênh (Omnichannel), Ngân sách và Mục tiêu tăng trưởng ROI.',
    '/uploads/thumbnails/PPT-003.svg',
    'storage/templates/PPT-003.pptx',
    4,
    'Marketing, Chiến lược, Tăng trưởng, Social Media, ROI',
    29
);

-- 2.3 Các Slide xem trước của từng bộ template
-- Template 1: PPT-001 (5 slides)
INSERT INTO presentation_slides (template_id, slide_number, slide_title, subtitle, content_body, layout_type, notes, image_url) VALUES
(
    1, 1, 
    'NEXT-GEN AI CLOUD PLATFORM', 
    'Bản trình chiếu gọi vốn Vòng Hạt Giống (Seed Round)', 
    'Nền tảng Tự Động Hóa Soạn Thảo Văn Bản & Quản Lý Mẫu Doanh Nghiệp\nTrình bày bởi: Đội ngũ Sáng lập\nNgày: 30/10/2026', 
    'TitleSlide', 
    'Nhấn mạnh vào sứ mệnh số hóa và tiềm năng thị trường tỷ USD.', 
    '/images/slides/pitch_deck_s1.png'
),
(
    1, 2, 
    'VẤN ĐỀ VÀ THỜI ĐIỂM THỊ TRƯỜNG', 
    'Tại sao doanh nghiệp lãng phí hàng nghìn giờ mỗi năm?', 
    '• 72% nhân sự hành chính mất quá nhiều thời gian tìm kiếm và chỉnh sửa mẫu văn bản cũ.\n• Sai lệch định dạng khi tải và chuyển đổi giữa các phần mềm khác nhau.\n• Thiếu tính năng cộng tác và kiểm soát phiên bản mẫu tập trung.', 
    'TitleAndContent', 
    'Sử dụng số liệu khảo sát thực tế để tăng độ thuyết phục với các nhà đầu tư.', 
    '/images/slides/pitch_deck_s2.png'
),
(
    1, 3, 
    'GIẢI PHÁP ĐỘT PHÁ CỦA CHÚNG TÔI', 
    'Hệ sinh thái Quản lý Mẫu Toàn diện trên Nền tảng Đám mây', 
    '• Trình biên tập trực tuyến WYSIWYG tích hợp công nghệ phân tích giữ nguyên định dạng DOCX/PPTX.\n• Hệ thống phân loại thông minh, hỗ trợ tìm kiếm ngữ nghĩa theo danh mục và thẻ tag.\n• Tạo bản sao độc lập tức thì, bảo vệ tuyệt đối tính toàn vẹn của template gốc.', 
    'TitleAndContent', 
    'Trình diễn demo tính năng tạo tài liệu mới chỉ với 1 cú click.', 
    '/images/slides/pitch_deck_s3.png'
),
(
    1, 4, 
    'MÔ HÌNH DOANH THU & KINH DOANH', 
    'Chiến lược B2B SaaS với Tỷ lệ Giữ chân Khách hàng Cao', 
    '• Gói Starter: Miễn phí cho người dùng cá nhân (dưới 50 tài liệu/tháng).\n• Gói Business: 15 USD/user/tháng với dung lượng không giới hạn và hỗ trợ OpenXML API.\n• Gói Enterprise: Triển khai On-Premise kết hợp dịch vụ tùy biến chuyên biệt.', 
    'TwoColumn', 
    'Nhấn mạnh biên lợi nhuận gộp trên 80% của mô hình phần mềm SaaS.', 
    '/images/slides/pitch_deck_s4.png'
),
(
    1, 5, 
    'KÊU GỌI VỐN ĐẦU TƯ & MỤC TIÊU', 
    'Tìm kiếm khoản đầu tư 500,000 USD cho 10% Cổ phần', 
    '• 50%: Mở rộng đội ngũ R&D và tích hợp AI Generator.\n• 30%: Chiến dịch Marketing và thu hút 10,000 doanh nghiệp vừa và nhỏ.\n• 20%: Vận hành hạ tầng máy chủ và bảo mật dữ liệu.\nLiên hệ: contact@docmaster.vn', 
    'TitleAndContent', 
    'Kêu gọi hành động (Call To Action) và mở phiên hỏi đáp Q&A.', 
    '/images/slides/pitch_deck_s5.png'
);

-- Template 2: PPT-002 (4 slides)
INSERT INTO presentation_slides (template_id, slide_number, slide_title, subtitle, content_body, layout_type, notes, image_url) VALUES
(
    2, 1, 
    'BÁO CÁO TIẾN ĐỘ SPRINT REVIEW', 
    'Dự án Hệ thống Quản trị Mẫu Văn bản & Slide', 
    'Sprint: 14 | Kỳ đánh giá: Tuần 44/2026\nTech Lead: Nguyễn Bá Lộc\nTeam: Web Platform & Core Services', 
    'TitleSlide', 
    'Giới thiệu tổng quan mục tiêu của Sprint.', 
    '/images/slides/project_review_s1.png'
),
(
    2, 2, 
    'KẾT QUẢ CÔNG VIỆC TRONG SPRINT', 
    'Mức độ hoàn thành các Story Points cam kết', 
    '• [Hoàn thành 100%] Khởi tạo CSDL PostgreSQL và các Store SQL Parameterized.\n• [Hoàn thành 100%] Xây dựng RESTful API cho cả 2 sản phẩm văn bản và thuyết trình.\n• [Hoàn thành 100%] Trình soạn thảo văn bản và slide editor trực tuyến mượt mà.', 
    'TitleAndContent', 
    'Nhấn mạnh tiến độ vượt trước thời hạn cam kết ban đầu.', 
    '/images/slides/project_review_s2.png'
),
(
    2, 3, 
    'KIẾN TRÚC KỸ THUẬT & CÔNG NGHỆ', 
    'Mô hình 3 lớp phân tầng của hệ thống quản lý mẫu',
    '• Controller Layer: Kế thừa BaseApiController chuẩn hóa phản hồi JSON OkResponse.\n• Service & Data Layer: Độc lập logic, truy vấn SQL an toàn không SQL Injection.\n• Common Layer: PostgresDatabaseManager tối ưu hóa Connection Pool.', 
    'TwoColumn', 
    'Giải thích sơ đồ kiến trúc các tầng dữ liệu.', 
    '/images/slides/project_review_s3.png'
),
(
    2, 4, 
    'KẾ HOẠCH BÀN GIAO & PHÁT HÀNH', 
    'Kế hoạch kiểm thử tải và bàn giao tài liệu kỹ thuật', 
    '• Đóng gói source code đầy đủ theo đúng chuẩn quy định.\n• Viết README hướng dẫn cài đặt và video quay thao tác màn hình.\n• Sẵn sàng cho buổi phỏng vấn chuyên sâu về thiết kế và kiến trúc.', 
    'TitleAndContent', 
    'Tổng kết và đón nhận câu hỏi đánh giá từ ban giám khảo.', 
    '/images/slides/project_review_s4.png'
);

-- Template 3: PPT-003 (4 slides)
INSERT INTO presentation_slides (template_id, slide_number, slide_title, subtitle, content_body, layout_type, notes, image_url) VALUES
(
    3, 1, 
    'CHIẾN LƯỢC TIẾP THỊ SỐ 2026', 
    'Đột phá Tăng trưởng Khách hàng Đa kênh', 
    'Phòng Tiếp thị & Truyền thông Doanh nghiệp\nBáo cáo chiến lược quý 4', 
    'TitleSlide', 
    'Mở đầu buổi thuyết trình chiến lược tiếp thị.', 
    '/images/slides/marketing_s1.png'
),
(
    3, 2, 
    'CHÂN DUNG KHÁCH HÀNG MỤC TIÊU', 
    'Phân khúc Doanh nghiệp và Nhân sự Văn phòng', 
    '• Nhóm 1: Doanh nghiệp SME cần chuẩn hóa mẫu báo cáo, hợp đồng lao động.\n• Nhóm 2: Các chuyên viên tư vấn, Marketing và Quản lý dự án cần slide chuyên nghiệp nhanh chóng.\n• Nhóm 3: Các trường đại học, tổ chức giáo dục đào tạo giảng dạy.', 
    'TitleAndContent', 
    'Phân tích chi tiết nhu cầu và nỗi đau của từng phân khúc.', 
    '/images/slides/marketing_s2.png'
),
(
    3, 3, 
    'KÊNH PHÂN PHỐI & TRUYỀN THÔNG', 
    'Tối ưu hóa phễu chuyển đổi từ Inbound sang Khách hàng trả phí', 
    '• Content Marketing & SEO: Bài viết chia sẻ kho mẫu văn bản miễn phí dẫn về website.\n• Social Media & Video Shorts: Video hướng dẫn tạo nhanh slide báo cáo trong 2 phút.\n• Email Automation: Chăm sóc tự động các khách hàng dùng thử.', 
    'TwoColumn', 
    'Giải thích các chỉ số CAC (Chi phí sở hữu khách hàng) và LTV (Giá trị vòng đời).', 
    '/images/slides/marketing_s3.png'
),
(
    3, 4, 
    'MỤC TIÊU KPI & DỰ TOÁN NGÂN SÁCH', 
    'Chỉ tiêu tăng trưởng trong 6 tháng tiếp theo', 
    '• Đạt 50,000 lượt tải và tạo tài liệu thành công.\n• 1,200 khách hàng doanh nghiệp trả phí định kỳ.\n• Tỷ lệ hài lòng người dùng (CSAT) trên 95%.\n• Ngân sách đầu tư ước tính: 250,000,000 VNĐ.', 
    'TitleAndContent', 
    'Chốt lại mục tiêu cam kết và đề xuất phê duyệt ngân sách.', 
    '/images/slides/marketing_s4.png'
);
