using Ganss.Xss;

namespace TemplateManagement.Core.Security
{
    public static class HtmlContentSanitizer
    {
        private static readonly string[] OfficeContentTags =
        [
            "table", "thead", "tbody", "tfoot", "tr", "th", "td", "h1", "h2", "h3", "h4", "h5", "h6",
            "p", "div", "span", "strong", "b", "em", "i", "u", "ul", "ol", "li", "br", "hr"
        ];

        private static readonly string[] OfficeCssProperties =
        [
            "background-color", "border", "border-collapse", "border-left", "color", "font-family",
            "font-size", "font-weight", "line-height", "margin", "margin-bottom", "margin-left",
            "margin-top", "padding", "padding-left", "text-align", "text-indent", "width"
        ];

        /// <summary>Lọc HTML đầu vào, loại bỏ nội dung và thuộc tính không an toàn.</summary>
        public static string Sanitize(string? html)
        {
            var sanitizer = new HtmlSanitizer();
            foreach (string tag in OfficeContentTags)
            {
                sanitizer.AllowedTags.Add(tag);
            }
            sanitizer.AllowedAttributes.Add("style");
            foreach (string property in OfficeCssProperties)
            {
                sanitizer.AllowedCssProperties.Add(property);
            }
            return sanitizer.Sanitize(html ?? string.Empty);
        }
    }
}
