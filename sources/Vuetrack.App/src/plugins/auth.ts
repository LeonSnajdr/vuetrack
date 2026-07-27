import AuthPlugin, { type AuthOptions } from "@samhammer/authentication-vue";
import type { WebAuthOptions } from "@/contracts/ConfigContract";

export const setupAuth = async (webAuthOptions: WebAuthOptions) => {
    const authOptions: AuthOptions = {
        authUrl: webAuthOptions.authUrl,
        realm: webAuthOptions.realm,
        appClientId: webAuthOptions.appClientId,
        apiClientId: webAuthOptions.apiClientId,
        keycloakInitOptions: {
            responseMode: "query"
        }
    };

    await AuthPlugin.initOnce(authOptions);
};
