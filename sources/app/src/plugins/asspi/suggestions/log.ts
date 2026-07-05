/*
 * Lightweight console logging for the AssPI suggestion pipeline, so an empty
 * result can be traced to the stage that dropped it (Jira returned nothing, no
 * project mapped for a key, a swallowed request error, …). Part of the throwaway
 * AssPI backend — a real backend would log server-side instead.
 */

const PREFIX = "[suggestions]";

export const suggestionLog = {
    info: (...args: unknown[]): void => console.info(PREFIX, ...args),
    warn: (...args: unknown[]): void => console.warn(PREFIX, ...args),
    error: (...args: unknown[]): void => console.error(PREFIX, ...args)
};
