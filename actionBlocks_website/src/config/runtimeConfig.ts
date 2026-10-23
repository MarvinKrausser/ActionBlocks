interface RuntimeConfig {
  serverUrl?: string;
}

declare global {
  interface Window {
    __ACTIONBLOCKS_CONFIG__?: RuntimeConfig;
  }
}

/** URL of the game hub. Priority: runtime config.js, then VITE_SERVER_URL, then "<origin>/hub". */
export function getServerUrl(): string {
  const runtime = window.__ACTIONBLOCKS_CONFIG__?.serverUrl?.trim();
  if (runtime) return runtime;
  const buildTime = (import.meta.env.VITE_SERVER_URL as string | undefined)?.trim();
  if (buildTime) return buildTime;
  return `${window.location.origin}/hub`;
}
