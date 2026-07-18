import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 55173,
    // Bind scope is opt-in. Default is loopback only (localhost). To expose the dev server set
    // VITE_HOST at launch:
    //   VITE_HOST=0.0.0.0            → all interfaces (Tailscale + LAN)
    //   VITE_HOST=100.100.187.39     → this host's Tailscale IP ONLY (tailnet-only, no LAN)
    // Reachable either way at http://beeglab.tailc7c3a.ts.net:55173 from any tailnet device.
    host: process.env.VITE_HOST ?? "localhost",
    // Vite rejects requests whose Host header isn't a localhost/IP literal, so allow this
    // tailnet's MagicDNS domain (the leading dot covers beeglab.tailc7c3a.ts.net and siblings)
    // for access by hostname. Raw-IP access (100.x:55173) needs no entry here.
    allowedHosts: [".tailc7c3a.ts.net"],
  },
});
