export enum IntegrationKey {
    Jira = "jira",
    Github = "github",
    Timetracking = "timetracking"
}

export type IntegrationContract = {
    key: IntegrationKey;
    connected: boolean;
    healthy: boolean;
};

export type IntegrationAuthorizeResponse = {
    authorizationUrl: string;
    state: string;
};

export type IntegrationConnectRequest = {
    code: string;
    state: string;
    redirectUri: string;
};

export type IntegrationStatusResponse = {
    connected: boolean;
    healthy: boolean;
};
