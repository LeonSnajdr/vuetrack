export type ActionResult<T = void, TError = unknown> =
    | (T extends void ? { status: "success"; data?: T } : { status: "success"; data: T })
    | { status: "error"; error?: TError }
    | { status: "cancelled" };

export function success(): ActionResult<void>;
export function success<T>(data: T): ActionResult<T>;
export function success<T>(data?: T): ActionResult<T> {
    return { status: "success", data } as ActionResult<T>;
}
export const error = <TError = unknown>(cause?: TError): ActionResult<never, TError> => ({ status: "error", error: cause });
export const cancelled = (): ActionResult<never> => ({ status: "cancelled" });
