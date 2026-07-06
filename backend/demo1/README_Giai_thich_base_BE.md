# Backend Quan Ly Hop Dong - Phase 1

Tai lieu nay giai thich ngan gon cau truc code hien tai de team co the doc, hieu va code tiep cung mot kieu.

## Muc Tieu Base

Base nay dang dung de len khung backend cho he thong quan ly hop dong, van ban va giay to.

Hien tai backend **dang dung MySQL thong qua EF Core + Pomelo**. Du lieu CRUD duoc luu qua `AppDbContext` va `DbCrudService`, khong con luu tam trong memory. Xem them `MYSQL_SETUP.md` de biet cach restore tool, apply migration va chay API tren may moi.

## Cau Truc Thu Muc

```text
Controllers/          Noi nhan request tu Swagger/Frontend
DTOs/                 Mau du lieu dau vao va dau ra cua API
Entity/               Doi tuong nghiep vu trong he thong
Mapper/               Chuyen doi giua Entity va DTO
Services/Interfaces/  Khai bao service co nhung chuc nang gi
Services/Implements/  Noi viet logic xu ly that
Validator/            Cac rule kiem tra nghiep vu don gian
Data/                 AppDbContext ket noi MySQL/EF Core
Program.cs            Dang ky service, Swagger, CORS, pipeline
```

## Y Nghia Cac Lop Chinh

### Controllers

Controller la cua ngo cua API. Khi frontend hoac Swagger goi API, request se vao Controller truoc.

Vi du:

```text
GET /api/contracts
POST /api/contracts
PUT /api/contracts/1
DELETE /api/contracts/1
```

Moi module co controller rieng:

```text
ProjectsController
PartnersController
BidPackagesController
ContractsController
ResolutionsController
WarningsController
```

`CrudControllerBase` chua san logic CRUD chung, giup cac controller con khong bi lap code.

### DTOs

DTO la mau du lieu API nhan vao hoac tra ra.

Co 3 nhom chinh:

```text
Create...Dto   Du lieu khi tao moi
Update...Dto   Du lieu khi cap nhat
...Dto         Du lieu tra ve cho frontend
```

Vi du voi hop dong:

```text
CreateContractDto
UpdateContractDto
ContractDto
```

Khong nen tra thang `Entity` ra API. Dung DTO de an bot thong tin noi bo va giu API on dinh hon.

### Entity

Entity dai dien cho doi tuong nghiep vu trong he thong.

Vi du:

```text
Project       Du an
Partner       Doi tac/nha cung cap
BidPackage    Goi thau
Contract      Hop dong
Resolution    Nghi quyet/van ban
```

`BaseEntity` chua cac truong dung chung:

```text
Id
Code
Name
Description
IsActive
CreatedAt
UpdatedAt
```

### Mapper

Mapper dung de chuyen doi giua `Entity` va `DTO`.

Vi du:

```text
Contract entity -> ContractDto
CreateContractDto -> Contract entity
UpdateContractDto -> cap nhat Contract entity
```

Lam vay de Controller va Service khong phai tu gan tung field qua lai.

### Services/Interfaces

Interface khai bao service lam duoc viec gi, nhung chua viet logic that.

Vi du:

```text
IContractService
IProjectService
IPartnerService
```

`ICrudService` la interface CRUD chung cho cac module co them/sua/xoa/xem danh sach.

### Services/Implements

Implements la noi viet logic that.

Vi du:

```text
ContractService
ProjectService
PartnerService
```

Hien tai cac service dang ke thua `DbCrudService`, nghia la CRUD duoc thuc hien bang EF Core tren MySQL.

### Validator

Validator chua cac rule kiem tra nghiep vu don gian.

Vi du:

```text
ContractValidator      Kiem tra gia tri hop dong, ngay hieu luc, ngay het han
BidPackageValidator    Kiem tra gia tri goi thau, nguong canh bao
BudgetValidator        Kiem tra hop dong co vuot ngan sach/goi thau khong
RenewalValidator       Kiem tra hop dong sap het han/da het han
```

## Flow Hoat Dong Cua API

Vi du khi goi API tao hop dong:

```text
Frontend/Swagger
    -> ContractsController
    -> IContractService
    -> ContractService
    -> ContractValidator
    -> ContractMapper
    -> DbCrudService/AppDbContext
    -> Tra ve ContractDto
```

Giai thich don gian:

1. Swagger/Frontend gui request.
2. Controller nhan request.
3. Controller goi service.
4. Service kiem tra nghiep vu bang Validator.
5. Mapper chuyen DTO thanh Entity.
6. DbCrudService luu du lieu vao MySQL qua AppDbContext.
7. Mapper chuyen Entity thanh DTO tra ve.
8. Controller tra response cho frontend.

## Cac API Dang Co

```text
GET    /api/projects
POST   /api/projects
PUT    /api/projects/{id}
DELETE /api/projects/{id}

GET    /api/partners
POST   /api/partners
PUT    /api/partners/{id}
DELETE /api/partners/{id}

GET    /api/bid-packages
POST   /api/bid-packages
PUT    /api/bid-packages/{id}
DELETE /api/bid-packages/{id}

GET    /api/contracts
POST   /api/contracts
PUT    /api/contracts/{id}
DELETE /api/contracts/{id}

GET    /api/resolutions
POST   /api/resolutions
PUT    /api/resolutions/{id}
DELETE /api/resolutions/{id}
```

API canh bao:

```text
GET /api/warnings/contracts-expiring-soon
GET /api/warnings/expired-contracts
GET /api/warnings/over-budget-contracts
```

## Cach Code Them Mot Module Moi

Vi du muon them module `DocumentType`, lam theo thu tu:

```text
1. Tao Entity/DocumentType.cs
2. Tao DTOs/CreateDocumentTypeDto.cs
3. Tao DTOs/UpdateDocumentTypeDto.cs
4. Tao DTOs/DocumentTypeDto.cs
5. Tao Mapper/DocumentTypeMapper.cs
6. Tao Services/Interfaces/IDocumentTypeService.cs
7. Tao Services/Implements/DocumentTypeService.cs
8. Tao Controllers/DocumentTypesController.cs
9. Dang ky service trong Program.cs
10. Build va test tren Swagger
```

Nen copy cach lam tu module `Contract` hoac `Project` de giu code dong nhat.

## Cach Chay Va Test

Chay project:

```bash
dotnet run --project demo1/demo1.csproj
```

Mo Swagger:

```text
https://localhost:44356/swagger/index.html
```

Hoac dung port hien thi trong Visual Studio khi chay project.

## Luu Y Phase 1

- Chua co database.
- Chua co dang nhap/phan quyen.
- Chua co upload file that.
- Chua co audit log.
- Chua co repository pattern.
- Du lieu in-memory chi de test flow API.

Base hien tai phu hop de team thong nhat cach chia folder, cach viet CRUD va cach test API truoc khi buoc sang phase database.
