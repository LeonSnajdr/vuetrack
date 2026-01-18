export type ActionResult<T = void> =
    | (T extends void ? { status: "success"; data?: T } : { status: "success"; data: T })
    | { status: "error" }
    | { status: "cancelled" };

export function success(): ActionResult<void>;
export function success<T>(data: T): ActionResult<T>;
export function success<T>(data?: T): ActionResult<T> {
    return { status: "success", data } as ActionResult<T>;
}
export const error = (): ActionResult<never> => ({ status: "error" });
export const cancelled = (): ActionResult<never> => ({ status: "cancelled" });
