# ContractManagement.Api

Base Backend tối giản cho hệ thống quản lý hợp đồng và hồ sơ nghiệp vụ.

## Công nghệ

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- SQL Server
- Swagger

## Cấu trúc

```text
Controllers
Data
DTOs
Entities
Services
Program.cs
appsettings.json
```

## API mẫu

Module: DocumentType - danh mục loại hồ sơ/giấy tờ.

```text
GET    /api/document-types
GET    /api/document-types/{id}
POST   /api/document-types
PUT    /api/document-types/{id}
DELETE /api/document-types/{id}
```

## Cách chạy

```bash
dotnet restore
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Mở Swagger:

```text
https://localhost:<port>/swagger
```

## Ghi chú

Base này cố tình giữ đơn giản:
- Chưa dùng Clean Architecture.
- Chưa dùng Repository Pattern.
- Chưa dùng JWT/Auth.
- Chưa dùng Upload File.
- Chưa dùng Background Job/Notification.

Các phần này sẽ bổ sung sau khi thống nhất nghiệp vụ và database chính.
