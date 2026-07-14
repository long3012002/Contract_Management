import React from 'react';
import Checkbox from '@/components/Checkbox/Checkbox';
import { usePermissions } from '../hooks/usePermissions';

const ACTIONS = [
  { key: 'canAccess', label: 'Xem' },
  { key: 'create', label: 'Thêm' },
  { key: 'update', label: 'Sửa' },
  { key: 'delete', label: 'Xóa' }
];

export default function PermissionMatrix() {
  const { featuresQuery, permissionsQuery, matrixState, handlePermissionChange: onPermissionChange } = usePermissions();

  const features = featuresQuery.data || [];
  const isLoading = featuresQuery.isLoading || permissionsQuery.isLoading;

  if (isLoading) {

    return (
      <div className="w-full h-64 flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div className="rounded-custom border border-border overflow-hidden bg-card">
      <div className="overflow-x-auto">
        <table className="w-full text-sm text-left">
          <thead className="text-xs uppercase tracking-wider bg-secondary text-secondary-foreground border-b-2 border-border sticky top-0 z-10">
            <tr>
              <th scope="col" className="px-6 py-4 font-semibold w-1/3">
                Chức năng
              </th>
              {ACTIONS.map(action => (
                <th key={action.key} scope="col" className="px-6 py-4 font-semibold text-center w-1/6">
                  {action.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {features.map((feature, idx) => {
              const state = matrixState[feature.id] || { canAccess: false, create: false, update: false, delete: false };

              return (
                <tr
                  key={feature.id}
                  className="hover:bg-muted/50 transition-colors"
                >
                  <td className="px-6 py-4 font-medium text-foreground">
                    <div className="flex flex-col">
                      <span>{feature.name}</span>
                      <span className="text-xs text-muted-foreground font-normal mt-0.5">{feature.description}</span>
                    </div>
                  </td>
                  {ACTIONS.map(action => {
                    // Actions (create, update, delete) depend on canAccess
                    const isDependentAction = action.key !== 'canAccess';
                    const isDisabled = isDependentAction && !state.canAccess;

                    return (
                      <td key={`${feature.id}-${action.key}`} className="px-6 py-4 text-center">
                        <div className="flex justify-center">
                          <div className={isDisabled ? 'opacity-50 transition-opacity duration-150' : 'opacity-100 transition-opacity duration-150'}>
                            <Checkbox
                              id={`cb-${feature.id}-${action.key}`}
                              aria-label={`Quyền ${action.label} cho chức năng ${feature.name}`}
                              checked={state[action.key]}
                              disabled={isDisabled}
                              onChange={(e) => onPermissionChange(feature.id, action.key, e.target.checked)}
                              className={isDisabled ? 'cursor-not-allowed' : 'cursor-pointer'}
                            />
                          </div>
                        </div>
                      </td>
                    );
                  })}
                </tr>
              );
            })}

                  {features.length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-6 py-8 text-center text-muted-foreground">
                        Không có dữ liệu chức năng
                      </td>
                    </tr>
                  )}
                </tbody>
        </table>
      </div>
    </div>
  );
}
