/*
 * Custom axios adapter for AssPI. Replaces the network layer entirely: it matches
 * the request's method + URL against a small route table and fulfils each one by
 * scraping the legacy site (via the handlers), then returns a synthetic
 * AxiosResponse. To the services/stores/components this is indistinguishable from
 * a real REST backend — no other code changes.
 */

import axios, { type AxiosAdapter, type AxiosResponse, type InternalAxiosRequestConfig } from "axios";
import { AssPiHttpError } from "@/plugins/asspi/AssPiClient";
import { deleteTimeEntry, findProjectIdByTaskId, loadActivities, loadProjects, loadTimeEntries, upsertTimeEntry, type TimeEntryDTO } from "@/plugins/asspi/AssPiHandlers";
import {
    acceptSuggestion,
    dismissSuggestion,
    loadIssueDetails,
    loadSuggestions,
    recommendAgain,
    updateSuggestion,
    type TimeEntrySuggestionUpdateDTO
} from "@/plugins/asspi/AssPiSuggestionHandlers";

// Reduce a request URL to a baseURL-relative path without query string.
function normalizePath(config: InternalAxiosRequestConfig): string {
    let url = config.url ?? "";
    const base = config.baseURL ?? "";
    if (base && url.startsWith(base)) url = url.slice(base.length);
    url = url.split("?")[0];
    return url.replace(/^\/+/, "").replace(/\/+$/, "");
}

// axios runs transformRequest before the adapter, so object bodies arrive as JSON strings.
function parseData<T>(data: unknown): T {
    if (typeof data === "string") return JSON.parse(data) as T;
    return data as T;
}

function ok(data: unknown, config: InternalAxiosRequestConfig): AxiosResponse {
    return { data, status: 200, statusText: "OK", headers: {}, config };
}

async function route(config: InternalAxiosRequestConfig): Promise<AxiosResponse> {
    const method = (config.method ?? "get").toLowerCase();
    const path = normalizePath(config);
    const params = (config.params ?? {}) as Record<string, string>;

    // Connectors are the real backend replacement for AssPI — never fake them; hit the network.
    if (path.startsWith("connectors")) {
        return axios.getAdapter("xhr")(config);
    }

    if (method === "get" && path === "timeEntry") {
        return ok(await loadTimeEntries(params.from, params.to), config);
    }
    if (method === "post" && path === "timeEntry/upsert") {
        await upsertTimeEntry(parseData<TimeEntryDTO>(config.data));
        return ok(undefined, config);
    }
    if (method === "delete" && path === "timeEntry") {
        const body = parseData<{ idsToDelete: string }>(config.data);
        await deleteTimeEntry(body.idsToDelete);
        return ok(undefined, config);
    }
    if (method === "get" && path === "project") {
        return ok(await loadProjects(), config);
    }
    if (method === "get" && path === "project/findByTaskId") {
        return ok(await findProjectIdByTaskId(String(params.taskId ?? "")), config);
    }
    const activityMatch = /^project\/([^/]+)\/activity$/.exec(path);
    if (method === "get" && activityMatch) {
        return ok(await loadActivities(activityMatch[1]), config);
    }

    // Suggestions: RESTful fake api backed by Jira + IndexedDB (all in plugins/asspi).
    if (method === "get" && path === "timeEntrySuggestions") {
        return ok(await loadSuggestions(String(params.from ?? ""), String(params.to ?? "")), config);
    }
    if (method === "post" && path === "timeEntrySuggestions/recommendAgain") {
        await recommendAgain();
        return ok(undefined, config);
    }
    const suggestionAcceptMatch = /^timeEntrySuggestions\/(\d+)\/accept$/.exec(path);
    if (method === "post" && suggestionAcceptMatch) {
        await acceptSuggestion(Number(suggestionAcceptMatch[1]));
        return ok(undefined, config);
    }
    const suggestionIdMatch = /^timeEntrySuggestions\/(\d+)$/.exec(path);
    if (method === "put" && suggestionIdMatch) {
        return ok(await updateSuggestion(Number(suggestionIdMatch[1]), parseData<TimeEntrySuggestionUpdateDTO>(config.data)), config);
    }
    if (method === "delete" && suggestionIdMatch) {
        await dismissSuggestion(Number(suggestionIdMatch[1]));
        return ok(undefined, config);
    }
    if (method === "get" && path === "issueDetails") {
        return ok(await loadIssueDetails(String(params.taskId ?? "")), config);
    }

    throw new Error(`AssPI: no handler for ${method.toUpperCase()} ${path}`);
}

export const assPiAdapter: AxiosAdapter = async (config) => {
    try {
        return await route(config);
    } catch (error) {
        // Surface legacy HTTP failures as real AxiosErrors so the services'
        // validation/error handling (which inspects error.response) still works.
        if (error instanceof AssPiHttpError) {
            const response: AxiosResponse = { data: null, status: error.status, statusText: "Error", headers: {}, config };
            throw new axios.AxiosError(error.message, String(error.status), config, undefined, response);
        }
        throw new axios.AxiosError(error instanceof Error ? error.message : String(error), "ERR_ASSPI", config);
    }
};

export default assPiAdapter;
