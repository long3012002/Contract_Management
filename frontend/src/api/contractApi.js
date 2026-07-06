import axiosClient from './axiosClient';

export const contractApi = {
  getAll: async (search = '', page = 1, pageSize = 20) => {
    // API returns ContractDtoPagedResult which usually contains items, totalCount, etc.
    const response = await axiosClient.get('/api/contracts', {
      params: { search, page, pageSize }
    });
    return response.data;
  },

  getById: async (id) => {
    const response = await axiosClient.get(`/api/contracts/${id}`);
    return response.data;
  },

  create: async (payload) => {
    // The C# endpoint: Create([FromBody] IEnumerable<TCreateDto> dtos)
    // So we wrap the single creation payload in an array.
    const response = await axiosClient.post('/api/contracts', [payload]);
    return response.data;
  },

  update: async (id, payload) => {
    const response = await axiosClient.put(`/api/contracts/${id}`, payload);
    return response.data;
  },

  delete: async (id) => {
    const response = await axiosClient.delete(`/api/contracts/${id}`);
    return response.data;
  }
};
