export type BackendDescriptorContract = {
    key: string;
    displayName: string;
    capabilities: string;
};

export type TimetrackingAuthorizeResponse = {
    authorizationUrl: string;
    state: string;
};

export type TimetrackingConnectRequest = {
    code: string;
    state: string;
    redirectUri: string;
};

export type TimetrackingConnectResponse = {
    connected: boolean;
};

export type TimetrackingStatusResponse = {
    connected: boolean;
};
