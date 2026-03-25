import AuthPlugin, { type AuthOptions } from "@samhammer/authentication-vue";

export const setupAuth = async () => {
    const authOptions: AuthOptions = {
        authUrl: "https://auth-test.cloud.samhammer.de/auth",
        realm: "timetracking-dev",
        appClientId: "dev-app",
        keycloakInitOptions: {
            responseMode: "query"
        }
    };

    await AuthPlugin.initOnce(authOptions);
};
