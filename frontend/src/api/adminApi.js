import { Client } from './apiClient';
import axiosClient from './axiosClient';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:64950';

export const adminApi = new Client(BASE_URL, axiosClient);

// Custom methods for Feature Management
adminApi.createFeature = async (payload) => {
  const response = await axiosClient.post('/api/Admin/features', payload);
  return response.data;
};

adminApi.updateFeature = async (featureId, payload) => {
  const response = await axiosClient.put(`/api/Admin/features/${featureId}`, payload);
  return response.data;
};

adminApi.deleteFeature = async (featureId) => {
  const response = await axiosClient.delete(`/api/Admin/features/${featureId}`);
  return response.data;
};
