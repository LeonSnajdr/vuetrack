export type WebAuthOptions = {
    authUrl: string;
    realm: string;
    appClientId: string;
    apiClientId: string;
};

export type ConfigContract = {
    authOptions: WebAuthOptions;
};
