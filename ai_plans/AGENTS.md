# AGENTS.md for AI Plans

In this folder, we keep one folder per planned feature. Each feature folder uses an incrementing four-digit prefix followed by a short slug, for example `0001-rework-start`.

## Folder Structure

Create a dedicated folder for each feature:

```text
0001-rework-start/
|_ v1_prompt.md
|_ v1_plan.md
|_ v2_prompt.md
|_ v2_plan.md
|_ _results.md
```

Rules:

1. The feature folder name must start with a four-digit prefix which is incrementing across `ai_plans`.
2. Keep iterative prompt and plan files together in the same feature folder.
3. Name prompt files `v<number>_prompt.md`.
4. Name plan files `v<number>_plan.md`.
5. Each prompt version should have a matching plan version.
6. When the work is finished, add `_results.md` to capture relevant notes such as what was implemented, follow-up improvements, and token usage if it is useful.

## How to Write Plans

1. Each plan consists of exactly the three following elements: a rationale, acceptance criteria, and technical details.
2. A rationale describes the overarching goal of the plan. Keep this concise, only elaborate when special circumstances require it. Use free text in this section of the plan.
3. Acceptance criteria is a list of requirements that must be met for the plan to be considered complete. Use check marks `- [ ]` for these in Markdown.
4. Technical details describes the changes on a technical level. Which classes and members should be extended, which design patterns should be used? How do we ensure performance? Avoid going into too much detail (do not write the finished implementation), but focus on the high-level design and how things are connected. It should give the implementer a clear picture of what needs to be done, but also give her or him freedom to implement the plan in a way that she or he deems best. The length of this section is highly dependent on the task that the plan addresses.
