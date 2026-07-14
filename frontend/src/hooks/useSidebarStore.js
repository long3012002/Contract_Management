import { create } from "zustand";

export const useSidebarStore = create((set) => ({
  isCollapsed: false,
  toggleSidebar: () => set((state) => ({ isCollapsed: !state.isCollapsed })),
  setCollapsed: (collapsed) => set({ isCollapsed: collapsed }),
}));
