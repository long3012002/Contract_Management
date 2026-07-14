import axiosClient from '../../../api/axiosClient';

/**
 * Fetch audit logs from the API with given filters and pagination
 * @param {Object} params
 * @param {string} [params.userId]
 * @param {string} [params.date] - ISO string or yyyy-mm-dd
 * @param {string} [params.tableName]
 * @param {number} [params.page=1]
 * @param {number} [params.pageSize=20]
 * @returns {Promise<Object>} PagedResult<AuditLog>
 */
export const getAuditLogs = async (params) => {
  const response = await axiosClient.get('/api/admin/audit-logs', { params });
  return response.data;
};
