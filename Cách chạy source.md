# Cách chạy source

Hướng dẫn này dành cho Windows và chạy cả hai ứng dụng của Template Management System:

- **SP1 – Thư viện mẫu văn bản:** `http://localhost:5001`
- **SP2 – Thư viện mẫu trình chiếu:** `http://localhost:5002`

## 1. Yêu cầu trước khi chạy

Cài đặt và khởi động:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL
- MinIO/AIStor có thể truy cập tại `localhost:9000`

Mở PowerShell tại thư mục gốc source:

```powershell
cd "C:\Users\HP\Downloads\TemplateManagementSystem_Fixed\TemplateManagementSystem"
```

Kiểm tra .NET SDK:

```powershell
dotnet --list-sdks
```

## 2. Tạo database và nạp dữ liệu mẫu

Đảm bảo PostgreSQL đang chạy. Tạo database (chỉ cần làm một lần):

```powershell
psql -U <postgres-user> -c "CREATE DATABASE template_library_db;"
```

Tạo cấu trúc và nạp dữ liệu mẫu:

```powershell
psql -U <postgres-user> -d template_library_db -f "Database\01_CREATE_SCHEMA.sql"
psql -U <postgres-user> -d template_library_db -f "Database\02_SEED_DATA.sql"
```

Thay `<postgres-user>` bằng tên tài khoản PostgreSQL của bạn. Nếu `psql` chưa có trong `PATH`, hãy gọi `psql.exe` bằng đường dẫn cài PostgreSQL, ví dụ:

```powershell
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U <postgres-user> -d template_library_db -f "Database\01_CREATE_SCHEMA.sql"
```

## 3. Cấu hình PostgreSQL

Hai ứng dụng dùng chung User Secrets ID `template-management-local`. Đặt connection string bằng thông tin PostgreSQL của bạn:

```powershell
dotnet user-secrets set "ConnectionStrings:PostgresDb" "Host=localhost;Port=5432;Database=template_library_db;Username=<postgres-user>;Password=<postgres-password>" --project "DocumentTemplateSystem\DocumentTemplateSystem.csproj"
```

Thay các placeholder bằng thông tin thật trên máy. Không commit mật khẩu hoặc connection string chứa mật khẩu lên GitHub.

## 4. Cấu hình và khởi động MinIO/AIStor

Ứng dụng dùng bucket riêng `template-management` tại `localhost:9000`. Đặt access key và secret key giống với credentials sẽ dùng để khởi động AIStor:

```powershell
dotnet user-secrets set "ObjectStorage:AccessKey" "<access-key>" --project "DocumentTemplateSystem\DocumentTemplateSystem.csproj"
dotnet user-secrets set "ObjectStorage:SecretKey" "<secret-key>" --project "DocumentTemplateSystem\DocumentTemplateSystem.csproj"
```

Script khởi động AIStor yêu cầu:

- `minio.exe` (mặc định tìm trong thư mục `Downloads`)
- License AIStor tại `%LOCALAPPDATA%\TemplateManagementSystem\minio.license`
- User Secrets đã được cấu hình như trên

Mở **một cửa sổ PowerShell riêng** tại thư mục gốc source và giữ cửa sổ đó mở:

```powershell
powershell -ExecutionPolicy Bypass -File "Database\Start-AIStor.ps1"
```

Nếu `minio.exe` nằm ở vị trí khác, đặt biến môi trường trước khi chạy:

```powershell
$env:MINIO_SERVER_EXE = "C:\duong-dan\minio.exe"
powershell -ExecutionPolicy Bypass -File "Database\Start-AIStor.ps1"
```

Nếu đang chuyển dữ liệu object storage cũ từ thư mục `storage/`, mở cửa sổ PowerShell thứ hai và chạy:

```powershell
powershell -ExecutionPolicy Bypass -File "Database\Upload-StorageToMinio.ps1"
```

Nếu chưa có file Office cũ cần chuyển, có thể bỏ qua bước này. Ứng dụng tự tạo bucket nếu bucket chưa tồn tại.

## 5. Chạy hai ứng dụng

Đảm bảo PostgreSQL và MinIO/AIStor vẫn đang chạy. Từ PowerShell tại thư mục gốc source:

```powershell
.\Start-TemplateManagementSystem.ps1
```

Script khởi động SP1 và SP2 trên cổng `5001` và `5002`. Nếu ứng dụng cùng dự án đã chạy, script sẽ khởi động lại ứng dụng đó để nạp code mới. Nếu cổng đang được một chương trình khác sử dụng, script sẽ báo lỗi và không dừng chương trình đó.

Mở các địa chỉ sau trong trình duyệt:

- [http://localhost:5001](http://localhost:5001) — SP1, thư viện mẫu văn bản
- [http://localhost:5002](http://localhost:5002) — SP2, thư viện mẫu trình chiếu

Đăng ký tài khoản tại `/account/register` hoặc đăng nhập tại `/account/login` để dùng các chức năng tài liệu cá nhân.

### Chạy từng ứng dụng riêng lẻ

Thay vì chạy script khởi động cả hai, có thể mở hai cửa sổ PowerShell riêng:

```powershell
dotnet run --project "DocumentTemplateSystem\DocumentTemplateSystem.csproj" --urls "http://localhost:5001"
```

```powershell
dotnet run --project "PresentationTemplateSystem\PresentationTemplateSystem.csproj" --urls "http://localhost:5002"
```

## 6. Chạy kiểm thử

Từ thư mục gốc source:

```powershell
dotnet test "TemplateSystem.Tests\TemplateSystem.Tests.csproj"
```

## 7. Dừng ứng dụng

- Nếu chạy `dotnet run` trong cửa sổ PowerShell, nhấn `Ctrl+C` trong cửa sổ tương ứng.
- Nếu dùng script khởi động, có thể dừng ứng dụng bằng cách đóng các tiến trình tương ứng trong VS Code/Task Manager hoặc khởi động lại máy.
- Để dừng AIStor được chạy trong cửa sổ PowerShell, nhấn `Ctrl+C` tại cửa sổ đó.

## 8. Một số lỗi thường gặp

### Không kết nối được PostgreSQL

- Kiểm tra dịch vụ PostgreSQL đang chạy và cổng `5432` có thể truy cập.
- Kiểm tra database `template_library_db`, username/password và connection string trong User Secrets.
- Đảm bảo đã chạy `01_CREATE_SCHEMA.sql` và `02_SEED_DATA.sql`.

### Không kết nối được MinIO/AIStor

- Kiểm tra cửa sổ AIStor vẫn đang chạy và endpoint là `localhost:9000`.
- Kiểm tra `ObjectStorage:AccessKey` và `ObjectStorage:SecretKey` trùng với credentials khi chạy AIStor.
- Kiểm tra file license và đường dẫn `minio.exe`.

### Cổng 5001 hoặc 5002 đang được sử dụng

Đóng ứng dụng cũ đang dùng cổng đó rồi chạy lại `.\Start-TemplateManagementSystem.ps1`. Script không tự dừng tiến trình không thuộc dự án này.

### Thay đổi code nhưng trình duyệt vẫn hiện giao diện cũ

Khởi động lại ứng dụng bằng `.\Start-TemplateManagementSystem.ps1`, sau đó tải lại trang bằng `Ctrl+F5`.

## Tham khảo thêm

- [README.md](./README.md) — kiến trúc, cấu hình và thông tin chi tiết dự án
- [Database/01_CREATE_SCHEMA.sql](./Database/01_CREATE_SCHEMA.sql) — tạo/cập nhật cấu trúc database
- [Database/02_SEED_DATA.sql](./Database/02_SEED_DATA.sql) — dữ liệu mẫu
- [Start-TemplateManagementSystem.ps1](./Start-TemplateManagementSystem.ps1) — khởi động hai ứng dụng
