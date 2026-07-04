/*
 * AssPI Bridge — background service worker.
 *
 * Performs credentialed fetches to the legacy timetracking site. Because the
 * extension has host_permissions for that origin, the browser attaches your live
 * session cookies (HttpOnly included) automatically — no login or cookie paste.
 *
 * This is a pure transport: it returns the raw response text. All HTML parsing
 * happens back in the vuetrack page (a service worker has no DOMParser).
 *
 * The absolute target URL is chosen by the vuetrack page (dev vs prod) and passed
 * in each message; this worker just fetches it. Cookies are attached automatically
 * for any host listed in the manifest's host_permissions.
 */

chrome.runtime.onMessage.addListener((msg, _sender, sendResponse) => {
    if (!msg || msg.type !== "asspi-fetch") return;

    (async () => {
        try {
            const init = {
                method: msg.method || "GET",
                credentials: "include",
                headers: msg.headers || {}
            };
            if (msg.body != null) init.body = msg.body;

            const res = await fetch(msg.url, init);
            const text = await res.text();
            sendResponse({ ok: res.ok, status: res.status, text });
        } catch (e) {
            sendResponse({ ok: false, status: 0, text: "", error: String((e && e.message) || e) });
        }
    })();

    return true; // keep the message channel open for the async sendResponse
});
