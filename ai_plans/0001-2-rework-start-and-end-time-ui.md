## Rationale

Rework the time-entry time editing UI so the common flow is faster while preserving the existing granular editing behavior. In this iteration, the existing end-time field becomes responsible for choosing how `endTime` is presented. A new `mode` prop allows the same field to render either the current exact timestamp editor or a minute-based duration view that still writes to `endTime` internally. This keeps the data model unchanged and localizes the display logic around the end-time field.

## Acceptance Criteria

- [x] `TimeEntryFieldEndTime` accepts a `mode` prop that determines how the end-time field is displayed.
- [x] In the default mode, the end-time field shows a minute-based input that represents the duration between `startTime` and `endTime` and updates `endTime` internally when changed.
- [x] The minute selection in default mode is implemented with a `VNumberInput` and offers reasonable preset values through a `VMenu`.
- [x] In granular mode, the end-time field keeps the current explicit `BaseDateTimeInput` behavior unchanged.
- [x] The default mode keeps the existing behavior that a newly created list entry pre-fills `startTime` from the latest entry end time or other existing defaults.
- [x] Switching between the available display modes keeps the current timestamps instead of resetting user input.
- [x] Validation still prevents invalid time ranges, and saving continues to submit `startTime` and `endTime` through the existing contracts without backend changes.
- [x] The new end-time display behavior is applied consistently in the time-entry overlays that currently use `TimeEntryFieldContainer`, except flows that intentionally hide time fields.
- [x] The revised default mode does not need to place the controls on a single line yet.
- [x] New labels and option texts are added to both English and German translations.

## Technical Details

Keep `TimeEntryCreateContract` and `TimeEntryUpdateContract` unchanged. The rework should stay in the UI layer and continue to submit concrete `startTime` and `endTime` values to the existing stores and services.

Extend `TimeEntryFieldEndTime` with a `mode` prop, for example a small union such as `"granular" | "duration"`. The field should remain the single owner of `endTime` input behavior, while `TimeEntryFieldContainer` decides which mode to pass based on the active UI flow.

In granular mode, preserve the current implementation based on `BaseDateTimeInput` and `dateAfter(startTime)` validation. This path should remain behaviorally identical to today so the new work does not regress explicit timestamp editing.

In duration mode, `TimeEntryFieldEndTime` should render a `VNumberInput` instead of the date/time picker. The numeric value is not a separate domain field; it is another representation of `endTime` relative to `startTime`. The component should derive the displayed minute count from the difference between `startTime` and `endTime`, and when the user changes that number, it should update `endTime` to `startTime + minutes`.

Attach a `VMenu` to the `VNumberInput` so users can quickly choose from predefined reasonable minute values such as 10, 15, 30, 45, 60, and similar options agreed on during implementation. The menu presets should write into the same numeric input state so there is only one source of truth in the component.

Because duration mode depends on `startTime`, keep `startTime` as a required input for `TimeEntryFieldEndTime` in both modes. In duration mode, validation should ensure that `startTime` exists, the minute value is present and positive, and the derived `endTime` remains after `startTime`.

Update `TimeEntryFieldContainer` to pass the correct `mode` prop into `TimeEntryFieldEndTime`. The container can remain responsible for higher-level form composition, but it should not duplicate the end-time display logic that now lives inside the field component.

For this iteration, the default-mode controls do not need to be rendered on a single line. It is sufficient to group them clearly within the form so the behavior can be validated before tightening the layout.

Centralize the predefined minute options in a small composable or constant so they can be reused for rendering the `VMenu` and for handling edit flows consistently. If an existing entry has a duration that does not match a preset, the numeric input should still show the exact calculated minute value.

Apply the new field behavior in the overlays that currently render `TimeEntryFieldContainer`, including:
- list create
- list edit
- calendar edit

The calendar create overlay currently uses `skipTimeFields` because time comes from the calendar interaction itself. Keep that behavior unless there is an explicit product decision to expose the new time UI there as well.

Update `useTimeEntryHelper` only as needed to preserve default `startTime` behavior for new entries. The helper should continue to produce actual `startTime` and `endTime` values only; it should not become responsible for UI mode or minute-input state.

Add translation keys in the English and German translation files for the mode labels, minute input label, and preset option texts. Keep terminology consistent with the existing `timeEntry.field.startTime`, `timeEntry.field.endTime`, and duration wording already used in the list and calendar UI.
