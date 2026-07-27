import type { tags } from "typia";

export type Branded<T extends string, Name extends string> = T &
    tags.TagBase<{
        kind: Name;
        target: "string";
        value: undefined;
        validate: "true";
    }>;
