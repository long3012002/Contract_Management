import axiosClient from './axiosClient';

export const partnerApi = {
  getAll: async (search = '', page = 1, pageSize = 10) => {
    const response = await axiosClient.get('/api/partners', {
      params: { search, page, pageSize }
    });
    return response.data;
  },

  getById: async (id) => {
    const response = await axiosClient.get(`/api/partners/${id}`);
    return response.data;
  },

  create: async (payload) => {
    // Backend expects IEnumerable<TCreateDto>, so wrap single payload in array
    const response = await axiosClient.post('/api/partners', [payload]);
    return response.data;
  },

  update: async (id, payload) => {
    const response = await axiosClient.put(`/api/partners/${id}`, payload);
    return response.data;
  },

  delete: async (id) => {
    const response = await axiosClient.delete(`/api/partners/${id}`);
    return response.data;
  }
};
