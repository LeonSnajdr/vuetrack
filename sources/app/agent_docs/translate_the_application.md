# agent_doc: Translating the Application (i18n)

## Purpose

This document guides an agent through translating the application safely and consistently. Follow these instructions to add or update translations with minimal duplication, a predictable key structure, and correct singular/plural handling.

## Translation framework

- We use **i18n**.
- Translations live in: **`src/translations`**.

## Using translations in Vue components (required)

- In Vue templates, **always use `$t(...)`**.
- Do not call `t(...)` directly inside templates.
- In `<script setup>`, always import `useI18n()` and destructure t

## Scope: which languages to translate

- **All languages present in `src/translations` must be kept in sync.**
- Any new key added in one language must be added to every other language file in that folder.
- Do not leave missing keys; the PR should contain complete coverage across all existing languages.

## File and key conventions

### Use i18n “flat” structure

- Use a **flat key structure**, not nested objects.
- Keys should be namespaced using dot notation.

Examples:

- `action.close`
- `action.close.error`
- `settings.language`
- `user.profile.title`

Avoid:

- Nested JSON like:
  - `action: { close: "Close" }`

### Alphabetical sorting

- **Sort translation keys alphabetically** within each file.
- Keep ordering consistent across languages (same key order) to simplify reviews and diffs.

### Avoid repetition (create generic translations)

- Prefer reusable, generic keys for common UI actions and messages.
- Only introduce a new key when the meaning truly differs.

Good pattern (generic + parameterized):

- `action.close`: `"Close"`
- `action.confirm`: `"Confirm"`
- `action.close.error`: `"{type} '{name}' couldn't be closed"`

- Prefer composition via variables over duplicating near-identical strings.

## Variables and interpolation

- Use i18n interpolation placeholders (example format shown below).
- Keep placeholder names consistent across languages.

Example:

- `action.close.error`: `"{type} '{name}' couldn't be closed"`

Rules:

- Don’t bake dynamic values directly into strings.
- Use descriptive variable names like `{name}`, `{type}`, `{count}`, `{field}`.

## Pluralization (required pattern)

Pluralization is expressed using a pipe-separated singular/plural form in the same value:

- Format:  
  `"<singular> | <plural>"`

## Quality standards

- Consistency: same key used for same meaning across the app.
- Minimal duplication: consolidate repeated phrases into shared keys.
- Determinism: alphabetical ordering and flat keys for clean diffs.
- Completeness: every language in `src/translations` updated together.
- Correctness: pluralization and interpolation wired correctly.

---

## Done criteria (PR readiness)

- No hardcoded UI strings added/left behind.
- All keys are flat, alphabetically sorted, and consistently named.
- All languages in `src/translations` contain the same set of keys.
- Repetition reduced via generic keys and interpolation.
- Singular/plural handled via i18n pluralization features.

## Agent docs (progressive disclosure)

If you need more context, read the relevant file(s) in `agent_docs/` before
making changes. Prefer using these docs instead of guessing project-specific
behavior.

When a task touches translation/localization, start by reading:

- `agent_docs/translate_the_application.md` — How we translate the application
  (workflow, conventions, file locations, QA checks, and pitfalls).

If you’re unsure which doc applies, ask which `agent_docs/*` you should read,
or propose the specific files you want to open and why.
