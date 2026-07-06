/*
 * Jira REST access for the AssPI fake backend.
 *
 * Ported from the Tampermonkey userscript's JiraService, trimmed to the two calls
 * the suggestion feature needs. All requests go through the AssPI Bridge extension
 * (JIRA_BASE is in its host_permissions), so they reuse the live Atlassian session
 * — no token handling here.
 *
 * This whole folder is throwaway: when a real backend exists it queries Jira
 * server-side and this client is deleted.
 */

import client, { JIRA_BASE } from "@/plugins/asspi/AssPiClient";

// Issue the current user changed status on a given day, rolled up to its parent
// story where applicable (mirrors the userscript's getWorkedOnIssues).
export type WorkedOnIssue = {
    key: string;
    summary: string;
    parentSummary: string;
    status: string;
    // The key to suggest: parent story key for tasks/subtasks, else the issue key.
    displayKey: string;
    // Summary to show for the suggested key (parent's summary when rolled up).
    displaySummary: string;
};

export type IssueDetails = {
    summary: string;
    type: string;
    status: string;
};

type JiraIssue = {
    key?: string;
    fields?: {
        summary?: string;
        issuetype?: { name?: string };
        status?: { name?: string };
        parent?: { key?: string; fields?: { summary?: string } };
    };
};

type JiraSearchResult = { issues?: JiraIssue[] };

const TASK_TYPES = new Set(["task", "subtask", "sub-task"]);

// Small LRU cache for tooltip lookups; keeps a long session from refetching.
const detailsCache = new Map<string, IssueDetails>();
const DETAILS_CACHE_MAX = 200;

function isTaskType(name: string): boolean {
    return TASK_TYPES.has(name.toLowerCase());
}

export async function getWorkedOnIssues(isoDate: string): Promise<WorkedOnIssue[]> {
    const jql = `status changed by currentUser() on "${isoDate}" ORDER BY updated DESC`;
    const path = `/rest/api/3/search/jql?jql=${encodeURIComponent(jql)}&maxResults=20&fields=summary,issuetype,status,parent`;

    const data = await client.getJsonFrom<JiraSearchResult>(JIRA_BASE, path);

    const seen = new Set<string>();
    const result: WorkedOnIssue[] = [];
    for (const issue of data.issues ?? []) {
        const key = issue.key ?? "";
        if (!key || seen.has(key)) continue;
        seen.add(key);

        const typeName = issue.fields?.issuetype?.name ?? "";
        const parentKey = issue.fields?.parent?.key ?? "";
        const parentSummary = issue.fields?.parent?.fields?.summary ?? "";
        const summary = issue.fields?.summary ?? "";
        const rolledUp = isTaskType(typeName) && !!parentKey;

        result.push({
            key,
            summary,
            parentSummary,
            status: issue.fields?.status?.name ?? "",
            displayKey: rolledUp ? parentKey : key,
            displaySummary: rolledUp ? parentSummary : summary
        });
    }
    return result;
}

export async function getIssueDetails(taskId: string): Promise<IssueDetails | null> {
    if (!taskId) return null;

    const cacheKey = taskId.toLowerCase();
    const cached = detailsCache.get(cacheKey);
    if (cached) return cached;

    const path = `/rest/api/3/issue/${encodeURIComponent(taskId)}?fields=summary,issuetype,status`;
    const issue = await client.getJsonFrom<JiraIssue>(JIRA_BASE, path);

    const details: IssueDetails = {
        summary: issue.fields?.summary ?? "",
        type: issue.fields?.issuetype?.name ?? "",
        status: issue.fields?.status?.name ?? ""
    };

    if (detailsCache.size >= DETAILS_CACHE_MAX) {
        const oldest = detailsCache.keys().next().value;
        if (oldest !== undefined) detailsCache.delete(oldest);
    }
    detailsCache.set(cacheKey, details);
    return details;
}
