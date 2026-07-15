import pymysql

try:
    connection = pymysql.connect(
        host='10.225.11.201',
        user='quangmd',
        password='31122001',
        database='QLDA',
        cursorclass=pymysql.cursors.DictCursor
    )
    with connection.cursor() as cursor:
        # Get all DotThanhToans
        cursor.execute("SELECT Id, TenDot, CreatedAt FROM DotThanhToans WHERE NgayThanhToan IS NULL")
        installments = cursor.fetchall()
        
        updated_count = 0
        for inst in installments:
            inst_id = inst["Id"]
            ten_dot = inst["TenDot"]
            created_at = inst["CreatedAt"]
            
            # Simple rules to assign realistic values
            if "Tạm ứng" in ten_dot:
                ngay = created_at
                dieu_kien = "Sau khi ký kết hợp đồng và nhận bảo lãnh tạm ứng"
            else:
                ngay = created_at
                dieu_kien = "Sau khi bàn giao sản phẩm và ký biên bản nghiệm thu"
                
            cursor.execute(
                "UPDATE DotThanhToans SET NgayThanhToan = %s, DieuKienThanhToan = %s WHERE Id = %s",
                (ngay, dieu_kien, inst_id)
            )
            updated_count += 1
            
        connection.commit()
        print(f"Updated {updated_count} DotThanhToan records with realistic dates and conditions.")

except Exception as e:
    print("Error:", e)
finally:
    if 'connection' in locals() and connection.open:
        connection.close()
