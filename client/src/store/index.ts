import { create } from "zustand";
import { devtools, persist } from "zustand/middleware";
import type { RootStore } from "./types";
import { createAuthSlice } from "./slices/authSlice";
import { createUISlice } from "./slices/uiSlice";

export const useStore = create<RootStore>()(
  devtools(
    persist(
      (...args) => ({
        ...createAuthSlice(...args),
        ...createUISlice(...args),
      }),
      {
        name: "recruitment-store",
        partialize: (state) => ({
          auth: {
            token: state.auth.token,
            user: state.auth.user,
            roles: state.auth.roles,
            isAuthenticated: state.auth.isAuthenticated,
          },
          ui: {
            theme: state.ui.theme, // Only persist theme, not ephemeral UI state
          },
        }),
        merge: (persistedState, currentState) => {
          const persisted = persistedState as Partial<RootStore> | undefined;

          return {
            ...currentState,
            auth: {
              ...currentState.auth,
              ...(persisted?.auth ?? {}),
            },
            ui: {
              ...currentState.ui,
              ...(persisted?.ui ?? {}),
            },
          } satisfies RootStore;
        },
      }
    ),
    { name: "RecruitmentStore" }
  )
);

export const useAuth = useStore;
export const useUI = useStore;
