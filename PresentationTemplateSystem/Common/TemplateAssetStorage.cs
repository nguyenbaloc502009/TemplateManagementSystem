using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace PresentationTemplateSystem.Common
{
    public static class TemplateAssetStorage
    {
        private static string _webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        private static string _thumbnailFolder = "uploads/thumbnails";
        private static string _slidePreviewFolder = "uploads/slide-previews";

        /// <summary>Cấu hình thư mục gốc, thumbnail và ảnh xem trước slide trong web root.</summary>
        public static void Initialize(string webRoot, string? thumbnailFolder)
        {
            _webRoot = Path.GetFullPath(webRoot);
            _thumbnailFolder = NormalizeFolder(thumbnailFolder, "uploads/thumbnails");
        }

        /// <summary>Tạo khóa lưu trữ cho file PPTX mẫu theo mã bộ slide.</summary>
        public static string TemplateUrl(string code) => $"storage/templates/{SafeName(code)}.pptx";
        /// <summary>Tạo khóa lưu trữ riêng cho file PPTX của người dùng.</summary>
        public static string DeckUrl(Guid id) => $"storage/user/{id:N}.pptx";
        /// <summary>Tạo URL công khai của thumbnail theo mã bộ slide.</summary>
        public static string ThumbnailUrl(string code) => PublicUrl(_thumbnailFolder, SafeName(code) + ".svg");
        /// <summary>Tạo URL công khai cho ảnh xem trước theo số thứ tự slide.</summary>
        public static string SlidePreviewUrl(string code, int slideNumber) =>
            PublicUrl(_slidePreviewFolder, $"{SafeName(code)}-{slideNumber}.svg");
        /// <summary>Kiểm tra khóa có khớp vị trí lưu trữ chuẩn của file mẫu PPTX hay không.</summary>
        public static bool IsTemplateStorageKey(string storageKey, string code) =>
            string.Equals(storageKey, TemplateUrl(code), StringComparison.Ordinal);

        /// <summary>Kiểm tra khóa có chứa GUID hợp lệ theo định dạng file deck người dùng hay không.</summary>
        public static bool IsDeckStorageKey(string storageKey) =>
            storageKey.StartsWith("storage/user/", StringComparison.OrdinalIgnoreCase)
            && storageKey.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParseExact(storageKey["storage/user/".Length..^".pptx".Length], "N", out _);

        /// <summary>Tạo thumbnail SVG cho bộ slide nếu ảnh chưa tồn tại.</summary>
        public static void EnsureThumbnail(string code, string title, string color)
        {
            string path = WebPath(_thumbnailFolder, SafeName(code) + ".svg");
            if (!File.Exists(path))
            {
                WriteSvgThumbnail(path, title, color);
            }
        }

        /// <summary>Đảm bảo thumbnail dự phòng tồn tại và trả về URL của ảnh đó.</summary>
        public static string EnsureFallbackThumbnail()
        {
            const string fallbackCode = "presentation-thumbnail-fallback";
            EnsureThumbnail(fallbackCode, "Presentation preview unavailable", "6C757D");
            return ThumbnailUrl(fallbackCode);
        }

        /// <summary>Tạo ảnh xem trước SVG an toàn cho một slide nếu file chưa tồn tại.</summary>
        public static string EnsureSlidePreview(string code, int number, string title, string? subtitle, string? body, string color)
        {
            string url = SlidePreviewUrl(code, number);
            string path = WebPath(_slidePreviewFolder, $"{SafeName(code)}-{number}.svg");
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                string safeTitle = SecurityElement.Escape(title) ?? string.Empty;
                string safeSubtitle = SecurityElement.Escape(subtitle ?? string.Empty) ?? string.Empty;
                string safeBody = SecurityElement.Escape(body ?? string.Empty) ?? string.Empty;
                string bodyLines = string.Join("", safeBody.Split('\n').Take(4).Select((line, index) =>
                    $"<text x=\"56\" y=\"{210 + index * 30}\" font-family=\"Arial,sans-serif\" font-size=\"18\" fill=\"#344054\">{line}</text>"));
                string svg = $"""
                    <svg xmlns="http://www.w3.org/2000/svg" width="960" height="540" viewBox="0 0 960 540">
                      <rect width="960" height="540" fill="#fff"/>
                      <rect width="960" height="16" fill="#{color}"/>
                      <text x="56" y="100" font-family="Arial,sans-serif" font-size="34" font-weight="700" fill="#{color}">{safeTitle}</text>
                      <text x="56" y="150" font-family="Arial,sans-serif" font-size="22" fill="#667085">{safeSubtitle}</text>
                      {bodyLines}
                    </svg>
                    """;
                File.WriteAllText(path, svg, Encoding.UTF8);
            }
            return url;
        }

        /// <summary>Dựng thumbnail SVG từ tiêu đề đã escape và ghi xuống đĩa.</summary>
        private static void WriteSvgThumbnail(string path, string title, string color)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string safeTitle = SecurityElement.Escape(title) ?? string.Empty;
            string svg = $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="640" height="360" viewBox="0 0 640 360">
                  <rect width="640" height="360" rx="18" fill="#f7f9fc"/>
                  <rect width="640" height="18" rx="9" fill="#{color}"/>
                  <rect x="44" y="58" width="552" height="244" rx="10" fill="#fff" stroke="#dce3ed" stroke-width="2"/>
                  <rect x="82" y="94" width="476" height="12" rx="6" fill="#{color}" opacity=".18"/>
                  <text x="82" y="154" font-family="Arial,sans-serif" font-size="26" font-weight="700" fill="#25324a">{safeTitle}</text>
                  <rect x="82" y="188" width="410" height="9" rx="4" fill="#dce3ed"/>
                  <rect x="82" y="212" width="350" height="9" rx="4" fill="#e8edf4"/>
                  <rect x="82" y="236" width="390" height="9" rx="4" fill="#e8edf4"/>
                </svg>
                """;
            File.WriteAllText(path, svg, Encoding.UTF8);
        }

        /// <summary>Đổi đường dẫn web tương đối thành đường dẫn file đã kiểm tra an toàn.</summary>
        private static string WebPath(string folder, string fileName) => SafeCombine(_webRoot, folder, fileName);

        /// <summary>Ghép đường dẫn và từ chối mọi kết quả thoát khỏi thư mục gốc.</summary>
        private static string SafeCombine(string root, string folder, string fileName)
        {
            string normalizedRoot = Path.GetFullPath(root);
            string path = Path.GetFullPath(Path.Combine(normalizedRoot, folder, fileName));
            string relativePath = Path.GetRelativePath(normalizedRoot, path);
            if (Path.IsPathRooted(relativePath)
                || relativePath == ".."
                || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Asset path must stay inside its configured root.");
            }
            return path;
        }

        /// <summary>Chuẩn hóa dấu phân cách và tạo URL công khai từ thư mục cùng tên file.</summary>
        private static string PublicUrl(string folder, string fileName) => "/" + folder.Replace('\\', '/') + "/" + fileName;

        /// <summary>Chuẩn hóa thư mục asset tương đối và loại bỏ đường dẫn thoát root.</summary>
        private static string NormalizeFolder(string? folder, string fallback)
        {
            string value = string.IsNullOrWhiteSpace(folder) ? fallback : folder.Trim();
            value = value.Replace('\\', '/').Trim('/');
            if (Path.IsPathRooted(value) || value.Split('/').Any(part => part == ".."))
            {
                throw new InvalidOperationException("Thumbnail folders must be relative paths inside wwwroot.");
            }
            return value;
        }

        /// <summary>Chuẩn hóa mã thành tên file an toàn và yêu cầu có ít nhất một ký tự hợp lệ.</summary>
        private static string SafeName(string value)
        {
            string safe = Regex.Replace(value, @"[^a-zA-Z0-9_-]", "_");
            if (string.IsNullOrWhiteSpace(safe))
            {
                throw new ArgumentException("Asset code must contain at least one valid character.", nameof(value));
            }
            return safe;
        }
    }
}
