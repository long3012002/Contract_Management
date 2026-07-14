import axiosClient from '@/api/axiosClient';

export const permissionsApi = {
  getFeatures: () => axiosClient.get('/api/Admin/features'),
  getDepartments: () => axiosClient.get('/api/phong-ban'),
  getPositions: () => axiosClient.get('/api/chuc-vu'),
  getDepartmentPermissions: (deptId) => axiosClient.get(`/api/phong-ban/${deptId}/permissions`),
  getPositionPermissions: (posId) => axiosClient.get(`/api/chuc-vu/${posId}/permissions`),
  updateDepartmentPermissions: (deptId, payload) => axiosClient.put(`/api/phong-ban/${deptId}/permissions`, payload),
  updatePositionPermissions: (posId, payload) => axiosClient.put(`/api/chuc-vu/${posId}/permissions`, payload),
  
  createDepartment: (payload) => axiosClient.post('/api/phong-ban', payload),
  updateDepartment: (deptId, payload) => axiosClient.put(`/api/phong-ban/${deptId}`, payload),
  deleteDepartment: (deptId) => axiosClient.delete(`/api/phong-ban/${deptId}`),
  
  createPosition: (payload) => axiosClient.post('/api/chuc-vu', payload),
  updatePosition: (posId, payload) => axiosClient.put(`/api/chuc-vu/${posId}`, payload),
  deletePosition: (posId) => axiosClient.delete(`/api/chuc-vu/${posId}`),
};

