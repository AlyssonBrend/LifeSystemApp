import path from "path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["icone-192.png", "icone-512.png"],
      manifest: {
        name: "Life System — RPG da vida real",
        short_name: "Life System",
        description: "Hábitos viram XP; vícios viram chefes. Derrote seus limites.",
        lang: "pt-BR",
        display: "standalone",
        orientation: "portrait",
        start_url: "/",
        background_color: "#0a0d13",
        theme_color: "#0a0d13",
        icons: [
          { src: "icone-192.png", sizes: "192x192", type: "image/png" },
          { src: "icone-512.png", sizes: "512x512", type: "image/png" },
          { src: "icone-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" },
        ],
      },
      workbox: {
        // O app shell funciona offline; as chamadas /api sempre vão à rede (estado vive no servidor)
        navigateFallbackDenylist: [/^\/api/],
        runtimeCaching: [
          {
            urlPattern: /^https:\/\/fonts\.(googleapis|gstatic)\.com\/.*/,
            handler: "CacheFirst",
            options: { cacheName: "fontes", expiration: { maxEntries: 20, maxAgeSeconds: 60 * 60 * 24 * 365 } },
          },
        ],
      },
    }),
  ],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    host: true, // acessível pelo celular na rede local
    port: 5173,
    proxy: {
      "/api": { target: "http://localhost:5090", changeOrigin: true },
    },
  },
});
