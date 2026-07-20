export enum ConnectorCapability {
    Worklogs = "worklogs",
    IssueActivity = "issueActivity",
    OAuth = "oAuth"
}

export type ConnectorDescriptorContract = {
    key: string;
    capabilities: ConnectorCapability[];
    connected: boolean;
    healthy: boolean;
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
