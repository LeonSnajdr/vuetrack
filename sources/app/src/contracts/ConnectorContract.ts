export type ConnectorDescriptorContract = {
    key: string;
    displayName: string;
    capabilities: string;
};

export type ConnectorAuthorizeResponse = {
    authorizationUrl: string;
    state: string;
};

export type ConnectorConnectRequest = {
    code: string;
    state: string;
    redirectUri: string;
};

export type ConnectorStatusResponse = {
    connected: boolean;
    healthy: boolean;
};
