using PresentationTemplateSystem.Common;
using NUnit.Framework;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml;

namespace TemplateSystem.Tests
{
    [TestFixture]
    public class PptxExportHelperTests
    {
        /// <summary>Kiểm tra PPTX có master, layout, theme, ghi chú và OpenXML hợp lệ.</summary>
        public void GeneratedPresentationTemplate_ShouldHaveValidMasterLayoutThemeAndNotes()
        {
            byte[] contents = PptxExportHelper.GeneratePptx("Template test", new List<ExportSlideItem>
            {
                new() { SlideNumber = 1, Title = "Template slide", Notes = "Template notes" }
            });
            using var stream = new MemoryStream(contents);
            using var document = PresentationDocument.Open(stream, false);
            var presentationPart = document.PresentationPart!;
            Assert.That(presentationPart.SlideMasterParts, Is.Not.Empty);
            Assert.That(presentationPart.SlideMasterParts.First().SlideLayoutParts, Is.Not.Empty);
            Assert.That(presentationPart.SlideMasterParts.First().ThemePart, Is.Not.Null);
            Assert.That(presentationPart.SlideParts.Any(slide => slide.NotesSlidePart != null), Is.True);
            var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(error => $"{error.Description} {error.Path?.XPath}")));
        }

        [Test]
        /// <summary>Kiểm tra ghi chú diễn giả được nhúng vào notes slide của file PPTX.</summary>
        public void GeneratePptx_WithSpeakerNotes_ShouldEmbedNotesSlideInFile()
        {
            // Arrange: slide có ghi chú diễn giả (Speaker Notes)
            string notesText = "Nhấn mạnh tiềm năng thị trường tỷ USD khi thuyết trình.";
            var slides = new List<ExportSlideItem>
            {
                new ExportSlideItem
                {
                    SlideNumber = 1,
                    Title = "TRANG BÌA",
                    Notes = notesText
                }
            };

            // Act
            byte[] bytes = PptxExportHelper.GeneratePptx("Deck Kiểm Thử Notes", slides);

            // Assert: file .pptx (ZIP) phải chứa entry ppt/notesSlides/notesSlide1.xml và nội dung ghi chú thật sự có trong đó
            using var memStream = new MemoryStream(bytes);
            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);
            using (var validationStream = new MemoryStream(bytes))
            using (var document = PresentationDocument.Open(validationStream, false))
            {
                var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
                Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(error => $"{error.Description} {error.Path?.XPath}")));
            }

            var notesEntry = archive.Entries.FirstOrDefault(e => e.FullName.Contains("notesSlides/notesSlide"));
            Assert.That(notesEntry, Is.Not.Null, "File PPTX xuất ra phải chứa notesSlide khi slide có Speaker Notes");

            using var reader = new StreamReader(notesEntry!.Open(), Encoding.UTF8);
            string xmlContent = reader.ReadToEnd();
            Assert.That(xmlContent, Does.Contain(notesText), "Nội dung Speaker Notes phải được ghi thật vào file PPTX, không chỉ hiển thị trên Editor");
        }

        [Test]
        /// <summary>Kiểm tra có thể xuất nhiều slide thành gói PPTX có chữ ký ZIP hợp lệ.</summary>
        public void GeneratePptx_WithMultipleSlides_ShouldProduceValidPptxBytes()
        {
            // Arrange
            string title = "Bộ Slide Thuyết Trình Gọi Vốn";
            var slides = new List<ExportSlideItem>
            {
                new ExportSlideItem
                {
                    SlideNumber = 1,
                    Title = "STARTUP PITCH DECK 2026",
                    Subtitle = "Vòng Hạt Giống (Seed Round)",
                    ContentBody = "Đội ngũ sáng lập ABC"
                },
                new ExportSlideItem
                {
                    SlideNumber = 2,
                    Title = "VẤN ĐỀ THỊ TRƯỜNG",
                    Subtitle = "Tại sao doanh nghiệp cần giải pháp?",
                    ContentBody = "• 70% doanh nghiệp mất thời gian định dạng lại tài liệu\n• Thiếu kho mẫu chuẩn"
                }
            };

            // Act
            byte[] bytes = PptxExportHelper.GeneratePptx(title, slides);

            // Assert
            Assert.That(bytes, Is.Not.Null);
            Assert.That(bytes.Length, Is.GreaterThan(1000), "File PPTX sinh ra phải có kích thước hợp lệ của định dạng OpenXML Presentation");

            // Header PK signature của ZIP/PPTX chuẩn: 0x50, 0x4B
            Assert.That(bytes[0], Is.EqualTo(0x50));
            Assert.That(bytes[1], Is.EqualTo(0x4B));
        }

        [Test]
        /// <summary>Kiểm tra master, layout và theme được đăng ký đúng trong presentation.</summary>
        public void GeneratePptx_ShouldHaveValidRegisteredMasterLayoutAndTheme()
        {
            byte[] bytes = PptxExportHelper.GeneratePptx("Valid package", new List<ExportSlideItem>
            {
                new ExportSlideItem { SlideNumber = 1, Title = "Title", ContentBody = "Body" }
            });

            using var stream = new MemoryStream(bytes);
            using var document = PresentationDocument.Open(stream, false);
            Assert.That(document.PresentationPart!.SlideMasterParts, Is.Not.Empty);
            Assert.That(document.PresentationPart.SlideMasterParts.First().SlideLayoutParts, Is.Not.Empty);
            Assert.That(document.PresentationPart.SlideMasterParts.First().ThemePart, Is.Not.Null);
            var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
            Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(error => $"{error.Description} {error.Path?.XPath}")));
        }

        [Test]
        /// <summary>Kiểm tra cập nhật nội dung mà vẫn giữ theme và kiểu dáng của template.</summary>
        public void GeneratePptxFromTemplate_ShouldKeepTemplateStyleAndReplaceSlideText()
        {
            string path = Path.Combine(Path.GetTempPath(), $"template-{Guid.NewGuid():N}.pptx");
            try
            {
                byte[] template = PptxExportHelper.GeneratePptx("Template", new List<ExportSlideItem>
                {
                    new ExportSlideItem { SlideNumber = 1, Title = "Original title", Subtitle = "Original subtitle", ContentBody = "Original body", Notes = "Original notes" }
                }, "7030A0");
                File.WriteAllBytes(path, template);

                byte[] exported = PptxExportHelper.GeneratePptxFromTemplate(path, "User deck", new List<ExportSlideItem>
                {
                    new ExportSlideItem { SlideNumber = 1, Title = "Edited title", Subtitle = "Edited subtitle", ContentBody = "Edited body", Notes = "Edited notes" }
                });

                using var stream = new MemoryStream(exported);
                using var document = PresentationDocument.Open(stream, false);
                var slide = document.PresentationPart!.SlideParts.Single().Slide;
                string text = string.Join(" ", slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(item => item.Text));
                Assert.That(text, Does.Contain("Edited title"));
                Assert.That(text, Does.Contain("Edited body"));
                Assert.That(text, Does.Not.Contain("Original title"));
                Assert.That(slide.Descendants<DocumentFormat.OpenXml.Drawing.RgbColorModelHex>().Any(color => color.Val == "7030A0"), Is.True);
                string notes = string.Join(" ", document.PresentationPart.SlideParts.Single().NotesSlidePart!
                    .NotesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>().Select(item => item.Text));
                Assert.That(notes, Does.Contain("Edited notes"));
                Assert.That(notes, Does.Not.Contain("Original notes"));
                var errors = new OpenXmlValidator(FileFormatVersions.Office2019).Validate(document).ToList();
                Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors.Select(error => $"{error.Description} {error.Path?.XPath}")));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
