import { useEffect, useRef, useState } from "react";
import axios from "axios";
import { useAuthStore } from "../store/authStore";
import { mockRbacService } from "../../../api/mockRbac";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:64950';

export function useAuthInit() {
  const [isInitializing, setIsInitializing] = useState(true);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const completeMfa = useAuthStore((state) => state.completeMfa);
  // useRef survives StrictMode double-mount but resets on hard reload (unlike module-level var)
  const authInitDone = useRef(false);

  useEffect(() => {
    // If already authenticated (e.g. navigating between pages), skip immediately
    if (isAuthenticated) {
      setIsInitializing(false);
      return;
    }

    // If init already ran (e.g. StrictMode second mount), skip
    if (authInitDone.current) {
      setIsInitializing(false);
      return;
    }

    authInitDone.current = true;

    const initAuth = async () => {
      try {
        const response = await axios.post(
          `${BASE_URL}/api/Auth/refresh`,
          {},
          { withCredentials: true }
        );
        // Handle both camelCase (with backend fix) and PascalCase (fallback)
        const accessToken = response.data.accessToken ?? response.data.AccessToken;
        const username = response.data.username ?? response.data.Username;
        const apiIsSystemAdmin = response.data.isSystemAdmin ?? response.data.IsSystemAdmin;
        const apiPermissions = response.data.permissions ?? response.data.Permissions;

        const userPerms = mockRbacService.calculateUserPermissions(username);
        const isSystemAdmin = apiIsSystemAdmin ?? userPerms.isSystemAdmin;
        const permissions = apiPermissions ?? userPerms.permissions;

        completeMfa({
          username,
          name: username,
          accessToken,
          isSystemAdmin,
          permissions,
        });
      } catch (error) {
        // Silently fail if refresh token is invalid/expired
      } finally {
        setIsInitializing(false);
      }
    };

    initAuth();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return { isInitializing };
}

