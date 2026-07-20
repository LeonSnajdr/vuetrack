export enum BackendCapability {
    TimeEntries = "timeEntries",
    Projects = "projects",
    OAuth = "oAuth"
}

export type BackendDescriptorContract = {
    key: string;
    capabilities: BackendCapability[];
    connected: boolean;
    healthy: boolean;
};

export type BackendAuthorizeResponse = {
    authorizationUrl: string;
    state: string;
};

export type BackendConnectRequest = {
    code: string;
    state: string;
    redirectUri: string;
};

export type BackendStatusResponse = {
    connected: boolean;
    healthy: boolean;
};
