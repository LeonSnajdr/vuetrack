/*
 * Jira suggestion provider — turns "issues you changed status on a day" into
 * suggestion candidates. Part of the throwaway AssPI fake backend.
 */

import { eachDayOfInterval, formatISO } from "date-fns";
import { getWorkedOnIssues } from "@/plugins/asspi/jira/JiraClient";
import { suggestionLog } from "@/plugins/asspi/suggestions/log";
import type { DateRange, SuggestionCandidate, SuggestionProvider } from "@/plugins/asspi/suggestions/types";

const isoDay = (d: Date): string => formatISO(d, { representation: "date" });

export const jiraProvider: SuggestionProvider = {
    id: "jira",

    async getCandidates(range: DateRange): Promise<SuggestionCandidate[]> {
        const days = eachDayOfInterval({ start: range.from, end: range.to });

        const perDay = await Promise.all(
            days.map(async (day) => {
                const iso = isoDay(day);
                try {
                    const issues = await getWorkedOnIssues(iso);
                    // One candidate per rolled-up story key per day.
                    const seen = new Set<string>();
                    const candidates: SuggestionCandidate[] = [];
                    for (const issue of issues) {
                        if (seen.has(issue.displayKey)) continue;
                        seen.add(issue.displayKey);
                        candidates.push({
                            source: "jira",
                            externalId: issue.displayKey,
                            date: iso,
                            taskId: issue.displayKey,
                            title: issue.displaySummary
                        });
                    }
                    suggestionLog.info(`jira ${iso}: ${issues.length} worked-on issue(s) → ${candidates.length} candidate(s)`);
                    return candidates;
                } catch (error) {
                    // A Jira/VPN hiccup for one day must not blank the whole week.
                    suggestionLog.warn(`jira request for ${iso} failed (Jira unreachable, not logged in, or missing host permission?):`, error);
                    return [];
                }
            })
        );

        return perDay.flat();
    }
};
