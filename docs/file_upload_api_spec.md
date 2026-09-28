# Hướng dẫn Tích hợp API Quản lý & Tải lên Tệp tin (File Attachment API Specification)

Tài liệu này cung cấp đầy đủ thông số kỹ thuật, định dạng dữ liệu đầu vào (parameters), cấu trúc phản hồi (response), các ràng buộc hợp lệ (validations) và mã nguồn mẫu (TypeScript / React) để đội ngũ Frontend (FE) tích hợp các tính năng đính kèm và quản lý tệp tin.

---

## 1. Danh sách các API trong phân hệ Tệp tin (`/api/HeThong/files`)

Hệ thống cung cấp một cụm API thống nhất để tải lên, tải xuống, lấy danh sách, tích hợp xem/sửa tài liệu trực tuyến (ONLYOFFICE) và xóa tệp đính kèm:

1. **`POST /api/HeThong/files/upload`**: Tải lên tệp tin đính kèm cho một thực thể nghiệp vụ.
2. **`GET /api/HeThong/files/by-entity`**: Lấy danh sách tệp đính kèm theo mã thực thể.
3. **`GET /api/HeThong/files/download/by-id/{id}`**: Tải xuống hoặc xem tệp tin trực tiếp theo ID.
4. **`GET /api/HeThong/files/download`**: Tải xuống tệp tin bằng đường dẫn tương đối (Relative Path).
5. **`GET /api/HeThong/files/{id}/onlyoffice-config`**: Lấy cấu hình khởi tạo trình soạn thảo ONLYOFFICE (chế độ `view` hoặc `edit`).
6. **`GET /api/HeThong/files/{id}/versions`**: Lấy lịch sử tất cả các phiên bản của tệp tin.
7. **`DELETE /api/HeThong/files/delete-multiple`**: Xóa hàng loạt tệp đính kèm theo danh sách GUID.

---

## 2. Chi tiết API Tải lên Tệp tin (`POST /api/HeThong/files/upload`)

### 2.1. Thông tin Endpoint
- **URL**: `/api/HeThong/files/upload`
- **Method**: `POST`
- **Content-Type**: `multipart/form-data`
- **Xác thực (Authentication)**: Bắt buộc đính kèm JWT Bearer Token trong Header.

### 2.2. Request Headers

| Header | Kiểu dữ liệu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `Authorization` | `string` | **Có** | `Bearer <access_token>` của người dùng đã đăng nhập |
| `Content-Type` | `string` | **Có** | `multipart/form-data` (trình duyệt hoặc thư viện HTTP client như Axios/Fetch sẽ tự động sinh kèm boundary) |

### 2.3. Request Body (Form Data Parameters)

Dữ liệu gửi lên phải được đóng gói dưới dạng **`FormData`** với các key cụ thể sau:

| Tên tham số (Key) | Kiểu dữ liệu | Bắt buộc | Mô tả & Quy định |
| :--- | :--- | :---: | :--- |
| **`file`** | `File` / `Blob` | **Có** | Tệp tin cần tải lên từ thiết bị của người dùng. |
| **`featureCode`** | `string` | **Có** | Mã phân hệ / tính năng chức năng chứa tệp tin đính kèm. Không được để trống. |
| **`entityId`** | `string` (UUID/GUID) | **Có** | Khóa chính (`Id`) của bản ghi nghiệp vụ cần đính kèm tệp tin. |

#### Danh sách `featureCode` chuẩn hóa trong hệ thống:
| `featureCode` | Tên nghiệp vụ tương ứng | Ghi chú / Đối tượng gắn tệp |
| :--- | :--- | :--- |
| **`QUAN_LY_HOP_DONG`** | Quản lý Hợp đồng | Đính kèm file Hợp đồng chính, Phụ lục hợp đồng, Biên bản nghiệm thu/thanh lý |
| **`DU_AN`** | Quản lý Dự án | Đính kèm hồ sơ dự án, tờ trình, quyết định phê duyệt chủ trương |
| **`GOI_THAU`** | Quản lý Gói thầu | Đính kèm HSMT, HSDT, tài liệu công việc gói thầu (`CongViecGoiThau`) |
| **`DOI_TAC`** | Quản lý Đối tác / Nhà thầu | Hồ sơ năng lực, giấy tờ pháp lý đối tác |
| **`NGHI_QUYET`** | Quản lý Nghị quyết | Đính kèm văn bản nghị quyết HĐQT, ban điều hành |

### 2.4. Quy định & Ràng buộc Dữ liệu (Validation Rules)
1. **Dung lượng tối đa**: `50 MB` (52,428,800 bytes). Nếu vượt quá, Backend trả về lỗi `400 Bad Request`.
2. **Định dạng file được hỗ trợ**:
   - Tài liệu văn bản / Bảng tính: `.pdf`, `.docx`, `.doc`, `.xlsx`, `.xls`
   - Hình ảnh: `.png`, `.jpg`, `.jpeg`
3. **Mã thực thể (`entityId`)**: Phải là một UUID hợp lệ, khác GUID rỗng (`00000000-0000-0000-0000-000000000000`).

---

## 3. Cấu trúc Phản hồi (Response Specs)

### 3.1. Thành công (`200 OK`)
Trả về thông tin chi tiết của bản ghi tệp tin vừa được tạo trong CSDL:

```json
{
  "fileAttachmentId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "fileName": "Hop_Dong_Dich_Vu_CNTT.pdf",
  "relativePath": "a1b2c3d4-e5f6-7890-abcd-ef1234567890/QUAN_LY_HOP_DONG/8f7e6d5c-4b3a-2109-8765-4321fedcba09/a1b2c3d4-e5f6-7890-abcd-ef1234567890.pdf",
  "contentType": "application/pdf",
  "fileSize": 2048576,
  "createdAt": "2026-09-24T07:13:00.123Z"
}
```

**Mô tả các trường trong Response:**
- `fileAttachmentId`: Khóa chính định danh duy nhất (GUID) của tệp tin. Dùng ID này để gọi các API tải xuống, xem lịch sử phiên bản hoặc mở ONLYOFFICE.
- `fileName`: Tên gốc của tệp tin khi người dùng tải lên (dùng để hiển thị lên UI).
- `relativePath`: Đường dẫn tương đối lưu trữ trên máy chủ backend.
- `contentType`: MIME type của tệp tin (ví dụ `application/pdf`, `image/png`).
- `fileSize`: Dung lượng tệp tin tính theo đơn vị bytes.
- `createdAt`: Thời gian tải lên (chuỗi ISO 8601 UTC).

### 3.2. Lỗi dữ liệu không hợp lệ (`400 Bad Request`)
```json
{
  "message": "File vượt quá giới hạn cho phép (50 MB)."
}
```
*Các thông báo lỗi khác:*
- `"File không hợp lệ hoặc rỗng."`
- `"Định dạng file không được hỗ trợ."`
- `"Mã tính năng (featureCode) không được để trống."`
- `"Mã thực thể (entityId) không hợp lệ."`

### 3.3. Lỗi xác thực & phân quyền (`401 Unauthorized` / `403 Forbidden`)
- **`401 Unauthorized`**: Người dùng chưa đăng nhập hoặc Token đã hết hạn.
- **`403 Forbidden`**: Người dùng không có quyền truy cập hoặc thao tác trên phân hệ tương ứng.

### 3.4. Lỗi máy chủ (`500 Internal Server Error`)
```json
{
  "message": "Đã xảy ra lỗi trong quá trình upload file.",
  "detail": null
}
```

---

## 4. Các API Phụ trợ Dành cho Giao diện Đính kèm File

### 4.1. Lấy danh sách file đính kèm của một bản ghi (`GET /api/HeThong/files/by-entity`)
- **Query Params**:
  - `featureCode`: Mã phân hệ (ví dụ: `QUAN_LY_HOP_DONG`)
  - `entityId`: Mã định danh bản ghi (GUID)
- **Response `200 OK`**:
```json
[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "fileName": "Hop_Dong_Mua_Sam.pdf",
    "filePath": "a1b2c3d4-e5f6-7890-abcd-ef1234567890/QUAN_LY_HOP_DONG/.../file.pdf",
    "contentType": "application/pdf",
    "fileSize": 1048576,
    "createdAt": "2026-09-24T07:13:00.123Z"
  }
]
```

### 4.2. Tải xuống / Xem file (`GET /api/HeThong/files/download/by-id/{id}`)
- **Path Param**: `id` (GUID của tệp tin).
- **Response**: Trả về trực tiếp Binary Stream của file, kèm `Content-Disposition` và hỗ trợ resume download qua `Range Processing`.

### 4.3. Xóa nhiều file (`DELETE /api/HeThong/files/delete-multiple`)
- **Body (JSON Array)**: Danh sách các GUID file cần xóa:
```json
[
  "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "b2c3d4e5-f6a7-8901-bcde-f12345678901"
]
```
- **Response `200 OK`**:
```json
{
  "message": "Đã xóa cứng thành công 2 file.",
  "deletedIds": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ]
}
```

---

## 5. Code mẫu Tích hợp Frontend (TypeScript + React + Axios)

### 5.1. File Service API (`fileAttachmentApi.ts`)
```typescript
import axios from 'axios';

export interface FileAttachmentDto {
  id: string;
  fileName: string;
  filePath: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
}

export interface UploadFileResponse {
  fileAttachmentId: string;
  fileName: string;
  relativePath: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
}

export type FeatureCodeType = 
  | 'QUAN_LY_HOP_DONG' 
  | 'DU_AN' 
  | 'GOI_THAU' 
  | 'DOI_TAC' 
  | 'NGHI_QUYET';

// 1. Tải lên tệp tin
export const uploadFileAttachment = async (
  file: File,
  featureCode: FeatureCodeType,
  entityId: string,
  onProgress?: (percent: number) => void
): Promise<UploadFileResponse> => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('featureCode', featureCode);
  formData.append('entityId', entityId);

  const response = await axios.post<UploadFileResponse>(
    '/api/HeThong/files/upload',
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && onProgress) {
          const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress(percent);
        }
      },
    }
  );

  return response.data;
};

// 2. Lấy danh sách file theo thực thể
export const getAttachmentsByEntity = async (
  featureCode: FeatureCodeType,
  entityId: string
): Promise<FileAttachmentDto[]> => {
  const response = await axios.get<FileAttachmentDto[]>('/api/HeThong/files/by-entity', {
    params: { featureCode, entityId },
  });
  return response.data;
};

// 3. Đường dẫn tải xuống file
export const getDownloadFileUrl = (fileId: string): string => {
  return `/api/HeThong/files/download/by-id/${fileId}`;
};

// 4. Xóa nhiều file
export const deleteMultipleFiles = async (fileIds: string[]): Promise<void> => {
  await axios.delete('/api/HeThong/files/delete-multiple', {
    data: fileIds,
  });
};
```

### 5.2. Component React mẫu (`FileUploadSection.tsx`)
```tsx
import React, { useState } from 'react';
import { uploadFileAttachment, FeatureCodeType } from './fileAttachmentApi';

interface FileUploadSectionProps {
  featureCode: FeatureCodeType;
  entityId: string;
  onUploadSuccess?: () => void;
}

export const FileUploadSection: React.FC<FileUploadSectionProps> = ({
  featureCode,
  entityId,
  onUploadSuccess,
}) => {
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState<number>(0);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Client-side validation
    if (file.size > 50 * 1024 * 1024) {
      setErrorMsg('Dung lượng file không được vượt quá 50MB.');
      return;
    }

    try {
      setUploading(true);
      setErrorMsg(null);
      setProgress(0);

      await uploadFileAttachment(file, featureCode, entityId, (percent) => {
        setProgress(percent);
      });

      // Reset input sau khi upload thành công
      e.target.value = '';
      if (onUploadSuccess) {
        onUploadSuccess();
      }
    } catch (err: any) {
      const message = err.response?.data?.message || 'Tải lên file thất bại.';
      setErrorMsg(message);
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="flex flex-col gap-2">
      <label className="text-sm font-medium text-gray-700">Tài liệu đính kèm</label>
      <input
        type="file"
        disabled={uploading}
        onChange={handleFileChange}
        accept=".pdf,.docx,.doc,.xlsx,.xls,.png,.jpg,.jpeg"
        className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-primary file:text-white hover:file:bg-primary/90 cursor-pointer"
      />
      {uploading && (
        <div className="w-full bg-gray-200 rounded-full h-2.5">
          <div
            className="bg-blue-600 h-2.5 rounded-full transition-all duration-300"
            style={{ width: `${progress}%` }}
          />
        </div>
      )}
      {errorMsg && <p className="text-xs text-red-500">{errorMsg}</p>}
    </div>
  );
};
```
