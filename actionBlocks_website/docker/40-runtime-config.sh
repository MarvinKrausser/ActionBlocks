#!/bin/sh
# Writes the runtime configuration consumed by the website (see src/config/runtimeConfig.ts).
set -eu

# Escape backslashes and double quotes for the JS string literal.
server_url=$(printf '%s' "${SERVER_URL:-}" | sed 's/\\/\\\\/g; s/"/\\"/g')

cat > /usr/share/nginx/html/config.js <<CONFIG
window.__ACTIONBLOCKS_CONFIG__ = { serverUrl: "${server_url}" };
CONFIG
