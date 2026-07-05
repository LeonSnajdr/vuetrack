/*
 * Low-level HTTP for AssPI.
 *
 * All requests reach the legacy timetracking site
 * (`https://timetracking.cloud.samhammer.de`) through the AssPI Bridge browser
 * extension, which fetches with your live logged-in session (HttpOnly cookies
 * included) — no proxy, no VPN-through-localhost, no login step.
 *
 * The extension is a pure transport: it returns raw text/JSON; all HTML parsing
 * happens in HtmlParser. Communication is via window.postMessage; see
 * sources/asspi-extension/.
 */

// Target host the bridge fetches from: in a production build the real legacy site;
// otherwise the origin of VITE_API_BASE_URL (the part before "/api"). Override with
// VITE_ASSPI_BASE.
const devBase: string = (import.meta.env.VITE_API_BASE_URL || "").split("/api")[0];
const TARGET_BASE: string = import.meta.env.VITE_ASSPI_BASE || (import.meta.env.PROD ? "https://timetracking.cloud.samhammer.de" : devBase);

// Jira is fetched through the same bridge (its host is in the extension's
// host_permissions), so credentialed requests reuse the live Atlassian session.
export const JIRA_BASE: string = import.meta.env.VITE_JIRA_BASE || "https://samhammer.atlassian.net";

// How long to wait for the extension bridge before assuming it isn't installed.
const BRIDGE_TIMEOUT_MS = 30000;

export class AssPiHttpError extends Error {
    public readonly status: number;

    public constructor(status: number, message?: string) {
        super(message ?? `Legacy request failed (HTTP ${status})`);
        this.name = "AssPiHttpError";
        this.status = status;
    }
}

export type PageResult = {
    ok: boolean;
    status: number;
    text: string;
};

type RequestInitLite = {
    method: string;
    headers: Record<string, string>;
    body?: string;
};

// Round-trip a request through the AssPI Bridge extension via window.postMessage.
// `base` defaults to the legacy timetracking site; pass JIRA_BASE for Jira calls.
function request(path: string, init: RequestInitLite, base: string = TARGET_BASE): Promise<PageResult> {
    return new Promise((resolve, reject) => {
        const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`;

        const timeout = setTimeout(() => {
            cleanup();
            reject(new AssPiHttpError(0, "AssPI bridge did not respond — is the AssPI Bridge extension installed and enabled?"));
        }, BRIDGE_TIMEOUT_MS);

        const onMessage = (event: MessageEvent) => {
            if (event.source !== window) return;
            const d = event.data;
            if (!d || d.__asspi !== true || d.dir !== "res" || d.id !== id) return;
            cleanup();
            if (d.error) reject(new AssPiHttpError(d.status || 0, d.error));
            else resolve({ ok: d.ok, status: d.status, text: d.text });
        };

        const cleanup = () => {
            clearTimeout(timeout);
            window.removeEventListener("message", onMessage);
        };

        window.addEventListener("message", onMessage);
        window.postMessage({ __asspi: true, dir: "req", id, url: base + path, method: init.method, headers: init.headers, body: init.body }, "*");
    });
}

export class AssPiClient {
    // GET a JSON endpoint (e.g. /tracking/activities).
    public async getJson<T>(path: string): Promise<T> {
        const r = await request(path, { method: "GET", headers: { "X-Requested-With": "XMLHttpRequest", Accept: "application/json" } });
        if (!r.ok) throw new AssPiHttpError(r.status);
        return JSON.parse(r.text) as T;
    }

    // GET a JSON endpoint from an arbitrary base host (e.g. Jira at JIRA_BASE).
    public async getJsonFrom<T>(base: string, path: string): Promise<T> {
        const r = await request(path, { method: "GET", headers: { Accept: "application/json" } }, base);
        if (!r.ok) throw new AssPiHttpError(r.status);
        return JSON.parse(r.text) as T;
    }

    // GET an HTML page (e.g. /tracking/show or the entry list) as text.
    public getPage(path: string): Promise<PageResult> {
        return request(path, { method: "GET", headers: { Accept: "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8" } });
    }

    // POST an url-encoded form (e.g. /tracking/submit, /tracking/delete/confirm).
    public postForm(path: string, params: Record<string, string>): Promise<PageResult> {
        return request(path, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8", "X-Requested-With": "XMLHttpRequest" },
            body: new URLSearchParams(params).toString()
        });
    }
}

export default new AssPiClient();
