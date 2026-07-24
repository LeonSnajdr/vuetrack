import { parseISO } from "date-fns";

export function transformResponse(data: unknown): unknown {
    // https://github.com/axios/axios/blob/main/lib/defaults/index.js#L118
    if (typeof data === "string") {
        try {
            return JSON.parse(data, transformValue);
        } catch {
            /* ignore */
        }
    }

    return data;
}

export function transformResponseDefault(data: unknown): unknown {
    // https://github.com/axios/axios/blob/main/lib/defaults/index.js#L118
    if (typeof data === "string") {
        try {
            return JSON.parse(data);
        } catch {
            /* ignore */
        }
    }

    return data;
}

function transformValue(key: string, value: string): Date | string {
    // https://en.wikipedia.org/wiki/ISO_8601
    const isoDate = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,3})?Z$/;

    return isoDate.test(value) ? parseISO(value) : value;
}
