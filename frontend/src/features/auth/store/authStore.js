import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export const useAuthStore = create(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      tempUser: null,
      mfaPending: false,
      isMfaSetup: false,

      setAuth: (user) => {
        set({ user, isAuthenticated: true, tempUser: null, mfaPending: false });
      },

      setMfaPending: (user, isSetup) => {
        set({ tempUser: user, mfaPending: true, isMfaSetup: isSetup });
      },

      completeMfa: (user) => {
        set({ user, isAuthenticated: true, tempUser: null, mfaPending: false });
      },

      cancelMfa: () => {
        set({ tempUser: null, mfaPending: false, isMfaSetup: false });
      },

      logout: () => {
        set({ user: null, isAuthenticated: false, tempUser: null, mfaPending: false });
      },
    }),
    {
      name: 'auth-storage',
    }
  )
);


