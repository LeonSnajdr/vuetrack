# Rationale

Use JQL as a bounded candidate-selection mechanism, then turn Jira worklogs and field-level changelog records into typed, attributable evidence. This avoids treating a generic `updated` timestamp as work while retaining the detail needed to corroborate signals from Teams and GitHub.

# Acceptance criteria

- [ ] The Jira connector uses a time-bounded, project-scoped JQL query to obtain candidate work items and paginates all results.
- [ ] For the candidate set, the connector retrieves paginated changelogs in bulk, limited to the relevant field IDs where supported, and filters events to changes authored by the connected Jira user.
- [ ] The connector emits typed signals for Jira worklogs, issue creation, comments, status transitions, assignee changes, priority changes, sprint changes, parent/epic changes, and selected content edits when their author and timestamp are available.
- [ ] Every Jira signal includes the work-item ID/key, project, work type, source URL, event author, event timestamp, event type, and before/after values where applicable as metadata.
- [ ] The engine assigns explicit worklogs the highest weight; treats status transitions, comments, and authored content edits as bounded corroborating events; and gives planning/administrative events low weight or filters them by default.
- [ ] The connector never infers a long duration from a Jira lifecycle change alone, and tests cover author filtering, pagination, field filtering, and event-to-signal mapping.

# Technical details

Search with JQL for a deliberately small candidate set, scoped to configured projects and the requested time window, for example `project in (...) AND updated >= <from> AND updated < <to> ORDER BY updated ASC`. JQL supports historical predicates such as `CHANGED`, including optional `BY`, `FROM`, `TO`, and time predicates for fields including status, assignee, priority, resolution, reporter, and fix version. Use these predicates only to reduce the candidate set; do not rely on them as the event feed because the complete per-change timestamp, author, and old/new values are read from issue changelogs.

After JQL returns issue IDs/keys, call the Jira Cloud bulk-changelog endpoint in pages. It supports up to 1,000 issues per request and filtering by up to 10 field IDs, and returns entries in chronological order. Request the fields that matter first: `status`, `assignee`, `priority`, `resolution`, `Sprint`/its custom-field ID, parent/epic, summary, description, and selected organisation-specific fields. Fetch comments and worklogs through their dedicated endpoints if their information is not present in the changelog response. Preserve the Jira changelog ID and item index in `ExternalId` so re-fetches deduplicate deterministically.

Map high-value evidence as follows: worklog is an explicit range; a comment created by the connected user, a status transition by that user, or a substantive summary/description edit by that user is a point event eligible for a short bounded window; issue creation by the user is similar but weaker. Assignee, priority, sprint, parent/epic, resolution, and issue move events are context signals. They enrich the metadata and may corroborate an adjacent activity block, but should not independently generate a time suggestion. A generic issue `updated` event is not a signal because it loses field and author intent.

Standard Jira metadata should include `jira.issue.id`, `jira.issue.key`, `jira.project.key`, `jira.issue.type`, `display.title`, `source.url`, `activity.type`, `activity.author.id`, `activity.at`, `field.id`, `field.name`, `field.before.id`, `field.before.display`, `field.after.id`, and `changelog.id`. Add the current status, assignee, sprint, parent, labels, components, and linked-development references as context only. The work-item key is the default correlation key with external sources; it must not be treated as proof that the entire day was spent on the work item.

Run JQL queries incrementally with an overlap window, retain a cursor/high-water mark, and deduplicate by Jira event identity. Restrict projects before broad date predicates for performance. JQL `updatedBy` can select work items a user updated but has day-level granularity, so use changelog author filtering for the actual signal timestamp and author identity.
