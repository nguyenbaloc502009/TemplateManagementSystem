using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System.IO;
using System.Linq;
using System.Text;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PresentationTemplateSystem.Common
{
    /// <summary>
    /// Model dữ liệu cho mỗi slide khi xuất ra PPTX
    /// </summary>
    public class ExportSlideItem
    {
        public int SlideNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? ContentBody { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Tiện ích sinh và xuất file Microsoft PowerPoint (.pptx) chuẩn OpenXML
    /// Đảm bảo tương thích hoàn hảo với Microsoft Office, Google Slides, WPS Office.
    /// </summary>
    public static class PptxExportHelper
    {
        /// <summary>
        /// Tạo một file trình chiếu .pptx hoàn chỉnh từ danh sách các slide người dùng đã chỉnh sửa
        /// </summary>
        public static byte[] GeneratePptx(string deckTitle, List<ExportSlideItem> slides, string accentColorHex = "0D6EFD")
        {
            if (slides.Count == 0)
            {
                throw new ArgumentException("PPTX phải có ít nhất một slide.", nameof(slides));
            }

            using (MemoryStream memStream = new MemoryStream())
            {
                using (PresentationDocument pptDoc = PresentationDocument.Create(memStream, PresentationDocumentType.Presentation, true))
                {
                    // 1. Tạo PresentationPart
                    PresentationPart presentationPart = pptDoc.AddPresentationPart();
                    presentationPart.Presentation = new Presentation();

                    // Kích thước Slide 16:9 chuẩn (9144000 x 5143500 EMUs)
                    presentationPart.Presentation.SlideSize = new SlideSize() 
                    { 
                        Cx = 9144000, 
                        Cy = 5143500, 
                        Type = SlideSizeValues.Custom 
                    };
                    presentationPart.Presentation.NotesSize = new NotesSize() { Cx = 6858000, Cy = 9144000 };

                    SlideMasterIdList slideMasterIdList = new SlideMasterIdList();
                    SlideIdList slideIdList = new SlideIdList();
                    presentationPart.Presentation.InsertBefore(slideMasterIdList, presentationPart.Presentation.SlideSize);
                    presentationPart.Presentation.InsertBefore(slideIdList, presentationPart.Presentation.SlideSize);

                    SlideMasterPart slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
                    SlideLayoutPart slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
                    slideLayoutPart.SlideLayout = new SlideLayout(CreateEmptySlideCommonData(), new ColorMapOverride(new A.MasterColorMapping()))
                    {
                        Type = SlideLayoutValues.Blank,
                        Preserve = true
                    };
                    slideMasterPart.SlideMaster = new SlideMaster(
                        CreateEmptySlideCommonData(),
                        new ColorMap
                        {
                            Background1 = A.ColorSchemeIndexValues.Light1,
                            Text1 = A.ColorSchemeIndexValues.Dark1,
                            Background2 = A.ColorSchemeIndexValues.Light2,
                            Text2 = A.ColorSchemeIndexValues.Dark2,
                            Accent1 = A.ColorSchemeIndexValues.Accent1,
                            Accent2 = A.ColorSchemeIndexValues.Accent2,
                            Accent3 = A.ColorSchemeIndexValues.Accent3,
                            Accent4 = A.ColorSchemeIndexValues.Accent4,
                            Accent5 = A.ColorSchemeIndexValues.Accent5,
                            Accent6 = A.ColorSchemeIndexValues.Accent6,
                            Hyperlink = A.ColorSchemeIndexValues.Hyperlink,
                            FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink
                        },
                        new SlideLayoutIdList(new SlideLayoutId
                        {
                            Id = 2147483648U,
                            RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart)
                        }),
                        new TextStyles(new TitleStyle(), new BodyStyle(), new OtherStyle()));
                    SlideMasterId masterId = new SlideMasterId
                    {
                        Id = 2147483648U,
                        RelationshipId = presentationPart.GetIdOfPart(slideMasterPart)
                    };
                    slideMasterIdList.Append(masterId);
                    ThemePart themePart = slideMasterPart.AddNewPart<ThemePart>();
                    using (var themeStream = new MemoryStream(Encoding.UTF8.GetBytes(CreateThemeXml(accentColorHex))))
                    {
                        themePart.FeedData(themeStream);
                    }

                    // 3. Tạo từng Slide dựa trên danh sách slide của người dùng
                    uint currentSlideId = 256;
                    int slideIndex = 1;

                    foreach (var s in slides)
                    {
                        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();
                        Slide slide = new Slide(new CommonSlideData(new ShapeTree()));
                        ShapeTree shapeTree = slide.CommonSlideData?.ShapeTree
                            ?? throw new InvalidDataException("Không thể tạo cây hình cho slide.");

                        // Đặt NonVisualGroupShapeProperties
                        shapeTree.Append(new NonVisualGroupShapeProperties(
                            new NonVisualDrawingProperties() { Id = 1, Name = "" },
                            new NonVisualGroupShapeDrawingProperties(),
                            new ApplicationNonVisualDrawingProperties()));
                        shapeTree.Append(new GroupShapeProperties());

                        uint shapeId = 2;

                        // Tiêu đề Slide
                        if (!string.IsNullOrWhiteSpace(s.Title))
                        {
                            var titleShape = CreateTextShape(
                                shapeId: shapeId++,
                                shapeName: "Title",
                                text: s.Title,
                                x: 685800, // 0.75 inch
                                y: 457200, // 0.5 inch
                                width: 7772400, // 8.5 inch
                                height: 914400, // 1 inch
                                fontSize: 3200, // 32pt
                                isBold: true,
                                fontColorHex: accentColorHex
                            );
                            shapeTree.Append(titleShape);
                        }

                        // Phụ đề nếu có
                        if (!string.IsNullOrWhiteSpace(s.Subtitle))
                        {
                            var subShape = CreateTextShape(
                                shapeId: shapeId++,
                                shapeName: "Subtitle",
                                text: s.Subtitle,
                                x: 685800,
                                y: 1371600, // 1.5 inch
                                width: 7772400,
                                height: 457200, // 0.5 inch
                                fontSize: 1800, // 18pt
                                isBold: false,
                                fontColorHex: "6C757D" // Muted gray
                            );
                            shapeTree.Append(subShape);
                        }

                        // Nội dung chính Slide
                        if (!string.IsNullOrWhiteSpace(s.ContentBody))
                        {
                            long bodyY = string.IsNullOrWhiteSpace(s.Subtitle) ? 1463040 : 1920240;
                            var bodyShape = CreateMultilineTextShape(
                                shapeId: shapeId++,
                                shapeName: "Content",
                                rawText: s.ContentBody,
                                x: 685800,
                                y: bodyY,
                                width: 7772400,
                                height: 2743200, // 3 inch
                                fontSize: 1600, // 16pt
                                fontColorHex: "212529"
                            );
                            shapeTree.Append(bodyShape);
                        }

                        slidePart.Slide = slide;
                        slidePart.AddPart(slideLayoutPart);
                        slidePart.Slide.Save();

                        // Ghi chú của diễn giả (Speaker Notes): tạo NotesSlidePart gắn vào slide hiện tại
                        // để nội dung notes được bảo toàn thật sự trong file .pptx xuất ra (không chỉ hiển thị trên Editor)
                        if (!string.IsNullOrWhiteSpace(s.Notes))
                        {
                            AddNotesSlide(slidePart, s.Notes);
                        }

                        // Đăng ký Slide vào slideIdList
                        SlideId newSlideId = new SlideId()
                        {
                            Id = currentSlideId++,
                            RelationshipId = presentationPart.GetIdOfPart(slidePart)
                        };
                        slideIdList.Append(newSlideId);
                        slideIndex++;
                    }

                    presentationPart.Presentation.Save();
                }

                return memStream.ToArray();
            }
        }

        /// <summary>Tạo cấu trúc slide OpenXML tối thiểu dùng cho master và layout.</summary>
        private static CommonSlideData CreateEmptySlideCommonData()
        {
            return new CommonSlideData(
                new ShapeTree(
                    new NonVisualGroupShapeProperties(
                        new NonVisualDrawingProperties { Id = 1U, Name = "" },
                        new NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(new A.TransformGroup())));
        }

        /// <summary>Dựng XML theme PowerPoint với màu nhấn được chỉ định.</summary>
        private static string CreateThemeXml(string accentColorHex)
        {
            return $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Template Management Theme">
                  <a:themeElements>
                    <a:clrScheme name="Template Management">
                      <a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1>
                      <a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1>
                      <a:dk2><a:srgbClr val="25324A"/></a:dk2>
                      <a:lt2><a:srgbClr val="F7F9FC"/></a:lt2>
                      <a:accent1><a:srgbClr val="{accentColorHex}"/></a:accent1>
                      <a:accent2><a:srgbClr val="008C95"/></a:accent2>
                      <a:accent3><a:srgbClr val="70AD47"/></a:accent3>
                      <a:accent4><a:srgbClr val="FFC000"/></a:accent4>
                      <a:accent5><a:srgbClr val="4472C4"/></a:accent5>
                      <a:accent6><a:srgbClr val="ED7D31"/></a:accent6>
                      <a:hlink><a:srgbClr val="0563C1"/></a:hlink>
                      <a:folHlink><a:srgbClr val="954F72"/></a:folHlink>
                    </a:clrScheme>
                    <a:fontScheme name="Template Management">
                      <a:majorFont><a:latin typeface="Aptos Display"/><a:ea typeface=""/><a:cs typeface=""/></a:majorFont>
                      <a:minorFont><a:latin typeface="Aptos"/><a:ea typeface=""/><a:cs typeface=""/></a:minorFont>
                    </a:fontScheme>
                    <a:fmtScheme name="Template Management">
                      <a:fillStyleLst>
                        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                      </a:fillStyleLst>
                      <a:lnStyleLst>
                        <a:ln w="6350"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/></a:ln>
                        <a:ln w="12700"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/></a:ln>
                        <a:ln w="19050"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill><a:prstDash val="solid"/></a:ln>
                      </a:lnStyleLst>
                      <a:effectStyleLst>
                        <a:effectStyle><a:effectLst/></a:effectStyle>
                        <a:effectStyle><a:effectLst/></a:effectStyle>
                        <a:effectStyle><a:effectLst/></a:effectStyle>
                      </a:effectStyleLst>
                      <a:bgFillStyleLst>
                        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                      </a:bgFillStyleLst>
                    </a:fmtScheme>
                  </a:themeElements>
                  <a:objectDefaults/>
                  <a:extraClrSchemeLst/>
                </a:theme>
                """;
        }

        /// <summary>Đọc template PPTX từ đĩa rồi cập nhật nội dung slide và ghi chú.</summary>
        public static byte[] GeneratePptxFromTemplate(string templatePath, string deckTitle, List<ExportSlideItem> slides)
        {
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Không tìm thấy file template PowerPoint.", templatePath);
            }

            return GeneratePptxFromTemplate(File.ReadAllBytes(templatePath), deckTitle, slides);
        }

        /// <summary>Cập nhật nội dung trong PPTX hiện có và giữ nguyên cấu trúc, theme của template.</summary>
        public static byte[] GeneratePptxFromTemplate(byte[] templateBytes, string deckTitle, List<ExportSlideItem> slides)
        {
            ArgumentNullException.ThrowIfNull(templateBytes);
            using var stream = new MemoryStream();
            stream.Write(templateBytes, 0, templateBytes.Length);
            stream.Position = 0;
            using (PresentationDocument document = PresentationDocument.Open(stream, true))
            {
                document.PackageProperties.Title = deckTitle;
                PresentationPart presentationPart = document.PresentationPart
                    ?? throw new InvalidDataException("Template PPTX không có PresentationPart.");
                var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList()
                    ?? throw new InvalidDataException("Template PPTX không có slide nào.");
                var slideParts = slideIds.Select(id =>
                    presentationPart.GetPartById(id.RelationshipId?.Value
                        ?? throw new InvalidDataException("Template PPTX chứa slide thiếu quan hệ part.")) as SlidePart
                    ?? throw new InvalidDataException("Template PPTX chứa quan hệ slide không hợp lệ.")).ToList();

                // Nếu cấu trúc mẫu không khớp số slide cần xuất, dựng deck mới thay vì làm mất slide.
                if (slideParts.Count != slides.Count)
                {
                    string accent = slideParts.SelectMany(part => part.Slide.Descendants<P.Shape>())
                        .Where(shape => shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value == "Title")
                        .Select(shape => shape.Descendants<A.RgbColorModelHex>().FirstOrDefault()?.Val?.Value)
                        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "0D6EFD";
                    return GeneratePptx(deckTitle, slides, accent);
                }

                for (int i = 0; i < slides.Count; i++)
                {
                    SlidePart slidePart = slideParts[i];
                    ReplaceNamedShapeText(slidePart.Slide, "Title", slides[i].Title);
                    ReplaceNamedShapeText(slidePart.Slide, "Subtitle", slides[i].Subtitle ?? string.Empty);
                    ReplaceNamedShapeText(slidePart.Slide, "Content", slides[i].ContentBody ?? string.Empty);

                    NotesSlidePart? notesPart = slidePart.NotesSlidePart;
                    if (string.IsNullOrWhiteSpace(slides[i].Notes))
                    {
                        if (notesPart != null)
                        {
                            slidePart.DeletePart(notesPart);
                        }
                    }
                    else if (notesPart == null)
                    {
                        AddNotesSlide(slidePart, slides[i].Notes!);
                    }
                    else
                    {
                        ReplaceNamedShapeText(notesPart.NotesSlide, "Notes Placeholder", slides[i].Notes!);
                        notesPart.NotesSlide.Save();
                    }
                    slidePart.Slide.Save();
                }

                presentationPart.Presentation.Save();
            }

            return stream.ToArray();
        }

        /// <summary>Thay nội dung text của shape được định danh trong cây OpenXML.</summary>
        private static void ReplaceNamedShapeText(OpenXmlCompositeElement root, string shapeName, string value)
        {
            P.Shape? shape = root.Descendants<P.Shape>()
                .FirstOrDefault(candidate => candidate.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value == shapeName);
            if (shape?.TextBody == null)
            {
                return;
            }

            A.Paragraph? originalParagraph = shape.TextBody.Elements<A.Paragraph>().FirstOrDefault();
            A.ParagraphProperties? paragraphProperties = originalParagraph?.ParagraphProperties == null
                ? null
                : (A.ParagraphProperties)originalParagraph.ParagraphProperties.CloneNode(true);
            A.RunProperties? runProperties = originalParagraph?.Elements<A.Run>().FirstOrDefault()?.RunProperties == null
                ? null
                : (A.RunProperties)originalParagraph.Elements<A.Run>().First().RunProperties!.CloneNode(true);

            shape.TextBody.RemoveAllChildren<A.Paragraph>();
            string[] lines = value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                var paragraph = new A.Paragraph();
                if (paragraphProperties != null)
                {
                    paragraph.Append((A.ParagraphProperties)paragraphProperties.CloneNode(true));
                }
                var run = new A.Run();
                if (runProperties != null)
                {
                    run.Append((A.RunProperties)runProperties.CloneNode(true));
                }
                run.Append(new A.Text(line));
                paragraph.Append(run);
                shape.TextBody.Append(paragraph);
            }
        }

        /// <summary>
        /// Gắn một NotesSlidePart chứa nội dung ghi chú của diễn giả (Speaker Notes) vào SlidePart tương ứng,
        /// đảm bảo khi mở file .pptx bằng PowerPoint, phần ghi chú hiển thị trong khung Notes của từng slide.
        /// </summary>
        private static void AddNotesSlide(SlidePart slidePart, string notesText)
        {
            NotesSlidePart notesSlidePart = slidePart.AddNewPart<NotesSlidePart>();

            notesSlidePart.NotesSlide = new NotesSlide(
                new CommonSlideData(
                    new ShapeTree(
                        new NonVisualGroupShapeProperties(
                            new NonVisualDrawingProperties() { Id = 1, Name = "" },
                            new NonVisualGroupShapeDrawingProperties(),
                            new ApplicationNonVisualDrawingProperties()),
                        new GroupShapeProperties(new A.TransformGroup()),
                        new P.Shape(
                            new NonVisualShapeProperties(
                                new NonVisualDrawingProperties() { Id = 2, Name = "Notes Placeholder" },
                                new NonVisualShapeDrawingProperties(new A.ShapeLocks() { NoGrouping = true }),
                                new ApplicationNonVisualDrawingProperties(
                                    new PlaceholderShape() { Type = PlaceholderValues.Body, Index = 1 })),
                            new ShapeProperties(),
                            new TextBody(
                                new A.BodyProperties(),
                                new A.ListStyle(),
                                new A.Paragraph(
                                    new A.Run(
                                        new A.RunProperties() { Language = "vi-VN" },
                                        new A.Text(notesText))))
                        )
                    )
                ),
                new ColorMapOverride(new A.MasterColorMapping())
            );

            notesSlidePart.NotesSlide.Save();
        }

        /// <summary>Tạo shape văn bản đơn dòng với vị trí, kích thước và kiểu chữ chỉ định.</summary>
        private static P.Shape CreateTextShape(uint shapeId, string shapeName, string text, long x, long y, long width, long height, int fontSize, bool isBold, string fontColorHex)
        {
            var shape = new P.Shape();

            shape.NonVisualShapeProperties = new NonVisualShapeProperties(
                new NonVisualDrawingProperties() { Id = shapeId, Name = shapeName },
                new NonVisualShapeDrawingProperties(new A.ShapeLocks() { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties(new PlaceholderShape()));

            shape.ShapeProperties = new ShapeProperties(
                new A.Transform2D(
                    new A.Offset() { X = x, Y = y },
                    new A.Extents() { Cx = width, Cy = height }),
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle });

            var textBody = new TextBody(
                new A.BodyProperties(),
                new A.ListStyle(),
                new A.Paragraph(
                    new A.Run(
                        new A.RunProperties() 
                        { 
                            Language = "vi-VN", 
                            FontSize = fontSize, 
                            Bold = isBold 
                        },
                        new A.Text(text))));

            if (!string.IsNullOrEmpty(fontColorHex))
            {
                var runProps = textBody.Descendants<A.RunProperties>().FirstOrDefault();
                if (runProps != null)
                {
                    runProps.Append(new A.SolidFill(new A.RgbColorModelHex() { Val = fontColorHex }));
                }
            }

            shape.TextBody = textBody;
            return shape;
        }

        /// <summary>Tạo shape văn bản nhiều dòng, giữ từng dòng trong paragraph riêng.</summary>
        private static P.Shape CreateMultilineTextShape(uint shapeId, string shapeName, string rawText, long x, long y, long width, long height, int fontSize, string fontColorHex)
        {
            var shape = new P.Shape();

            shape.NonVisualShapeProperties = new NonVisualShapeProperties(
                new NonVisualDrawingProperties() { Id = shapeId, Name = shapeName },
                new NonVisualShapeDrawingProperties(new A.ShapeLocks() { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties(new PlaceholderShape()));

            shape.ShapeProperties = new ShapeProperties(
                new A.Transform2D(
                    new A.Offset() { X = x, Y = y },
                    new A.Extents() { Cx = width, Cy = height }),
                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle });

            var textBody = new TextBody(
                new A.BodyProperties(),
                new A.ListStyle());

            string[] lines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var para = new A.Paragraph();
                var run = new A.Run();
                var rPr = new A.RunProperties() { Language = "vi-VN", FontSize = fontSize };
                if (!string.IsNullOrEmpty(fontColorHex))
                {
                    rPr.Append(new A.SolidFill(new A.RgbColorModelHex() { Val = fontColorHex }));
                }
                run.Append(rPr);
                run.Append(new A.Text(line));
                para.Append(run);
                textBody.Append(para);
            }

            shape.TextBody = textBody;
            return shape;
        }
    }
}
