# AGENTS.md instructions for C:\Repos\vuetrack

<INSTRUCTIONS>
# Root Agents.md

Vuetrack is a modern UI for a time tracking application written in Vue.js.

## Codebase Overview

This repository contains two separate projects:

1. **UI**
2. **Fake API** — a mock backend used for features that are not yet implemented in the real time-tracking backend, which lives in a different repository.

## Project Rules

Before making changes, read the project-specific `AGENTS.md` file for the area you are working on:

- **UI:** `./sources/app/AGENTS.md`
- **Fake API:** `./sources/fake-api/AGENTS.md`

## Plan Rules

Read `./ai_plans/AGENTS.md` for details on how to write plans.

## Implementation rules

Plans typically have acceptance criteria with check boxes. Check each box when you are finished with the corresponding criterion.

## Here is Your Space

If you encounter something worth noting while you are working on this code base, write it down here in this section. Once you are finished, I will discuss it with you, and we can decide where to put your notes.

</INSTRUCTIONS><environment_context>
  <cwd>C:\Repos\vuetrack</cwd>
  <shell>powershell</shell>
  <current_date>2026-04-20</current_date>
  <timezone>Europe/Berlin</timezone>
</environment_context>

Please write a plan for the following feature.

Implement a timeEntry preset feature. 
- It should be located in the [Sidebar.vue](sources/app/src/components/tracking/Sidebar.vue) 
- The old preSelectedTaskId feature is removed and replaced with this new feature
- The user has the ability to create/edit a preset that has the following fields
  - Name (name of the preset)
  - TaskId 
  - Duration (in minutes)
  - Project
  - Activity
- each of these fields is optional except the name
- The sidebar should render these presets as a chip group (radio style) the user can select only one preset and also deselect it
- If a preset is selected the [useTimeEntryHelper.ts](sources/app/src/composables/timeEntry/useTimeEntryHelper.ts) uses them to crate a new timeEntry (but passed defaultValues are preffered)
