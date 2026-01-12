import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "path";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ["react", "react-dom", "react-router-dom"],
          ui: [
            "@radix-ui/react-dialog",
            "@radix-ui/react-select",
            "@radix-ui/react-dropdown-menu",
            "@radix-ui/react-popover",
            "@radix-ui/react-tabs",
            "@radix-ui/react-toast",
            "@radix-ui/react-tooltip",
            "@radix-ui/react-slider",
            "@radix-ui/react-checkbox",
            "@radix-ui/react-alert-dialog",
          ],
          query: ["@tanstack/react-query", "@tanstack/react-query-devtools"],
          forms: ["react-hook-form", "@hookform/resolvers", "zod"],
          utils: [
            "axios",
            "jwt-decode",
            "clsx",
            "class-variance-authority",
            "tailwind-merge",
            "lucide-react",
          ],
        },
      },
    },
    chunkSizeWarningLimit: 1000,
  },
});
