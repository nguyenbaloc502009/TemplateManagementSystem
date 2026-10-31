using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

namespace DocumentTemplateSystem.Common
{
    /// <summary>
    /// Tiện ích chuyển đổi và xuất định dạng tài liệu Microsoft Word (.docx) chuẩn OpenXML
    /// Giữ nguyên định dạng cơ bản của văn bản: Tiêu đề, In đậm, In nghiêng, Căn lề, Đoạn văn, Bảng biểu.
    /// </summary>
    public static class DocxExportHelper
    {
        /// <summary>
        /// Các thẻ inline được công nhận và giữ định dạng khi phân tích nội dung bên trong một khối (block).
        /// Bất kỳ thẻ nào khác (vd: &lt;td&gt;, &lt;table&gt;, &lt;ul&gt; còn sót lại...) sẽ bị loại bỏ hoàn toàn
        /// ở bước tiền xử lý để tránh rò rỉ chuỗi thuộc tính HTML thành văn bản hiển thị (vd: 'td style="...">').
        /// </summary>
        private static readonly Regex UnknownTagRegex = new Regex(
            @"</?(?!strong\b|b\b|em\b|i\b|u\b|span\b|br\b)[a-zA-Z][a-zA-Z0-9]*(?:\s[^>]*?)?/?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Tạo một file .docx hoàn chỉnh từ nội dung HTML/văn bản của người dùng
        /// </summary>
        /// <param name="documentTitle">Tiêu đề tài liệu</param>
        /// <param name="htmlContent">Nội dung HTML được soạn thảo trong trình biên tập</param>
        /// <returns>Mảng byte của file docx</returns>
        public static byte[] GenerateDocx(string documentTitle, string htmlContent)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                // Khởi tạo WordprocessingDocument trong bộ nhớ
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(memStream, WordprocessingDocumentType.Document, true))
                {
                    // Tạo MainDocumentPart
                    MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = new Body();

                    // Tiêu đề tài liệu
                    if (!string.IsNullOrWhiteSpace(documentTitle))
                    {
                        Paragraph titlePara = new Paragraph();
                        ParagraphProperties titleProps = new ParagraphProperties(
                            new SpacingBetweenLines() { After = "240" },
                            new Justification() { Val = JustificationValues.Center }
                        );
                        titlePara.Append(titleProps);

                        Run titleRun = new Run();
                        RunProperties titleRunProps = new RunProperties(
                            new Bold(),
                            new Color() { Val = "0D6EFD" }, // Primary blue
                            new FontSize() { Val = "32" } // 16pt
                        );
                        titleRun.Append(titleRunProps);
                        titleRun.Append(new Text(documentTitle));
                        titlePara.Append(titleRun);
                        body.Append(titlePara);
                    }

                    // Phân tích và render nội dung HTML thành các đoạn Word OpenXML
                    ParseHtmlToOpenXml(body, htmlContent);

                    mainPart.Document.Append(body);
                    mainPart.Document.Save();
                }

                return memStream.ToArray();
            }
        }

        /// <summary>
        /// Phân tích nội dung HTML thành các đoạn Paragraph, Table và Run trong Wordprocessing OpenXML
        /// </summary>
        private static void ParseHtmlToOpenXml(Body body, string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                body.Append(new Paragraph(new Run(new Text("(Nội dung trống)"))));
                return;
            }

            // Chuẩn hóa ngắt dòng và thẻ html
            string cleanHtml = html.Replace("\r", "").Replace("&nbsp;", " ");

            // Tách các đoạn thẻ khối: bảng (table) được nhận diện RIÊNG để dựng thành bảng Word thật,
            // các khối còn lại (h1-h6, p, li, div) được xử lý như đoạn văn bản thông thường.
            var blockRegex = new Regex(
                @"(<table[^>]*>.*?</table>|<(h[1-6]|p|li|div)[^>]*>.*?</\2>|<hr\s*/?>)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var matches = blockRegex.Matches(cleanHtml);

            if (matches.Count == 0)
            {
                // Nếu là văn bản thuần không có thẻ khối
                string[] lines = cleanHtml.Split(new[] { "<br>", "<br/>", "<br />", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    Paragraph p = new Paragraph();
                    AppendInlineContent(p, line, false, "28");
                    body.Append(p);
                }
                return;
            }

            foreach (Match match in matches)
            {
                string blockHtml = match.Value;

                if (Regex.IsMatch(blockHtml, @"^<table", RegexOptions.IgnoreCase))
                {
                    // Dựng bảng Word OpenXML thật từ thẻ <table>/<tr>/<td>/<th>
                    Table table = BuildOpenXmlTable(blockHtml);
                    body.Append(table);
                    // Thêm đoạn trống sau bảng để tránh dính liền nội dung kế tiếp
                    body.Append(new Paragraph());
                    continue;
                }

                if (Regex.IsMatch(blockHtml, @"^<hr", RegexOptions.IgnoreCase))
                {
                    // Thêm đường kẻ ngang
                    Paragraph hrPara = new Paragraph();
                    ParagraphProperties pPr = new ParagraphProperties(
                        new ParagraphBorders(new BottomBorder() { Val = BorderValues.Single, Size = 6, Space = 1, Color = "CCCCCC" })
                    );
                    hrPara.Append(pPr);
                    body.Append(hrPara);
                    continue;
                }

                Paragraph para = new Paragraph();
                ParagraphProperties paraProps = new ParagraphProperties();
                paraProps.Append(new SpacingBetweenLines() { After = "120", Line = "280", LineRule = LineSpacingRuleValues.Auto });

                // Xác định căn lề (align)
                if (blockHtml.Contains("text-align: center") || blockHtml.Contains("text-align:center") || blockHtml.Contains("align=\"center\""))
                {
                    paraProps.Append(new Justification() { Val = JustificationValues.Center });
                }
                else if (blockHtml.Contains("text-align: right") || blockHtml.Contains("text-align:right") || blockHtml.Contains("align=\"right\""))
                {
                    paraProps.Append(new Justification() { Val = JustificationValues.Right });
                }
                else if (blockHtml.Contains("text-align: justify") || blockHtml.Contains("text-align:justify"))
                {
                    paraProps.Append(new Justification() { Val = JustificationValues.Both });
                }

                para.Append(paraProps);

                // Kiểm tra loại thẻ
                bool isHeading = Regex.IsMatch(blockHtml, @"^<h[1-6]", RegexOptions.IgnoreCase);
                string headingSize = "28"; // 14pt
                if (blockHtml.StartsWith("<h1", StringComparison.OrdinalIgnoreCase)) headingSize = "36";
                else if (blockHtml.StartsWith("<h2", StringComparison.OrdinalIgnoreCase)) headingSize = "30";
                else if (blockHtml.StartsWith("<h3", StringComparison.OrdinalIgnoreCase)) headingSize = "26";

                // Trích xuất phần nội dung bên trong thẻ bao ngoài cùng (vd: bỏ <p ...> và </p>)
                string innerContent = ExtractInnerContent(blockHtml);

                // Phân tích các Run định dạng bên trong đoạn (bold, italic, underline, text)
                AppendInlineContent(para, innerContent, isHeading, headingSize);

                body.Append(para);
            }
        }

        /// <summary>
        /// Dựng một bảng Word (Table/TableRow/TableCell) OpenXML thật từ chuỗi HTML &lt;table&gt;...&lt;/table&gt;
        /// Mỗi hàng &lt;tr&gt; trở thành một TableRow, mỗi &lt;td&gt;/&lt;th&gt; trở thành một TableCell,
        /// đảm bảo cấu trúc lưới (grid) được giữ nguyên thay vì bị dồn thành một chuỗi văn bản liền mạch.
        /// </summary>
        private static Table BuildOpenXmlTable(string tableHtml)
        {
            Table table = new Table();

            TableProperties tblProps = new TableProperties(
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableBorders(
                    new TopBorder() { Val = BorderValues.Single, Size = 6, Color = "999999" },
                    new StartBorder() { Val = BorderValues.Single, Size = 6, Color = "999999" },
                    new BottomBorder() { Val = BorderValues.Single, Size = 6, Color = "999999" },
                    new EndBorder() { Val = BorderValues.Single, Size = 6, Color = "999999" },
                    new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 6, Color = "999999" },
                    new InsideVerticalBorder() { Val = BorderValues.Single, Size = 6, Color = "999999" }
                )
            );
            table.AppendChild(tblProps);

            var rowRegex = new Regex(@"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var cellRegex = new Regex(@"<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            MatchCollection rowMatches = rowRegex.Matches(tableHtml);
            int columnCount = rowMatches.Cast<Match>()
                .Select(match => cellRegex.Matches(match.Groups[1].Value).Count)
                .DefaultIfEmpty(1)
                .Max();
            var tableGrid = new TableGrid();
            for (int i = 0; i < columnCount; i++)
            {
                tableGrid.Append(new GridColumn());
            }
            table.AppendChild(tableGrid);

            bool hasAnyRow = false;

            foreach (Match rowMatch in rowMatches)
            {
                var cellMatches = cellRegex.Matches(rowMatch.Groups[1].Value);
                if (cellMatches.Count == 0)
                {
                    continue; // Bỏ qua hàng không có ô dữ liệu (vd: hàng header rỗng do thead/tbody)
                }

                TableRow row = new TableRow();
                bool isHeaderRow = Regex.IsMatch(rowMatch.Groups[1].Value, @"<th[\s>]", RegexOptions.IgnoreCase);

                foreach (Match cellMatch in cellMatches)
                {
                    bool isHeaderCell = cellMatch.Value.StartsWith("<th", StringComparison.OrdinalIgnoreCase);
                    string cellInnerRaw = cellMatch.Groups[1].Value;

                    TableCell cell = new TableCell();
                    cell.Append(new TableCellProperties(
                        new TableCellWidth() { Type = TableWidthUnitValues.Auto },
                        new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }
                    ));

                    Paragraph cellPara = new Paragraph();
                    ParagraphProperties cellParaProps = new ParagraphProperties(
                        new Justification() { Val = JustificationValues.Center }
                    );
                    cellPara.Append(cellParaProps);

                    AppendInlineContent(cellPara, cellInnerRaw, isHeaderCell || isHeaderRow, "22");

                    // Word yêu cầu mỗi ô có ít nhất một Run (kể cả ô rỗng) để render đúng
                    if (!cellPara.Elements<Run>().Any())
                    {
                        cellPara.Append(new Run(new Text(string.Empty)));
                    }

                    cell.Append(cellPara);
                    row.Append(cell);
                }

                table.Append(row);
                hasAnyRow = true;
            }

            if (!hasAnyRow)
            {
                // Bảng không phân tích được cấu trúc hàng/ô hợp lệ -> trả về bảng 1 ô chứa text thuần (an toàn, không mất dữ liệu)
                TableRow fallbackRow = new TableRow();
                TableCell fallbackCell = new TableCell();
                Paragraph fallbackPara = new Paragraph();
                AppendInlineContent(fallbackPara, ExtractInnerContent(tableHtml), false, "22");
                if (!fallbackPara.Elements<Run>().Any())
                {
                    fallbackPara.Append(new Run(new Text(string.Empty)));
                }
                fallbackCell.Append(fallbackPara);
                fallbackRow.Append(fallbackCell);
                table.Append(fallbackRow);
            }

            return table;
        }

        /// <summary>
        /// Trích xuất phần nội dung bên trong thẻ bao ngoài cùng (bỏ đi thẻ mở đầu và thẻ đóng cuối cùng)
        /// </summary>
        private static string ExtractInnerContent(string blockHtml)
        {
            int firstClose = blockHtml.IndexOf('>');
            int lastOpen = blockHtml.LastIndexOf('<');
            return (firstClose >= 0 && lastOpen > firstClose)
                ? blockHtml.Substring(firstClose + 1, lastOpen - firstClose - 1)
                : blockHtml;
        }

        /// <summary>
        /// Phân tích và nối các Run định dạng (in đậm, in nghiêng, gạch chân, ngắt dòng, màu sắc) vào một Paragraph
        /// từ một đoạn nội dung HTML "bên trong" (đã bỏ thẻ bao ngoài).
        /// QUAN TRỌNG: Mọi thẻ HTML không nằm trong danh sách được công nhận (strong/b/em/i/u/span/br) đều bị loại bỏ
        /// HOÀN TOÀN trước khi tách token, để tránh việc chuỗi thuộc tính của thẻ lạ (vd: &lt;td style="..."&gt;)
        /// bị lọt vào làm văn bản hiển thị trong file Word.
        /// </summary>
        private static void AppendInlineContent(Paragraph para, string innerContent, bool isHeading, string headingSize)
        {
            if (string.IsNullOrEmpty(innerContent)) return;

            // Loại bỏ mọi thẻ không được công nhận (an toàn với mọi thẻ lạ: table, td, tr, ul, font, img, ...)
            string sanitized = UnknownTagRegex.Replace(innerContent, " ");

            // Xử lý các đoạn ngắt dòng bên trong khối
            string[] segments = sanitized.Split(new[] { "<br>", "<br/>", "<br />" }, StringSplitOptions.None);

            for (int s = 0; s < segments.Length; s++)
            {
                string seg = segments[s];
                if (s > 0)
                {
                    // Thêm ngắt dòng
                    para.Append(new Run(new Break()));
                }

                // Xử lý các thẻ con <strong>, <em>, <u>, <span> còn được giữ lại
                var tokenRegex = new Regex(@"(<(strong|b|em|i|u|span)[^>]*>.*?</\2>|[^<]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                var tokenMatches = tokenRegex.Matches(seg);

                if (tokenMatches.Count == 0)
                {
                    string plain = StripHtmlTags(seg).Trim();
                    if (!string.IsNullOrEmpty(plain))
                    {
                        Run r = new Run(new Text(plain) { Space = SpaceProcessingModeValues.Preserve });
                        if (isHeading)
                        {
                            r.RunProperties = new RunProperties(new Bold(), new FontSize() { Val = headingSize });
                        }
                        para.Append(r);
                    }
                    continue;
                }

                foreach (Match token in tokenMatches)
                {
                    string tokenText = token.Value;
                    if (string.IsNullOrWhiteSpace(tokenText)) continue;

                    bool isBold = isHeading || Regex.IsMatch(tokenText, @"<(strong|b)[^>]*>", RegexOptions.IgnoreCase);
                    bool isItalic = Regex.IsMatch(tokenText, @"<(em|i)[^>]*>", RegexOptions.IgnoreCase);
                    bool isUnderline = Regex.IsMatch(tokenText, @"<u[^>]*>", RegexOptions.IgnoreCase);

                    string textVal = StripHtmlTags(tokenText);
                    if (string.IsNullOrEmpty(textVal)) continue;

                    Run run = new Run();
                    RunProperties rPr = new RunProperties();

                    if (isBold) rPr.Append(new Bold());
                    if (isItalic) rPr.Append(new Italic());
                    if (isUnderline) rPr.Append(new Underline() { Val = UnderlineValues.Single });
                    // Màu sắc tùy chỉnh nếu có
                    if (tokenText.Contains("#0d6efd") || isHeading)
                    {
                        rPr.Append(new Color() { Val = "0D6EFD" });
                    }
                    else if (tokenText.Contains("#198754"))
                    {
                        rPr.Append(new Color() { Val = "198754" });
                    }
                    if (isHeading) rPr.Append(new FontSize() { Val = headingSize });

                    run.Append(rPr);
                    run.Append(new Text(textVal) { Space = SpaceProcessingModeValues.Preserve });
                    para.Append(run);
                }
            }
        }

        /// <summary>
        /// Loại bỏ thẻ HTML lấy nội dung text thuần túy
        /// </summary>
        private static string StripHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, "<.*?>", string.Empty)
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&amp;", "&")
                .Replace("&quot;", "\"");
        }
    }
}
