import { resolveForce, resolveShiftDown, resolveShiftUp, resolveTruncate, type ConflictResolver } from "@/components/tracking/calendar/conflictResolvers";

export interface ConflictResolutionStrategy {
    id: string;
    label: string;
    subtitle: string;
    icon: string;
    // Set on resolutions that reshape other entries.
    color?: "error";
    resolve: ConflictResolver;
}

// The offer list. Each resolution is pure: the panel decides whether to stage its answer.
export function useConflictStrategies() {
    const { t } = useI18n();

    const strategies = computed<ConflictResolutionStrategy[]>(() => [
        {
            id: "shift-up",
            label: t("calendar.conflict.strategy.movePrevious"),
            subtitle: t("calendar.conflict.strategy.movePrevious.subtitle"),
            icon: mdiArrowUpThin,
            resolve: resolveShiftUp
        },
        {
            id: "shift-down",
            label: t("calendar.conflict.strategy.moveNext"),
            subtitle: t("calendar.conflict.strategy.moveNext.subtitle"),
            icon: mdiArrowDownThin,
            resolve: resolveShiftDown
        },
        {
            id: "truncate",
            label: t("calendar.conflict.strategy.fitToGap"),
            subtitle: t("calendar.conflict.strategy.fitToGap.subtitle"),
            icon: mdiArrowCollapseVertical,
            resolve: resolveTruncate
        },
        {
            id: "force",
            label: t("calendar.conflict.strategy.forcePosition"),
            subtitle: t("calendar.conflict.strategy.forcePosition.subtitle"),
            icon: mdiAlertBoxOutline,
            color: "error",
            resolve: resolveForce
        }
    ]);

    return { strategies };
}
