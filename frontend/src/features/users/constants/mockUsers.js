export const MOCK_USERS = Array.from({ length: 50 }).map((_, index) => {
  const id = `usr-${index + 1}`.padStart(7, '0'); // usr-001
  const firstNames = ['Nguyễn', 'Trần', 'Lê', 'Phạm', 'Hoàng', 'Huỳnh', 'Phan', 'Vũ', 'Võ', 'Đặng', 'Bùi', 'Đỗ', 'Hồ', 'Ngô', 'Dương'];
  const middleNames = ['Văn', 'Thị', 'Hữu', 'Đức', 'Thanh', 'Minh', 'Ngọc', 'Quang', 'Hải', 'Thành', 'Thu', 'Kim', 'Xuân'];
  const lastNames = ['Hùng', 'Hương', 'Linh', 'Anh', 'Bình', 'Cường', 'Dũng', 'Em', 'Giang', 'Hà', 'Khang', 'Lan', 'Mai', 'Nam', 'Oanh', 'Phương', 'Quyên', 'Sơn', 'Trang', 'Uyên', 'Vinh', 'Yến'];
  
  const firstName = firstNames[index % firstNames.length];
  const middleName = middleNames[(index * 3) % middleNames.length];
  const lastName = lastNames[(index * 7) % lastNames.length];
  const fullName = `${firstName} ${middleName} ${lastName}`;
  
  // Basic romanization for username/email
  const noAccentName = fullName.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d").replace(/Đ/g, "D");
  const nameParts = noAccentName.toLowerCase().split(' ');
  const username = `${nameParts[nameParts.length - 1]}.${nameParts[0]}${nameParts[1] ? nameParts[1][0] : ''}${index}`;
  const email = `${username}@quanlyduan.vn`;
  
  const isSystemAdmin = index === 0;
  
  const rolesOptions = [
    ['Developer'],
    ['Tester'],
    ['Project Manager'],
    ['Business Analyst'],
    ['Designer'],
    ['Developer', 'Scrum Master'],
    ['DevOps']
  ];
  const roles = isSystemAdmin ? ['Admin'] : rolesOptions[index % rolesOptions.length];

  return {
    id,
    fullName,
    username,
    email,
    phone: `09${Math.floor(Math.random() * 100000000).toString().padStart(8, '0')}`,
    roles,
    isSystemAdmin,
    isActive: index % 10 !== 9, // 1 in 10 is locked
  };
});
