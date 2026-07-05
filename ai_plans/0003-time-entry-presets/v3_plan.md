## Rationale

Replace the single `preselectedTaskId` sidebar shortcut with a fuller preset workflow that lets users maintain named time-entry templates and apply one optional preset when creating new entries from tracking flows. The change should stay localized to the UI app, preserve the existing "default values win" behavior in `useTimeEntryHelper`, and keep the sidebar usable for both selecting and maintaining presets while storing the preset state behind a dedicated store abstraction.

## Acceptance Criteria

- [ ] The old `preselectedTaskId` state and sidebar text field are removed and replaced by a time-entry preset workflow in the tracking UI.
- [ ] A `TimeEntryPreset` model is introduced under the app `src/models` area with a required `name` and optional `taskId`, `duration`, `projectId`, and `activityId`.
- [ ] A new preset store owns the preset collection in an in-memory `ref`, tracks the selected preset, and exposes the active preset to consumers.
- [ ] Users can create and edit presets from the sidebar, with the duration field rendered via `VNumberInput`.
- [ ] The sidebar renders presets as a single-select chip group that allows selecting one preset at a time and clearing the current selection.
- [ ] `useTimeEntryHelper` reads the active preset from the preset store and applies it as fallback defaults for new entries, while any values passed in `defaultValues` still take precedence over the preset.
- [ ] When a selected preset provides a duration, the helper derives `endTime` by adding that duration to the resolved `startTime`.
- [ ] All affected translations and automated tests are updated to reflect the new preset workflow.

## Technical Details

Introduce a dedicated `TimeEntryPreset` model under [src/models](C:\Repos\vuetrack\sources\app\src\models). The model should include an internal id for list rendering/editing, a required `name`, and nullable `taskId`, `durationMinutes`, `projectId`, and `activityId`.

Add a dedicated preset store, separate from `trackingStore`, to hold this feature’s state. The store should keep the preset list in an in-memory `ref` for now, along with the selected preset id, and expose a derived `activePreset` so callers do not have to repeat lookup logic. It should also provide explicit actions for creating, updating, and toggling selection on and off. This keeps persistence concerns out of the initial implementation while giving the UI and helper a stable API that can later be backed by local storage or a service without reshaping consumers.

Rework [Sidebar.vue](C:\Repos\vuetrack\sources\app\src\components\tracking\Sidebar.vue) from a single text field into a small preset management surface that consumes the new preset store. Reuse the existing time-entry field components and project/activity loading patterns where practical so project and activity selection behaves consistently with the rest of the app. The sidebar should present the stored presets as `VChip` items inside a `VChipGroup` configured for single selection, with an explicit way to deselect the active chip because the requirement is radio-style selection plus manual clearing. The preset editor can stay inline or use a lightweight dialog/menu section, but it should use `VNumberInput` for the duration and keep edit state separate from the currently selected preset so selection does not accidentally mutate data.

Update [useTimeEntryHelper.ts](C:\Repos\vuetrack\sources\app\src\composables\timeEntry\useTimeEntryHelper.ts) to consume `activePreset` from the new preset store and merge values in precedence order: `defaultValues` first, active preset second, existing helper fallbacks last. For scalar fields this means `taskId`, `projectId`, and `activityId` fall back to preset values only when the caller did not provide them. For time fields, continue deriving `startTime` from `defaultValues.startTime`, then newest entry end time, then the 8:00 fallback; because the resolved start time always exists, if the active preset contains `durationMinutes`, compute `endTime` by adding the duration on top of that resolved start time whenever the caller did not already provide `defaultValues.endTime`.

Because the sidebar strings currently only cover `preselectedTaskId`, update both translation files under `src/translations` with flat, synchronized keys for the preset list, form labels, empty states, and selection-clearing affordances. Add or update unit/component coverage around the new preset store and helper precedence rules, and add a sidebar-focused test that verifies creating/editing presets, numeric duration input, and toggling chip selection if there is already test infrastructure for that component area.
