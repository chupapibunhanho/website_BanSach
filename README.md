# website_BanSach

## Cấu hình database trên máy phát triển

Project sử dụng ASP.NET MVC 5, .NET Framework 4.8.1 và SQL Server. Cần có Visual Studio 2022 với workload **ASP.NET and web development**, .NET Framework 4.8.1 Developer Pack và một database `Bookstore` đã được thiết lập tương thích với project.

1. Trong thư mục `webstie_BanSach`, sao chép `ConnectionStrings.example.config` thành `ConnectionStrings.local.config`. File này bắt buộc vì `Web.config` sử dụng `configSource` để đọc kết nối.
2. Mặc định, file mẫu dùng SQL Server instance mặc định trên `localhost`, database `Bookstore` và Windows Authentication. Nếu máy dùng instance khác, sửa `Data Source` trong file cục bộ, ví dụ `localhost\SQLEXPRESS`. Tài khoản Windows chạy Visual Studio/IIS Express phải có quyền phù hợp với database.
3. `ConnectionStrings.local.config` được Git bỏ qua. Không commit file này hoặc đưa mật khẩu vào file mẫu hay các file được theo dõi. Thiết lập `TrustServerCertificate=True` trong mẫu dành cho môi trường phát triển; xem xét chứng chỉ và chính sách bảo mật phù hợp khi triển khai.
4. Mở `webstie_BanSach/webstie_BanSach.sln` trong Visual Studio 2022, khôi phục NuGet packages nếu cần, chọn project làm Startup Project, build và chạy bằng IIS Express. Trang `/BookMgmt/BookList` có thể dùng để kiểm tra đọc dữ liệu sách; trang chủ hiện là mẫu tĩnh.

Không chạy các script tạo database hoặc dữ liệu mẫu lên database hiện có chỉ để thử kết nối. Trước khi thiết lập database mới, cần kiểm tra nội dung script và tính tương thích với môi trường đích.
