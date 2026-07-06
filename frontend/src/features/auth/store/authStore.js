import { create } from 'zustand';

export const useAuthStore = create((set) => ({
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
}));

