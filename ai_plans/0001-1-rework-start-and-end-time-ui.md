## Rationale

Rework the time-entry time editing UI so the default flow is faster for the common case while still keeping the existing granular editing available. In the revised default flow, the user still works with concrete `startTime` and `endTime` values, but the end time can be adjusted through a minute-based duration input that acts as an alternate view on `endTime`. This keeps the data model unchanged and allows the UI to evolve incrementally.

## Acceptance Criteria

- [ ] The default time-entry UI exposes a minute selection that represents the duration between `startTime` and `endTime` and updates `endTime` internally when changed.
- [ ] The minute selection is implemented with a `VNumberInput` and offers reasonable preset values through a `VMenu`.
- [ ] The default mode keeps the existing behavior that a newly created list entry pre-fills `startTime` from the latest entry end time or other existing defaults.
- [ ] A granular mode is available that exposes the current explicit `startTime` and `endTime` inputs unchanged in behavior.
- [ ] Switching between default mode and granular mode keeps the current timestamps instead of resetting user input.
- [ ] Validation still prevents invalid time ranges, and saving continues to submit `startTime` and `endTime` through the existing contracts without backend changes.
- [ ] The new UI is applied consistently in the time-entry overlays that currently use `TimeEntryFieldContainer`, except flows that intentionally hide time fields.
- [ ] The revised default mode does not need to place the controls on a single line yet.
- [ ] New labels and option texts are added to both English and German translations.

## Technical Details

Keep `TimeEntryCreateContract` and `TimeEntryUpdateContract` unchanged. The rework should stay in the UI layer and continue to submit concrete `startTime` and `endTime` values to the existing stores and services.

Refactor `TimeEntryFieldContainer` so the time section becomes mode-aware instead of always rendering separate start-time and end-time rows. Introduce a dedicated time-range field component that owns:
- the mode switch between default and granular mode
- the default-mode start-time input plus minute-based duration input
- the existing granular start-time and end-time inputs as a fallback view

In default mode, bind `startTime` directly to the existing `BaseDateTimeInput`. The minute selection is not a separate domain value; it is only another representation of `endTime`. The component should calculate the currently displayed minutes from the difference between `startTime` and `endTime`, and when the user changes that number, the component should update `endTime` to `startTime + minutes`.

Implement the minute input with `VNumberInput`. Attach a `VMenu` to that control so users can quickly choose from predefined reasonable minute values such as 10, 15, 30, 45, 60, and similar options agreed on during implementation. The presets should populate the same numeric input state rather than introducing a second source of truth.

For this iteration, the default-mode controls do not need to be rendered in a single line. It is sufficient to group them clearly within one time section so the behavior can be validated before tightening the layout.

Centralize the predefined minute options in a small composable or constant so they can be reused for rendering the menu and for matching existing durations in edit flows. If an existing entry has a duration that does not match a preset, the numeric input should still show the exact calculated minute value.

Validation rules should remain based on concrete timestamps. In default mode, validation should ensure that `startTime` exists, the minute value is present and positive, and the derived `endTime` remains after `startTime`. In granular mode, keep the current `dateAfter(startTime)` validation path.

Apply the new field behavior in the overlays that currently render `TimeEntryFieldContainer`, including:
- list create
- list edit
- calendar edit

The calendar create overlay currently uses `skipTimeFields` because time comes from the calendar interaction itself. Keep that behavior unless there is an explicit product decision to expose the new time UI there as well.

Update `useTimeEntryHelper` only as needed to preserve default `startTime` behavior for new entries. The helper should continue to produce actual `startTime` and `endTime` values only; it should not become responsible for UI mode or minute-input state.

Add translation keys in the English and German translation files for the mode labels, minute input label, and preset option texts. Keep terminology consistent with the existing `timeEntry.field.startTime`, `timeEntry.field.endTime`, and duration wording already used in the list and calendar UI.
