-- ==============================================================================
-- SCRIPT IMPORT DỮ LIỆU DANH MỤC CHUẨN CHO HỆ THỐNG QUẢN LÝ DỰ ÁN & HỢP ĐỒNG
-- Cơ sở dữ liệu: PostgreSQL
-- Lưu ý: Các câu lệnh sử dụng ON CONFLICT hoặc kiểm tra tồn tại để đảm bảo an toàn,
--        có thể chạy nhiều lần mà không bị trùng lặp dữ liệu (Idempotent).
-- ==============================================================================

BEGIN;

-- Đảm bảo extension uuid-ossp hoặc pgcrypto khả dụng để sinh UUID ngẫu nhiên nếu cần
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ==============================================================================
-- 1. DANH MỤC PHÒNG BAN (PhongBans)
-- ==============================================================================
INSERT INTO "PhongBans" ("Id", "TenPhongBan", "CreatedAt")
SELECT gen_random_uuid(), val.ten, NOW() AT TIME ZONE 'UTC'
FROM (VALUES
    ('Phòng CNTT'),
    ('Phòng Kế hoạch'),
    ('Phòng Tài chính'),
    ('Phòng Nhân sự'),
    ('Phòng Pháp chế'),
    ('Phòng Kinh doanh'),
    ('Phòng Dự án'),
    ('Phòng Kỹ thuật'),
    ('Phòng R&D'),
    ('Phòng Giám sát'),
    ('Ban Quản lý dự án')
) AS val(ten)
WHERE NOT EXISTS (
    SELECT 1 FROM "PhongBans" WHERE "TenPhongBan" = val.ten
);

-- ==============================================================================
-- 2. DANH MỤC CHỨC VỤ (ChucVus)
-- ==============================================================================
INSERT INTO "ChucVus" ("Id", "Code", "TenChucVu", "Level", "CreatedAt")
SELECT gen_random_uuid(), val.code, val.ten, val.lvl, NOW() AT TIME ZONE 'UTC'
FROM (VALUES
    ('TGD', 'Tổng giám đốc', 1),
    ('GD',  'Giám đốc', 2),
    ('PGD', 'Phó giám đốc', 3),
    ('TP',  'Trưởng phòng', 4),
    ('PP',  'Phó phòng', 5),
    ('CV',  'Chuyên viên', 6)
) AS val(code, ten, lvl)
WHERE NOT EXISTS (
    SELECT 1 FROM "ChucVus" WHERE "Code" = val.code OR "TenChucVu" = val.ten
);

-- ==============================================================================
-- 3. DANH MỤC ĐƠN VỊ (DonVis)
-- ==============================================================================
INSERT INTO "DonVis" ("Id", "TenDonVi", "CreatedAt")
SELECT gen_random_uuid(), val.ten, NOW() AT TIME ZONE 'UTC'
FROM (VALUES
    ('Hội sở chính'),
    ('Chi nhánh Hà Nội'),
    ('Chi nhánh TP. Hồ Chí Minh'),
    ('Chi nhánh Đà Nẵng'),
    ('Chi nhánh Cần Thơ'),
    ('Chi nhánh Hải Phòng'),
    ('Chi nhánh Nghệ An'),
    ('Chi nhánh Khánh Hòa')
) AS val(ten)
WHERE NOT EXISTS (
    SELECT 1 FROM "DonVis" WHERE "TenDonVi" = val.ten
);

-- ==============================================================================
-- 4. DANH MỤC NGUỒN VỐN (NguonVons)
-- ==============================================================================
INSERT INTO "NguonVons" ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), val.code, val.name, val.descr, TRUE, NOW() AT TIME ZONE 'UTC', FALSE
FROM (VALUES
    ('NV_VDL_QDTR', 'Vốn điều lệ và Quỹ dự trữ bổ sung vốn điều lệ', 'Vốn điều lệ và Quỹ dự trữ bổ sung vốn điều lệ'),
    ('NV_QDTPT',    'Quỹ đầu tư phát triển', 'Quỹ đầu tư phát triển'),
    ('NV_QPL',      'Quỹ phúc lợi', 'Quỹ phúc lợi'),
    ('NV_KHAC',     'Nguồn khác', 'Nguồn khác'),
    ('NV_CN',       'Chi phí tại chi nhánh', 'Chi phí tại chi nhánh'),
    ('NV_NHHT',     'Chi phí của NHHT', 'Chi phí của NHHT')
) AS val(code, name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "NguonVons" WHERE "Code" = val.code AND "IsDeleted" = FALSE
);

-- ==============================================================================
-- 5. DANH MỤC PHÂN LOẠI DỰ ÁN / LOẠI DỰ ÁN (PhanLoaiDuAns)
-- ==============================================================================
INSERT INTO "PhanLoaiDuAns" ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), val.code, val.name, val.descr, TRUE, NOW() AT TIME ZONE 'UTC', FALSE
FROM (VALUES
    ('PL_CNTT',            'Công nghệ thông tin', 'Dự án đầu tư hạ tầng, phần mềm và giải pháp CNTT'),
    ('PL_DIGITAL_BANKING', 'Ngân hàng số & Thẻ', 'Core Banking, eBiz, Chatbot, Thẻ...'),
    ('PL_HTPM',            'Hệ thống phần mềm', 'Hệ thống các phần mềm nghiệp vụ'),
    ('PL_SECURITY',        'Mạng & An ninh bảo mật', 'Bảo mật mạng, SOC, An ninh thông tin...'),
    ('PL_HA_TANG',         'Hạ tầng', 'Hạ tầng mạng, máy chủ, trung tâm dữ liệu'),
    ('PL_MSHH_DV',         'Mua sắm hàng hóa & Dịch vụ', 'Trang thiết bị văn phòng, dịch vụ tư vấn...'),
    ('PL_XDCB',            'Xây dựng & Bảo trì', 'Sửa chữa, cải tạo trụ sở, phòng giao dịch'),
    ('PL_KHAC',            'Khác', 'Các loại dự án khác')
) AS val(code, name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "PhanLoaiDuAns" WHERE "Code" = val.code AND "IsDeleted" = FALSE
);

-- ==============================================================================
-- 6. DANH MỤC NHÓM DỰ ÁN (NhomDuAns)
-- ==============================================================================
INSERT INTO "NhomDuAns" ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), val.code, val.name, val.descr, TRUE, NOW() AT TIME ZONE 'UTC', FALSE
FROM (VALUES
    ('NHOM_A', 'Các dự án nhóm A', 'Dự án quy mô lớn nhóm A theo quy định'),
    ('NHOM_B', 'Các dự án nhóm B', 'Dự án quy mô nhóm B (tổng mức đầu tư từ 45 tỷ đến dưới 800 tỷ)'),
    ('NHOM_C', 'Các dự án nhóm C', 'Dự án quy mô nhóm C và các dự án nhỏ khác')
) AS val(code, name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "NhomDuAns" WHERE "Code" = val.code AND "IsDeleted" = FALSE
);

-- ==============================================================================
-- 7. DANH MỤC LOẠI HỢP ĐỒNG (LoaiHopDongs)
-- ==============================================================================
INSERT INTO "LoaiHopDongs" ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), val.code, val.name, val.descr, TRUE, NOW() AT TIME ZONE 'UTC', FALSE
FROM (VALUES
    ('01', 'Bảo trì', 'Hợp đồng bảo trì'),
    ('02', 'Mua sắm phần cứng', 'Hợp đồng mua sắm thiết bị, phần cứng'),
    ('03', 'Bản quyền phần mềm', 'Hợp đồng mua sắm bản quyền, phần mềm'),
    ('04', 'Tư vấn', 'Hợp đồng tư vấn (lập dự án, thẩm định, giám sát)'),
    ('05', 'Thuê dịch vụ', 'Hợp đồng thuê dịch vụ (đường truyền, cloud, server)'),
    ('07', 'Phần mềm', 'Hợp đồng phát triển, gia công phần mềm'),
    ('99', 'Khác', 'Các loại hợp đồng khác')
) AS val(code, name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "LoaiHopDongs" WHERE "Code" = val.code AND "IsDeleted" = FALSE
);

-- ==============================================================================
-- 8. DANH MỤC ĐƠN VỊ TÍNH (DonViTinhs)
-- ==============================================================================
INSERT INTO "DonViTinhs" ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), val.code, val.name, val.descr, TRUE, NOW() AT TIME ZONE 'UTC', FALSE
FROM (VALUES
    ('LICENSE',   'License',    'Đơn vị tính: License / Bản quyền'),
    ('BAN_QUYEN', 'Bản quyền', 'Đơn vị tính: Bản quyền'),
    ('USER',      'User',       'Đơn vị tính: Người dùng (User / Account)'),
    ('CORE',      'Core',       'Đơn vị tính: Core / vCPU'),
    ('SOCKET',    'Socket',     'Đơn vị tính: Socket / CPU Socket'),
    ('NODE',      'Node',       'Đơn vị tính: Node / Instance'),
    ('MODULE',    'Module',     'Đơn vị tính: Module phần mềm'),
    ('GOI',       'Gói',        'Đơn vị tính: Gói dịch vụ / Phần mềm'),
    ('HE_THONG',  'Hệ thống',   'Đơn vị tính: Hệ thống'),
    ('SERVER',    'Máy chủ',    'Đơn vị tính: Máy chủ / Server'),
    ('THIET_BI',  'Thiết bị',   'Đơn vị tính: Thiết bị'),
    ('BO',        'Bộ',         'Đơn vị tính: Bộ'),
    ('CAI',       'Cái',        'Đơn vị tính: Cái'),
    ('CHIEC',     'Chiếc',      'Đơn vị tính: Chiếc'),
    ('NAM',       'Năm',        'Đơn vị tính thời gian: Năm'),
    ('THANG',     'Tháng',      'Đơn vị tính thời gian: Tháng'),
    ('GIO',       'Giờ',        'Đơn vị tính thời gian: Giờ công / Man-hour'),
    ('NGAY',      'Ngày',       'Đơn vị tính thời gian: Ngày công / Man-day'),
    ('LUOT',      'Lượt',       'Đơn vị tính: Lượt / Lần')
) AS val(code, name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "DonViTinhs" WHERE "Code" = val.code AND "IsDeleted" = FALSE
);

-- ==============================================================================
-- 9. DANH MỤC HÃNG SẢN XUẤT (HangSanXuats)
-- ==============================================================================
INSERT INTO "HangSanXuats" ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), val.code, val.name, val.descr, TRUE, NOW() AT TIME ZONE 'UTC', FALSE
FROM (VALUES
    ('MICROSOFT', 'Microsoft',            'Tập đoàn Microsoft'),
    ('ORACLE',    'Oracle',               'Tập đoàn Oracle'),
    ('CISCO',     'Cisco Systems',        'Cisco Systems, Inc.'),
    ('DELL',      'Dell Technologies',    'Dell Inc. / Dell EMC'),
    ('HP',        'HP Inc / HPE',         'Hewlett Packard Enterprise'),
    ('VMWARE',    'VMware',               'VMware by Broadcom'),
    ('IBM',       'IBM',                  'International Business Machines'),
    ('FORTINET',  'Fortinet',             'Fortinet Inc.'),
    ('PALO_ALTO', 'Palo Alto Networks',  'Palo Alto Networks, Inc.'),
    ('APPLE',     'Apple',                'Apple Inc.'),
    ('SAMSUNG',   'Samsung',              'Samsung Electronics'),
    ('LENOVO',    'Lenovo',               'Lenovo Group Limited'),
    ('REDHAT',    'Red Hat',              'Red Hat Enterprise Linux'),
    ('CHECKPOINT','Check Point',          'Check Point Software Technologies'),
    ('F5',        'F5 Networks',          'F5, Inc.'),
    ('SAP',       'SAP',                  'SAP SE')
) AS val(code, name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "HangSanXuats" WHERE "Code" = val.code AND "IsDeleted" = FALSE
);

-- ==============================================================================
-- 10. DANH MỤC XUẤT XỨ (XuatXus)
-- ==============================================================================
INSERT INTO "XuatXus" ("Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "IsDeleted")
SELECT gen_random_uuid(), val.code, val.name, val.descr, TRUE, NOW() AT TIME ZONE 'UTC', FALSE
FROM (VALUES
    ('VN', 'Việt Nam',   'Xuất xứ Việt Nam'),
    ('US', 'Mỹ (USA)',   'Xuất xứ Hợp chúng quốc Hoa Kỳ'),
    ('JP', 'Nhật Bản',   'Xuất xứ Nhật Bản'),
    ('KR', 'Hàn Quốc',   'Xuất xứ Hàn Quốc'),
    ('DE', 'Đức',        'Xuất xứ Cộng hòa Liên bang Đức'),
    ('SG', 'Singapore',  'Xuất xứ Singapore'),
    ('CN', 'Trung Quốc', 'Xuất xứ Trung Quốc'),
    ('TW', 'Đài Loan',   'Xuất xứ Đài Loan'),
    ('UK', 'Vương quốc Anh', 'Xuất xứ Vương quốc Anh'),
    ('EU', 'Châu Âu (EU)',   'Xuất xứ Liên minh Châu Âu')
) AS val(code, name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "XuatXus" WHERE "Code" = val.code AND "IsDeleted" = FALSE
);

-- ==============================================================================
-- 11. DANH MỤC TÍNH NĂNG HỆ THỐNG (Features)
-- ==============================================================================
INSERT INTO "Features" ("Id", "Code", "Name", "Description", "ParentCode", "SortOrder", "IsActive", "CreatedAt")
SELECT gen_random_uuid(), val.code, val.name, val.descr, val.p_code, val.sort_order, TRUE, NOW() AT TIME ZONE 'UTC'
FROM (VALUES
    ('DU_AN',              'Quản lý dự án',               'Chức năng xem, thêm, sửa, xoá dự án', NULL, 10),
    ('GOI_THAU',           'Quản lý gói thầu',            'Chức năng xem, thêm, sửa, xoá gói thầu', NULL, 20),
    ('QUAN_LY_HOP_DONG',   'Quản lý hợp đồng',            'Chức năng xem, thêm, sửa, xoá hợp đồng', NULL, 30),
    ('CONG_VIEC',          'Quản lý công việc',           'Chức năng xem, thêm, sửa, xoá công việc gói thầu', NULL, 35),
    ('DOI_TAC',            'Quản lý đối tác',             'Chức năng xem, thêm, sửa, xoá đối tác', NULL, 40),
    ('KE_HOACH_VON',       'Kế hoạch vốn',                'Chức năng quản lý kế hoạch vốn đầu tư và phân kỳ vốn', NULL, 45),
    ('LICENSE',            'Quản lý Bản quyền / License', 'Chức năng quản lý license và bản quyền phần mềm', NULL, 46),
    ('DANH_MUC',           'Quản lý Danh mục dữ liệu',    'Quản lý các loại dự án, nguồn vốn, loại hợp đồng, nhóm dự án', NULL, 50),
    ('BAO_CAO',            'Báo cáo & Thống kê',          'Nhóm chức năng báo cáo tổng hợp & chi tiết', NULL, 60),
    ('BAO_CAO_TIEN_DO',    'Báo cáo 1: Tiến độ Dự án',    'Báo cáo trình tự thực hiện các công việc thuộc gói thầu và dự án', 'BAO_CAO', 61),
    ('BAO_CAO_VON',        'Báo cáo 2: Phân bổ & Vốn',    'Báo cáo kế hoạch vốn đầu tư, mua sắm và phân kỳ vốn CNTT', 'BAO_CAO', 62),
    ('BAO_CAO_DAU_THAU',   'Báo cáo 3: Nhà thầu (LCNT)',  'Báo cáo kế hoạch và kết quả lựa chọn nhà thầu', 'BAO_CAO', 63),
    ('BAO_CAO_HOP_DONG',   'Báo cáo 4: Quản lý Hợp đồng', 'Báo cáo theo dõi chi tiết tình hình thực hiện hợp đồng', 'BAO_CAO', 64),
    ('BAO_CAO_THANH_TOAN', 'Báo cáo 5: Đợt thanh toán',   'Báo cáo theo dõi giải ngân và các đợt thanh toán hợp đồng', 'BAO_CAO', 65),
    ('BAO_CAO_DU_AN_THAU', 'Báo cáo 6: TT Dự án thầu',    'Báo cáo tiến độ thanh toán tổng hợp các dự án thầu', 'BAO_CAO', 66),
    ('BAO_CAO_DAU_TU',     'Báo cáo Tổng hợp Đầu tư',     'Báo cáo tổng hợp tình hình thực hiện kinh phí đầu tư', 'BAO_CAO', 67),
    ('BAO_CAO_PHE_DUYET',  'Danh mục Dự án phê duyệt',    'Báo cáo danh mục dự án phê duyệt và hạn License / SLA', 'BAO_CAO', 68),
    ('BAO_CAO_02A',        'Báo cáo Biểu số 02.A',        'Báo cáo tổng hợp tình hình thực hiện dự án đầu tư theo Thông tư 200 (Đơn vị: Tỷ đồng)', 'BAO_CAO', 69)
) AS val(code, name, descr, p_code, sort_order)
WHERE NOT EXISTS (
    SELECT 1 FROM "Features" WHERE "Code" = val.code
);

-- ==============================================================================
-- 12. DANH MỤC VAI TRÒ (Roles) & QUYỀN (Permissions)
-- ==============================================================================
-- Roles
INSERT INTO "Roles" ("Id", "Name", "Description", "IsActive", "CreatedAt")
SELECT gen_random_uuid(), val.name, val.descr, TRUE, NOW() AT TIME ZONE 'UTC'
FROM (VALUES
    ('Admin',   'Quyền quản trị toàn hệ thống'),
    ('Manager', 'Quản lý dự án, hợp đồng'),
    ('Staff',   'Nhân viên xem và cập nhật thông tin')
) AS val(name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "Roles" WHERE "Name" = val.name
);

-- Permissions
INSERT INTO "Permissions" ("Id", "Code", "Name", "Description", "CreatedAt")
SELECT gen_random_uuid(), val.code, val.name, val.descr, NOW() AT TIME ZONE 'UTC'
FROM (VALUES
    ('VIEW',    'Xem',       'Quyền xem dữ liệu'),
    ('CREATE',  'Tạo mới',   'Quyền tạo mới dữ liệu'),
    ('EDIT',    'Chỉnh sửa', 'Quyền chỉnh sửa bản ghi'),
    ('DELETE',  'Xóa',       'Quyền xóa bản ghi'),
    ('APPROVE', 'Phê duyệt', 'Quyền phê duyệt yêu cầu')
) AS val(code, name, descr)
WHERE NOT EXISTS (
    SELECT 1 FROM "Permissions" WHERE "Code" = val.code
);

COMMIT;
