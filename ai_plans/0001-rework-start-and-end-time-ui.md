## Rationale

Rework the time-entry time editing UI so the default flow is faster for the common case: the user chooses a start date/time once and then selects a duration from predefined minute options, with the end time derived automatically. Keep a granular mode available for cases where users need to edit both timestamps directly. This should reduce friction in list and edit overlays without changing the underlying start/end time data model.

## Acceptance Criteria

- [ ] The default time-entry UI shows a one-line time section that lets the user set `startTime` and choose a predefined duration in minutes, and it derives `endTime` automatically from that selection.
- [ ] The default mode preselects a reasonable duration option when an entry already has both `startTime` and `endTime` and their difference matches one of the supported presets.
- [ ] The default mode keeps the existing behavior that a newly created list entry pre-fills `startTime` from the latest entry end time or other existing defaults.
- [ ] A granular mode is available that exposes the current explicit `startTime` and `endTime` inputs unchanged in behavior.
- [ ] Switching between default mode and granular mode keeps the time entry valid and preserves the current timestamps instead of resetting user input.
- [ ] Validation still prevents invalid time ranges, and saving continues to submit `startTime` and `endTime` through the existing contracts without backend changes.
- [ ] The new UI is applied consistently in the time-entry overlays that currently use `TimeEntryFieldContainer`, except flows that intentionally hide time fields.
- [ ] New labels and option texts are added to both English and German translations.

## Technical Details

Keep `TimeEntryCreateContract` and `TimeEntryUpdateContract` unchanged. The rework should stay in the UI layer and continue to submit concrete `startTime` and `endTime` values to the existing stores and services.

Refactor `TimeEntryFieldContainer` so the time section becomes mode-aware instead of always rendering separate start-time and end-time rows. The cleanest approach is to introduce a dedicated time-range field component that owns:

- the mode switch between default and granular mode
- the quick-entry row for `startTime + duration`
- the existing granular inputs as a fallback view

In default mode, bind `startTime` directly to the existing `BaseDateTimeInput` and derive `endTime` from a selected duration preset. Store the selected preset as local UI state in the component, not in the contracts. Compute duration from `startTime` and `endTime` so edit flows can infer the correct preset when possible. If the current duration does not match a preset, the component should still remain stable when entering granular mode and back again; the implementation can either leave the quick preset unselected until the user picks one, or fall back to a designated default preset. The plan should favor explicit behavior over silent rounding.

Centralize the predefined duration values in a small composable or constant so they can be reused for rendering options and matching an existing entry duration. Reuse the existing minute vocabulary already present in translations and calendar interval handling where practical, but do not couple this UI to calendar-only store state.

Validation rules should remain based on concrete timestamps. In quick mode, validation should require `startTime` and a selected duration and then ensure the derived `endTime` stays after `startTime`. In granular mode, keep the current `dateAfter(startTime)` validation path.

Apply the new field behavior in the overlays that currently render `TimeEntryFieldContainer`, including:

- list create
- list edit
- calendar edit

The calendar create overlay currently uses `skipTimeFields` because time comes from the calendar interaction itself. Keep that behavior unless there is an explicit product decision to expose the new time UI there as well.

Update `useTimeEntryHelper` only as needed to preserve default `startTime` behavior for new entries. The helper should continue to produce actual `startTime` and `endTime` values only; it should not become responsible for UI mode or preset state.

Add translation keys in the English and German translation files for the mode labels, duration label, and duration options. Keep terminology consistent with the existing `timeEntry.field.startTime`, `timeEntry.field.endTime`, and duration wording already used in the list and calendar UI.
