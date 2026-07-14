import React, { createContext } from 'react';
import { usePermissionsLogic } from '../hooks/usePermissions';

export const PermissionsContext = createContext(null);

export const PermissionsProvider = ({ children }) => {
  const value = usePermissionsLogic();

  return (
    <PermissionsContext.Provider value={value}>
      {children}
    </PermissionsContext.Provider>
  );
};

