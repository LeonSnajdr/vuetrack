# AssPI Bridge (browser extension)

A tiny MV3 extension that lets the vuetrack dev app talk to the legacy
timetracking site (`https://timetracking.cloud.samhammer.de`) **using your own
logged-in browser session** — no proxy, no cookie pasting, no login step.

## How it works

- vuetrack's AssPI layer sends fetch requests to the page via `window.postMessage`.
- The extension's **content script** (injected into `localhost:9100`) relays them to
  the **background service worker**.
- The background worker does `fetch(..., { credentials: "include" })` against the
  legacy site. Because the extension has `host_permissions` for that origin, the
  browser attaches your session cookies automatically (HttpOnly included).
- The raw response text goes back to vuetrack, which parses the HTML itself.

The extension is a pure transport — all parsing stays in vuetrack.

## Install (unpacked)

1. Open `chrome://extensions` (or `edge://extensions`).
2. Enable **Developer mode**.
3. Click **Load unpacked** and select this folder (`sources/asspi-extension`).
4. In `sources/app/.env`, ensure `VITE_USE_ASSPI=true`.
5. Make sure you're on VPN and logged into `https://timetracking.cloud.samhammer.de`
   in the same browser.
6. Run `yarn dev` in `sources/app` and open `http://localhost:9100/`.

## Notes

- **Different dev port / deploy origin?** Update the `matches` list in
  `manifest.json` (currently `http://localhost:9100/*` and `127.0.0.1`).
- **Session:** you just need to be logged into the legacy site somewhere in this
  browser; the extension does not need a timetracking tab open.
- **If POST/writes fail with 403:** the legacy server may validate the `Origin`/
  `Referer` header on writes. If so, add a `declarativeNetRequest` rule to set those
  headers to the legacy base for requests to that host (ask and this can be added).
