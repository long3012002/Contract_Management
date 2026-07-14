/**
 * Che bớt ký tự của tên đăng nhập để bảo mật thông tin hiển thị.
 * Ví dụ: admin -> a***n, newuser -> n*****r
 * 
 * @param {string} username Tên đăng nhập cần che
 * @returns {string} Chuỗi tên đăng nhập đã được che
 */
export const maskUsername = (username) => {
  if (!username) return '***';
  if (username.length <= 2) return username;
  return username[0] + '*'.repeat(username.length - 2) + username[username.length - 1];
};

/**
 * Định dạng tiền tệ VND.
 * @param {number|null|undefined} value
 * @returns {string}
 */
export const formatVND = (value) => {
  if (value == null) return '—';
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value);
};

/**
 * Định dạng ngày tháng theo chuẩn vi-VN.
 * @param {string|Date|null|undefined} dateStr
 * @returns {string}
 */
export const formatDate = (dateStr) => {
  if (!dateStr) return '—';
  return new Date(dateStr).toLocaleDateString('vi-VN');
};

