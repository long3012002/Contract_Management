# MySQL setup for backend

Backend dang dung MySQL qua `Pomelo.EntityFrameworkCore.MySql`.

## Chay lan dau

Tu root repo:

```powershell
dotnet tool restore
dotnet restore backend\demo1\demo1.sln
dotnet ef database update --project backend\demo1\demo1\demo1.csproj --startup-project backend\demo1\demo1\demo1.csproj
dotnet run --project backend\demo1\demo1\demo1.csproj
```

Swagger mac dinh nam o `/swagger` khi chay moi truong Development.

## Cau hinh database rieng cho tung may

Neu can doi database local, dung user secrets de khong sua file config chung:

```powershell
cd backend\demo1\demo1
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;database=QLDA;user=root;password=your_password;"
```

## Luu y quan trong

- App khong con goi `EnsureDeleted()`, nen khong xoa database moi lan chay.
- Trong Development, `Database:AutoMigrate=true`, app se tu apply migration con thieu khi start.
- Khi them/sua Entity, tao migration moi:

```powershell
dotnet ef migrations add TenMigration --project backend\demo1\demo1\demo1.csproj --startup-project backend\demo1\demo1\demo1.csproj
dotnet ef database update --project backend\demo1\demo1\demo1.csproj --startup-project backend\demo1\demo1\demo1.csproj
```
