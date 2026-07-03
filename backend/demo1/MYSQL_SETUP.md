# MySQL setup for backend

Backend dang dung MySQL qua `Pomelo.EntityFrameworkCore.MySql`.

## Chay lan dau voi database chung

Tu root repo:

```powershell
dotnet tool restore
dotnet restore backend\demo1\demo1.sln
dotnet ef database update --project backend\demo1\demo1\demo1.csproj --startup-project backend\demo1\demo1\demo1.csproj
dotnet run --project backend\demo1\demo1\demo1.csproj
```

Swagger mac dinh nam o `/swagger` khi chay moi truong Development.

Voi database chung, chi nen de mot nguoi trong team chay lenh `dotnet ef database update` sau khi da thong bao moi nguoi. Cac may con lai chi can `git pull`, restore va chay app.

## Cau hinh database rieng cho tung may

Neu can doi database local, dung user secrets de khong sua file config chung:

```powershell
cd backend\demo1\demo1
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;database=QLDA;user=root;password=your_password;"
```

Neu dung database local rieng va muon app tu apply migration/seed data khi start:

```powershell
dotnet user-secrets set "Database:AutoMigrate" "true"
dotnet user-secrets set "Database:SeedSampleData" "true"
```

## Luu y quan trong

- App khong con goi `EnsureDeleted()`, nen khong xoa database moi lan chay.
- Mac dinh `Database:AutoMigrate=false`, nen app khong tu sua schema DB chung khi start.
- `Database:SeedSampleData=true` chi nen bat cho database local/demo. Du lieu demo duoc check theo `Code`, nen chay lai khong bi nhan doi.
- Khi them/sua Entity, tao migration moi:

```powershell
dotnet ef migrations add TenMigration --project backend\demo1\demo1\demo1.csproj --startup-project backend\demo1\demo1\demo1.csproj
dotnet ef database update --project backend\demo1\demo1\demo1.csproj --startup-project backend\demo1\demo1\demo1.csproj
```
