# TechStore (ASP.NET Core Razor Pages)

    dotnet run

- `/`          : danh sách + tìm kiếm tức thì (gõ tới đâu gửi request tới đó, debounce 250ms)
- `/Details/1` : chi tiết sản phẩm
- `/Create`    : thêm sản phẩm (upload ảnh -> wwwroot/uploads)
- `/Cart`      : giỏ hàng (lưu trong Session)

Chưa dùng database: sản phẩm thêm mới và giỏ hàng sẽ mất khi tắt app.
