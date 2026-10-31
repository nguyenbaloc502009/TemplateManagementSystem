using DocumentTemplateSystem.Common;
using NUnit.Framework;
using TemplateManagement.Core.Security;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

namespace TemplateSystem.Tests
{
    [TestFixture]
    public class DocxExportHelperTests
    {
        [Test]
        /// <summary>Kiểm tra sanitizer bỏ script và URL nguy hiểm nhưng giữ định dạng hợp lệ.</summary>
        public void HtmlContentSanitizer_ShouldRemoveScriptAndUnsafeUrlsButKeepDocumentFormatting()
        {
            string safeHtml = HtmlContentSanitizer.Sanitize(
                "<p style=\"text-align:center\"><strong>Title</strong><script>alert(1)</script><a href=\"javascript:alert(1)\">link</a></p>");

            Assert.That(safeHtml, Does.Contain("<strong>Title</strong>"));
            Assert.That(safeHtml, Does.Not.Contain("<script"));
            Assert.That(safeHtml, Does.Not.Contain("javascript:"));
        }

        /// <summary>Kiểm tra file DOCX mẫu có cấu trúc OpenXML hợp lệ.</summary>
        public void GeneratedDocumentTemplate_ShouldBeValidOpenXml()
        {
            byte[] contents = DocxExportHelper.GenerateDocx("Template test", "<p>Template content</p>");
            using var stream = new MemoryStream(contents);
            using var document = WordprocessingDocument.Open(stream, false);
            var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(error => $"{error.Description} {error.Path?.XPath}")));
        }

        [Test]
        /// <summary>Kiểm tra bảng HTML thành bảng Word thật và không lộ thuộc tính HTML thô.</summary>
        public void GenerateDocx_WithHtmlTable_ShouldRenderRealTableWithoutLeakingRawTagText()
        {
            // Arrange: HTML có bảng chữ ký giống hệt cấu trúc dùng trong các mẫu văn bản thật (DOC-001..DOC-005)
            string html = @"
                <p>Nội dung trước bảng</p>
                <table style='width:100%; border-collapse: collapse;'>
                    <tr>
                        <td style='width:33%; font-weight:bold;'>NGƯỜI LÀM ĐƠN<br><em>(Ký và ghi rõ họ tên)</em></td>
                        <td style='width:33%; font-weight:bold;'>TRƯỞNG BỘ PHẬN</td>
                        <td style='width:34%; font-weight:bold;'>BAN GIÁM ĐỐC DUYỆT</td>
                    </tr>
                </table>";

            // Act
            byte[] bytes = DocxExportHelper.GenerateDocx("Kiểm thử bảng biểu", html);

            using var memStream = new MemoryStream(bytes);
            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);
            using (var validationStream = new MemoryStream(bytes))
            using (var document = WordprocessingDocument.Open(validationStream, false))
            {
                var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
                Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(error => $"{error.Description} {error.Path?.XPath}")));
            }
            var docEntry = archive.Entries.First(e => e.FullName == "word/document.xml");
            using var reader = new StreamReader(docEntry.Open(), Encoding.UTF8);
            string xmlContent = reader.ReadToEnd();

            // Assert 1: Phải dựng được bảng Word thật (thẻ <w:tbl>), không chỉ là đoạn văn bản dồn chung
            Assert.That(xmlContent, Does.Contain("<w:tbl>"), "Nội dung <table> phải được dựng thành bảng Word (w:tbl) thật sự");

            // Assert 2: Nội dung từng ô phải còn nguyên vẹn
            Assert.That(xmlContent, Does.Contain("NGƯỜI LÀM ĐƠN"));
            Assert.That(xmlContent, Does.Contain("TRƯỞNG BỘ PHẬN"));
            Assert.That(xmlContent, Does.Contain("BAN GIÁM ĐỐC DUYỆT"));

            // Assert 3 (quan trọng nhất): KHÔNG được rò rỉ thuộc tính HTML thô (vd: 'td style=') ra thành text hiển thị
            Assert.That(xmlContent, Does.Not.Contain("td style="), "Thuộc tính của thẻ <td> không được rò rỉ thành văn bản hiển thị trong Word");
            Assert.That(xmlContent, Does.Not.Contain("font-weight:bold;\">"), "Chuỗi thuộc tính HTML thô không được xuất hiện trong nội dung Word");
        }

        [Test]
        /// <summary>Kiểm tra HTML hợp lệ được xuất thành file DOCX OpenXML đọc được.</summary>
        public void GenerateDocx_WithValidHtml_ShouldProduceValidDocxBytes()
        {
            // Arrange
            string title = "Đơn Xin Nghỉ Phép Kiểm Thử";
            string htmlContent = @"
                <h2>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</h2>
                <p><strong>Kính gửi:</strong> Ban Giám Đốc</p>
                <p>Tôi tên là: <em>Nguyễn Văn Test</em></p>
                <hr/>
                <p>Lý do nghỉ phép: Nghỉ việc gia đình.</p>
            ";

            // Act
            byte[] bytes = DocxExportHelper.GenerateDocx(title, htmlContent);

            // Assert
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes.Length, Is.GreaterThan(1000), "File DOCX sinh ra phải có kích thước hợp lệ của định dạng OpenXML");
            
            // Header PK signature của định dạng ZIP/DOCX chuẩn: 0x50, 0x4B (ASCII 'PK')
            Assert.That(bytes[0], Is.EqualTo(0x50));
            Assert.That(bytes[1], Is.EqualTo(0x4B));
            using var stream = new MemoryStream(bytes);
            using var document = WordprocessingDocument.Open(stream, false);
            var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(error => $"{error.Description} {error.Path?.XPath}")));
        }

        [Test]
        /// <summary>Kiểm tra nội dung HTML rỗng vẫn tạo được file DOCX hợp lệ.</summary>
        public void GenerateDocx_WithEmptyHtml_ShouldNotThrowException()
        {
            // Act
            byte[] bytes = DocxExportHelper.GenerateDocx("Tiêu đề rỗng", "");

            // Assert
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes.Length, Is.GreaterThan(500));
        }
    }
}
