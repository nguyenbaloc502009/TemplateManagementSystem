using Npgsql;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace TemplateManagement.Core.Data
{
    public sealed class DatabaseManager : IDisposable
    {
        private static readonly NpgsqlFactory Factory = NpgsqlFactory.Instance;
        private DbTransaction? _transaction;
        private bool _disposed;

        public DbConnection Connection { get; }

        public DatabaseManager(IConfiguration configuration, bool openConnection = true)
        {
            Connection = CreateConnection(configuration, openConnection);
        }

        /// <summary>Tạo kết nối PostgreSQL từ cấu hình và tùy chọn mở kết nối ngay.</summary>
        public static DbConnection CreateConnection(IConfiguration configuration, bool openConnection = true)
        {
            string connectionString = configuration.GetConnectionString("PostgresDb")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgresDb is not configured.");
            DbConnection connection = Factory.CreateConnection()
                ?? throw new InvalidOperationException("Npgsql could not create a database connection.");
            connection.ConnectionString = connectionString;
            if (openConnection)
            {
                connection.Open();
            }
            return connection;
        }

        /// <summary>Mở kết nối nếu cần và bắt đầu giao dịch dùng chung cho các lệnh tiếp theo.</summary>
        public DbTransaction BeginTransaction()
        {
            if (_transaction == null)
            {
                if (Connection.State != ConnectionState.Open)
                {
                    Connection.Open();
                }
                _transaction = Connection.BeginTransaction();
            }
            return _transaction;
        }

        /// <summary>Ghi nhận giao dịch hiện tại và xóa trạng thái giao dịch.</summary>
        public void Commit()
        {
            _transaction?.Commit();
            _transaction = null;
        }

        /// <summary>Hoàn tác giao dịch hiện tại và xóa trạng thái giao dịch.</summary>
        public void Rollback()
        {
            _transaction?.Rollback();
            _transaction = null;
        }

        /// <summary>Tạo lệnh SQL, gắn giao dịch hiện tại và thêm tham số có tên.</summary>
        private DbCommand CreateCommand(string sql, IDictionary? parameters = null)
        {
            DbCommand command = Connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _transaction;
            if (parameters != null)
            {
                foreach (DictionaryEntry entry in parameters)
                {
                    DbParameter parameter = command.CreateParameter();
                    string name = entry.Key?.ToString() ?? string.Empty;
                    // Giữ tham số ở dạng bind thay vì ghép giá trị trực tiếp vào SQL.
                    parameter.ParameterName = name.StartsWith('@') ? name : "@" + name;
                    parameter.Value = entry.Value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }
            }
            return command;
        }

        /// <summary>Thực thi truy vấn có tham số và trả về kết quả dạng bảng dữ liệu.</summary>
        public DataTable QueryData(string sql, IDictionary? parameters = null)
        {
            var result = new DataTable();
            using DbCommand command = CreateCommand(sql, parameters);
            using DbDataAdapter adapter = Factory.CreateDataAdapter()
                ?? throw new InvalidOperationException("Npgsql could not create a data adapter.");
            adapter.SelectCommand = command;
            adapter.Fill(result);
            return result;
        }

        /// <summary>Ánh xạ các hàng truy vấn thành danh sách đối tượng kiểu T.</summary>
        public List<T> SelectCollection<T>(string sql, IDictionary? parameters = null) where T : class, new()
        {
            using DataTable table = QueryData(sql, parameters);
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanWrite && property.GetIndexParameters().Length == 0)
                .ToArray();
            var result = new List<T>(table.Rows.Count);

            foreach (DataRow row in table.Rows)
            {
                T item = new();
                foreach (PropertyInfo property in properties)
                {
                    string column = FindColumnName(table.Columns, property.Name);
                    if (column.Length == 0 || row.IsNull(column))
                    {
                        continue;
                    }

                    try
                    {
                        object value = ConvertValue(row[column], property.PropertyType);
                        property.SetValue(item, value);
                    }
                    catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException or TargetInvocationException)
                    {
                        throw new InvalidOperationException(
                            $"Unable to map column '{column}' to {typeof(T).Name}.{property.Name}.", ex);
                    }
                }
                result.Add(item);
            }
            return result;
        }

        /// <summary>Trả về đối tượng đầu tiên từ truy vấn hoặc null nếu không có kết quả.</summary>
        public T? SelectSingle<T>(string sql, IDictionary? parameters = null) where T : class, new() =>
            SelectCollection<T>(sql, parameters).FirstOrDefault();

        /// <summary>Thực thi lệnh SQL không trả hàng và trả về số hàng bị ảnh hưởng.</summary>
        public int ExecuteNonQuery(string sql, IDictionary? parameters = null)
        {
            using DbCommand command = CreateCommand(sql, parameters);
            return command.ExecuteNonQuery();
        }

        /// <summary>Thực thi lệnh SQL và trả về giá trị ô đầu tiên của kết quả.</summary>
        public object? ExecuteScalar(string sql, IDictionary? parameters = null)
        {
            using DbCommand command = CreateCommand(sql, parameters);
            return command.ExecuteScalar();
        }

        /// <summary>Chuyển đổi giá trị database sang kiểu thuộc tính đích, kể cả nullable và enum.</summary>
        private static object ConvertValue(object value, Type propertyType)
        {
            Type targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }
            if (targetType.IsEnum)
            {
                return Enum.ToObject(targetType, value);
            }
            if (targetType == typeof(Guid))
            {
                return value is string text ? Guid.Parse(text) : new Guid((byte[])value);
            }
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        /// <summary>Tìm cột khớp tên thuộc tính trực tiếp hoặc theo quy ước snake_case.</summary>
        private static string FindColumnName(DataColumnCollection columns, string propertyName)
        {
            if (columns.Contains(propertyName))
            {
                return propertyName;
            }
            string snakeCase = string.Concat(propertyName.Select((character, index) =>
                index > 0 && char.IsUpper(character) ? "_" + character : character.ToString())).ToLowerInvariant();
            return columns.Cast<DataColumn>()
                .FirstOrDefault(column => string.Equals(column.ColumnName, snakeCase, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(column.ColumnName, propertyName, StringComparison.OrdinalIgnoreCase))
                ?.ColumnName ?? string.Empty;
        }

        /// <summary>Giải phóng giao dịch và kết nối đúng một lần.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _transaction?.Dispose();
            Connection.Dispose();
            _disposed = true;
        }
    }
}
