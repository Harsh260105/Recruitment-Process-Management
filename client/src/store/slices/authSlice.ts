import type { StateCreator } from "zustand";
import type { AuthSlice, RootStore } from "../types";

const baseState = {
  token: null,
  user: null,
  roles: [] as string[],
  isAuthenticated: false,
};

export const createAuthSlice: StateCreator<RootStore, [], [], AuthSlice> = (
  set
) => ({
  auth: {
    ...baseState,
    setAuth: (token, user, roles) => {
      set((state) => ({
        auth: {
          ...state.auth,
          token,
          user,
          roles: roles ?? user.roles ?? [],
          isAuthenticated: true,
        },
      }));
    },

    setToken: (token) => {
      set((state) => ({
        auth: {
          ...state.auth,
          token,
          isAuthenticated: Boolean(token),
        },
      }));
    },

    setUser: (user) => {
      set((state) => ({
        auth: {
          ...state.auth,
          user,
          roles: user?.roles ?? state.auth.roles,
        },
      }));
    },

    setRoles: (roles) => {
      set((state) => ({
        auth: {
          ...state.auth,
          roles,
        },
      }));
    },

    logout: () => {
      set((state) => ({
        auth: {
          ...state.auth,
          ...baseState,
        },
      }));
    },

    reset: () => {
      set((state) => ({
        auth: {
          ...state.auth,
          ...baseState,
        },
      }));
    },
  },
});
