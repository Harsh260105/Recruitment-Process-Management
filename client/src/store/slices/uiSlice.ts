import type { StateCreator } from "zustand";
import type { UISlice, RootStore, UIState } from "../types";

const baseState = {
    theme: "system" as "light" | "dark" | "system",
    modals: {} as Record<string, boolean>,
    drawers: {} as Record<string, boolean>,
    globalLoading: false,
    notifications: [] as Array<{
        id: string;
        type: "success" | "error" | "warning" | "info";
        message: string;
        duration?: number;
    }>,
}

export const createUISlice: StateCreator<RootStore, [], [], UISlice> = (
    set
) => ({

    ui: {
        ...baseState,
        setTheme: (theme: "light" | "dark" | "system") => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    theme,
                },
            }));
        },

        openModal: (modalId: string) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    modals: {
                        ...state.ui.modals,
                        [modalId]: true,
                    },
                }
            }))
        },

        closeModal: (modalId: string) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    modals: {
                        ...state.ui.modals,
                        [modalId]: false,
                    }
                }
            }))
        },

        toggleModal: (modalId: string) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    modals: {
                        ...state.ui.modals,
                        [modalId]: !state.ui.modals[modalId],
                    }
                }
            }))
        },

        openDrawer: (drawerId: string) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    drawers: {
                        ...state.ui.drawers,
                        [drawerId]: true,
                    },
                }
            }))
        },
        
        closeDrawer: (drawerId: string) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    drawers: {
                        ...state.ui.drawers,
                        [drawerId]: false,
                    },
                }
            }))
        },
        
        toggleDrawer: (drawerId: string) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    drawers: {
                        ...state.ui.drawers,
                        [drawerId]: !state.ui.drawers[drawerId],
                    },
                }
            }))
        },
        
        setGlobalLoading: (loading: boolean) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    globalLoading: loading,
                }
            }))
        },
        
        addNotification: (notification: Omit<UIState["notifications"][0], "id">) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    notifications: [
                        ...state.ui.notifications,
                        {
                            ...notification,
                            id: crypto.randomUUID(),
                        },
                    ],
                }
            }))
        },
        
        removeNotification: (id: string) => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    notifications: state.ui.notifications.filter(n => n.id !== id),
                }
            }))
        },
        
        clearNotifications: () => {
            set((state) => ({
                ui: {
                    ...state.ui,
                    notifications: [],
                }
            }))
        }
    }

})