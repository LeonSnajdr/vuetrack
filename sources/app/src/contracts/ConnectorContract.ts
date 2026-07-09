export type ConnectorDescriptorContract = {
    key: string;
    displayName: string;
    capabilities: number;
    configSchema: unknown[];
};

export type JiraAuthorizeResponse = {
    authorizationUrl: string;
    state: string;
};

export type JiraConnectRequest = {
    code: string;
    state: string;
    redirectUri: string;
};

export type ConnectResponse = {
    valid: boolean;
    errors: string[];
};

export type JiraStatusResponse = {
    connected: boolean;
    siteUrl: string | null;
};
