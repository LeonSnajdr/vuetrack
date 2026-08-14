export type OAuthPopupResult = { status: "success"; code: string; state: string } | { status: "blocked" } | { status: "cancelled" } | { status: "error" };

type OAuthCallbackData = { source?: string; code?: string; state?: string; error?: string };

export function useOAuthPopup() {
    let activePopup: Window | null = null;
    let activeListener: ((event: MessageEvent) => void) | null = null;
    let closedTimer = 0;

    const cancel = () => {
        if (activeListener) {
            window.removeEventListener("message", activeListener);
            activeListener = null;
        }
        if (closedTimer) {
            window.clearInterval(closedTimer);
            closedTimer = 0;
        }
    };

    const open = (key: string, authorizationUrl: string, expectedState: string): Promise<OAuthPopupResult> => {
        return new Promise((resolve) => {
            const finish = (result: OAuthPopupResult) => {
                cancel();
                activePopup?.close();
                activePopup = null;
                resolve(result);
            };

            activePopup = window.open(authorizationUrl, `${key}-oauth`, "width=600,height=800");
            if (!activePopup) {
                resolve({ status: "blocked" });
                return;
            }

            activeListener = (event: MessageEvent) => {
                if (event.origin !== window.location.origin) return;

                const data = event.data as OAuthCallbackData;
                if (data?.source !== `${key}-oauth`) return;

                if (data.error || !data.code || !data.state || data.state !== expectedState) {
                    finish({ status: "error" });
                    return;
                }

                finish({ status: "success", code: data.code, state: data.state });
            };
            window.addEventListener("message", activeListener);

            closedTimer = window.setInterval(() => {
                if (activePopup?.closed) {
                    finish({ status: "cancelled" });
                }
            }, 500);
        });
    };

    return { open, cancel };
}
