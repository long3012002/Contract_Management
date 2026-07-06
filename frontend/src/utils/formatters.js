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
