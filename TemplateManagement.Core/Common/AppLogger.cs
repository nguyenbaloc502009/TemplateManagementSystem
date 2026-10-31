namespace TemplateManagement.Core.Common
{
    public sealed class AppLogger
    {
        private readonly string _contextName;

        private AppLogger(Type contextType)
        {
            _contextName = contextType.Name;
        }

        /// <summary>Tạo logger gắn với tên của kiểu đang ghi log.</summary>
        public static AppLogger GetLogger(Type type) => new(type);

        /// <summary>Ghi thông điệp thông tin ở cấp Info.</summary>
        public void Info(string message) =>
            Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{_contextName}] {message}");

        /// <summary>Ghi thông điệp cảnh báo ở cấp Warn.</summary>
        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{_contextName}] {message}");
            Console.ResetColor();
        }

        /// <summary>Ghi thông điệp lỗi và thông tin ngoại lệ nếu có.</summary>
        public void Error(string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{_contextName}] {message} - {ex?.Message}");
            if (ex?.StackTrace != null)
            {
                Console.WriteLine(ex.StackTrace);
            }
            Console.ResetColor();
        }
    }
}
