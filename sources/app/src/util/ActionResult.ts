import type { ValidationErrors } from "@/util/ValidationProblem";

export type ActionResult<T = void, TError = unknown> =
    | (T extends void ? { status: "success"; data?: T } : { status: "success"; data: T })
    | { status: "error"; error?: TError; validation?: ValidationErrors | null }
    | { status: "cancelled" };

export function success(): ActionResult<void>;
export function success<T>(data: T): ActionResult<T>;
export function success<T>(data?: T): ActionResult<T> {
    return { status: "success", data } as ActionResult<T>;
}
export const error = <TError = unknown>(cause?: TError, validation?: ValidationErrors | null): ActionResult<never, TError> => ({
    status: "error",
    error: cause,
    validation: validation ?? null
});
export const cancelled = (): ActionResult<never> => ({ status: "cancelled" });
