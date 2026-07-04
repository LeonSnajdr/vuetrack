/*
 * AssPI Bridge — content script.
 *
 * Relays fetch requests between the vuetrack page (via window.postMessage) and the
 * background service worker (via chrome.runtime messaging). The page never needs
 * to know the extension id.
 *
 * Protocol (all messages carry `__asspi: true`):
 *   page -> here:  { dir: "req", id, url, method, headers, body }
 *   here -> page:  { dir: "res", id, ok, status, text, error }
 *   here -> page:  { dir: "ready" }   (announced once on load)
 */

window.addEventListener("message", (event) => {
    if (event.source !== window) return;
    const data = event.data;
    if (!data || data.__asspi !== true || data.dir !== "req") return;

    chrome.runtime.sendMessage(
        { type: "asspi-fetch", url: data.url, method: data.method, headers: data.headers, body: data.body },
        (response) => {
            const lastError = chrome.runtime.lastError;
            window.postMessage(
                {
                    __asspi: true,
                    dir: "res",
                    id: data.id,
                    ok: response ? response.ok : false,
                    status: response ? response.status : 0,
                    text: response ? response.text : "",
                    error: lastError ? lastError.message : response && response.error
                },
                "*"
            );
        }
    );
});

// Let the page know the bridge is available.
window.postMessage({ __asspi: true, dir: "ready" }, "*");
