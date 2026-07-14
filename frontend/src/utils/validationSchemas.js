import * as z from 'zod';

// Define validation schema using Zod
export const loginSchema = z.object({
  username: z.string().min(1, 'Vui lòng nhập tên đăng nhập hoặc mã nhân viên.'),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu.'),
});
