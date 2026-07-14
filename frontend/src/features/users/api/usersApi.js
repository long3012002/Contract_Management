import axiosClient from '../../../api/axiosClient';

export const usersApi = {
  getUsers: async () => {
    const response = await axiosClient.get('/api/Admin/users');
    return response.data;
  },

  importExcel: async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await axiosClient.post('/api/User/import-excel', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  bulkDelete: async (ids) => {
    const response = await axiosClient.delete('/api/User/bulk-delete', {
      data: ids,
    });
    return response.data;
  },

  updateUser: async (id, data) => {
    const response = await axiosClient.put(`/api/User/${id}`, data);
    return response.data;
  },

  updateUserRoles: async (userId, roleIds) => {
    const response = await axiosClient.put(`/api/Admin/users/${userId}/roles`, {
      roleIds,
    });
    return response.data;
  },
};

