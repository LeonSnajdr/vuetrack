## Rationale

Replace the single `preselectedTaskId` sidebar shortcut with a fuller preset workflow that lets users maintain named time-entry templates and apply one optional preset when creating new entries from tracking flows. The change should stay localized to the UI app, preserve the existing "default values win" behavior in `useTimeEntryHelper`, and keep the sidebar usable for both selecting and maintaining presets.

## Acceptance Criteria

- [ ] The old `preselectedTaskId` state and sidebar text field are removed and replaced by a time-entry preset model in the tracking UI.
- [ ] A `TimeEntryPreset` model is introduced under the app `src/models` area with a required `name` and optional `taskId`, `duration`, `projectId`, and `activityId`.
- [ ] Users can create and edit presets from the sidebar, with the duration field rendered via `VNumberInput`.
- [ ] The sidebar renders presets as a single-select chip group that allows selecting one preset at a time and clearing the current selection.
- [ ] `useTimeEntryHelper` applies the selected preset as fallback defaults for new entries, while any values passed in `defaultValues` still take precedence over the preset.
- [ ] When a selected preset provides a duration, the helper derives `endTime` by adding that duration to the resolved `startTime`.
- [ ] All affected translations and automated tests are updated to reflect the new preset workflow.

## Technical Details

Introduce a dedicated `TimeEntryPreset` model under [src/models](C:\Repos\vuetrack\sources\app\src\models) instead of overloading `TimeEntryCreateContract` or keeping the shape store-local. The model should include an internal id for list rendering/editing, a required `name`, and nullable `taskId`, `durationMinutes`, `projectId`, and `activityId`. Replace the current `preselectedTaskId` ref in `trackingStore` with reactive preset state that covers the preset collection plus the currently selected preset id, and expose explicit actions for create, update, and select-or-clear behavior.

Rework [Sidebar.vue](C:\Repos\vuetrack\sources\app\src\components\tracking\Sidebar.vue) from a single text field into a small preset management surface. Reuse the existing time-entry field components and project/activity loading patterns where practical so project and activity selection behaves consistently with the rest of the app. The sidebar should present the stored presets as `VChip` items inside a `VChipGroup` configured for single selection, with an explicit way to deselect the active chip because the requirement is radio-style selection plus manual clearing. The preset editor can stay inline or use a lightweight dialog/menu section, but it should use `VNumberInput` for the duration and keep edit state separate from the currently selected preset so selection does not accidentally mutate data.

Update [useTimeEntryHelper.ts](C:\Repos\vuetrack\sources\app\src\composables\timeEntry\useTimeEntryHelper.ts) to read the selected preset from the tracking store and merge values in precedence order: `defaultValues` first, selected preset second, existing helper fallbacks last. For scalar fields this means `taskId`, `projectId`, and `activityId` fall back to preset values only when the caller did not provide them. For time fields, continue deriving `startTime` from `defaultValues.startTime`, then newest entry end time, then the 8:00 fallback; because the resolved start time always exists, if the preset contains `durationMinutes`, compute `endTime` by adding the duration on top of that resolved start time whenever the caller did not already provide `defaultValues.endTime`.

Because the sidebar strings currently only cover `preselectedTaskId`, update both translation files under `src/translations` with flat, synchronized keys for the preset list, form labels, empty states, and selection-clearing affordances. Add or update unit/component coverage around the tracking store and helper precedence rules, and add a sidebar-focused test that verifies creating/editing presets, numeric duration input, and toggling chip selection if there is already test infrastructure for that component area.
