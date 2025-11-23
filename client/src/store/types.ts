import type { components } from "../types/api";

type Schemas = components["schemas"];

export type UserProfile = Schemas["UserProfileDto"] | null;

export interface AuthState {
  token: string | null;
  user: UserProfile;
  roles: string[];
  isAuthenticated: boolean;
  setAuth: (
    token: string,
    user: Schemas["UserProfileDto"],
    roles?: string[]
  ) => void;
  setToken: (token: string | null) => void;
  setUser: (user: Schemas["UserProfileDto"] | null) => void;
  setRoles: (roles: string[]) => void;
  logout: () => void;
  reset: () => void;
}

export interface UIState {
  theme: "light" | "dark" | "system";
  modals: Record<string, boolean>;
  drawers: Record<string, boolean>;
  globalLoading: boolean;
  notifications: Array<{
    id: string;
    type: "success" | "error" | "warning" | "info";
    message: string;
    duration?: number;
  }>;
  setTheme: (theme: "light" | "dark" | "system") => void;
  openModal: (modalId: string) => void;
  closeModal: (modalId: string) => void;
  toggleModal: (modalId: string) => void;
  openDrawer: (drawerId: string) => void;
  closeDrawer: (drawerId: string) => void;
  toggleDrawer: (drawerId: string) => void;
  setGlobalLoading: (loading: boolean) => void;
  addNotification: (
    notification: Omit<UIState["notifications"][0], "id">
  ) => void;
  removeNotification: (id: string) => void;
  clearNotifications: () => void;
}

export interface AuthSlice {
  auth: AuthState;
}

export interface UISlice {
  ui: UIState;
}

export type RootStore = AuthSlice & UISlice;
