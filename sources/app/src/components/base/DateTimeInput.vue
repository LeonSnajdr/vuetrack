<template>
    <VValidation v-slot="{ errorMessages, isValid, validate }" v-model="dateTime" :rules="rules">
        <BaseValidationDynamicRulesSupport @rulesChanged="validate" :rules="rules" />
        <div class="d-flex flex-column ga-2">
            <div class="d-flex ga-2">
                <VDateInput
                    ref="dateInputRef"
                    v-model="selectedDate"
                    v-model:menu="isDateMenuOpen"
                    v-bind="$attrs"
                    @keydown.capture="onDateInputKeydown"
                    :error="!(isValid.value ?? true)"
                />
                <VTextField
                    ref="timeInputRef"
                    v-model="timeInput"
                    @blur="onTimeInputBlur"
                    @keydown.enter.prevent="commitTimeInput"
                    :error="!(isValid.value ?? true)"
                    :tabindex="timeInputTabindex"
                >
                    <VMenu ref="timeMenuRef" v-model="isTimeMenuOpen" :closeOnContentClick="false" activator="parent" minWidth="0">
                        <VTimePicker
                            v-model="selectedTime"
                            v-model:viewMode="timePickerViewMode"
                            @pointerdown="onTimePickerPointerDown"
                            @update:minute="closeTimeMenu"
                            format="24hr"
                            hideHeader
                        />
                    </VMenu>
                </VTextField>
            </div>
            <div v-if="!(isValid.value ?? true)" class="ml-4">
                <VMessages :active="errorMessages.value.length > 0" :messages="errorMessages.value" color="error" style="opacity: 1" />
            </div>
        </div>
    </VValidation>
</template>

<script setup lang="ts">
import type { ValidationRule } from "vuetify";

defineProps<{
    rules?: ValidationRule[];
}>();

const dateTime = defineModel<Date | null>({ required: true });
const dateInputRef = useTemplateRef("dateInputRef");
const timeInputRef = useTemplateRef("timeInputRef");
const timeMenuRef = useTemplateRef("timeMenuRef");
const isDateMenuOpen = ref(false);
const isTimeMenuOpen = ref(false);
const timePickerViewMode = ref<"hour" | "minute">("hour");
const timeInput = ref("");
let skipTimeMenuCloseOnBlur = false;
const attrs = useAttrs();
const timeInputTabindex = computed(() => attrs.tabindex ?? attrs.tabIndex);

const selectedDate = computed({
    get: () => dateTime.value,
    set: (value: Date | null) => {
        if (value === null) {
            dateTime.value = null;
            timeInput.value = "";
            return;
        }

        const timeSource = dateTime.value ?? new Date();
        const nextDateTime = new Date(value);
        nextDateTime.setHours(timeSource.getHours(), timeSource.getMinutes(), timeSource.getSeconds(), timeSource.getMilliseconds());
        dateTime.value = nextDateTime;
    }
});

const selectedTime = computed({
    get: () => formatTime(dateTime.value),
    set: (value: string) => {
        const [hoursText = "", minutesText = ""] = value.split(":");
        const hours = Number.parseInt(hoursText, 10);
        const minutes = Number.parseInt(minutesText, 10);

        if (Number.isNaN(hours) || Number.isNaN(minutes)) {
            return;
        }

        updateDateTime(hours, minutes);
    }
});

watch(
    () => dateTime.value?.getTime() ?? null,
    () => {
        timeInput.value = formatTime(dateTime.value);
    },
    { immediate: true }
);

watch(isTimeMenuOpen, (isOpen: boolean) => {
    if (isOpen) {
        timePickerViewMode.value = "hour";
    }
});

function commitTimeInput(): void {
    const parsedTime = parseTimeInput(timeInput.value);

    if (!parsedTime) {
        timeInput.value = formatTime(dateTime.value);
        return;
    }

    updateDateTime(parsedTime.hours, parsedTime.minutes);
}

function onTimeInputBlur(event: FocusEvent): void {
    commitTimeInput();

    if (shouldKeepTimeMenuOpen(event)) {
        return;
    }

    closeTimeMenu();
}

function closeTimeMenu(): void {
    isTimeMenuOpen.value = false;
}

function onTimePickerPointerDown(): void {
    skipTimeMenuCloseOnBlur = true;

    requestAnimationFrame(() => {
        skipTimeMenuCloseOnBlur = false;
    });
}

async function onDateInputKeydown(event: KeyboardEvent): Promise<void> {
    if (event.key !== "Tab" || event.shiftKey) {
        return;
    }

    const target = event.target;

    if (!(target instanceof HTMLInputElement) || !isElementInsideDateInput(target)) {
        return;
    }

    if (isTabIndexMinusOne()) {
        return;
    }

    event.preventDefault();
    event.stopPropagation();

    isDateMenuOpen.value = false;

    await nextTick();
    focusTimeInputAndSelectAll();
}

function isTabIndexMinusOne(): boolean {
    const tabIndexAttr = attrs.tabindex ?? attrs.tabIndex;

    return `${tabIndexAttr ?? ""}`.trim() === "-1";
}

function focusTimeInputAndSelectAll(): void {
    timeInputRef.value?.focus?.();
    getTimeInputElement()?.select();
}

function getTimeInputElement(): HTMLInputElement | null {
    const timeInputElement = timeInputRef.value?.$el;

    if (!(timeInputElement instanceof Element)) {
        return null;
    }

    const inputElement = timeInputElement.querySelector("input");

    return inputElement instanceof HTMLInputElement ? inputElement : null;
}

function shouldKeepTimeMenuOpen(event: FocusEvent): boolean {
    if (skipTimeMenuCloseOnBlur) {
        return true;
    }

    const nextFocusedElement = event.relatedTarget;

    return nextFocusedElement instanceof HTMLElement ? isElementInsideTimeInputOrMenu(nextFocusedElement) : false;
}

function isElementInsideTimeInputOrMenu(element: HTMLElement): boolean {
    const timeInputElement = timeInputRef.value?.$el;
    const timeMenuContentElement = timeMenuRef.value?.contentEl;

    return (
        (timeInputElement instanceof Element && timeInputElement.contains(element)) ||
        (timeMenuContentElement instanceof Element && timeMenuContentElement.contains(element))
    );
}

function parseTimeInput(value: string): { hours: number; minutes: number } | null {
    const trimmedValue = value.trim();

    if (!trimmedValue) {
        return null;
    }

    const normalizedValue = trimmedValue.replace(/[.,\s-]+/g, ":");
    const colonMatch = normalizedValue.match(/^(\d{1,2}):(\d{0,2})$/);

    if (colonMatch) {
        return toTimeParts(colonMatch[1], colonMatch[2] || "0");
    }

    const hourOnlyMatch = normalizedValue.match(/^(\d{1,2}):?$/);

    if (hourOnlyMatch) {
        return toTimeParts(hourOnlyMatch[1], "0");
    }

    const digitMatch = trimmedValue.match(/^(\d{3,4})$/);

    if (digitMatch) {
        const compactValue = digitMatch[1];
        const hourLength = compactValue.length - 2;

        return toTimeParts(compactValue.slice(0, hourLength), compactValue.slice(hourLength));
    }

    return null;
}

function toTimeParts(hoursText: string, minutesText: string): { hours: number; minutes: number } | null {
    const hours = Number.parseInt(hoursText, 10);
    const minutes = Number.parseInt(minutesText, 10);

    if (Number.isNaN(hours) || Number.isNaN(minutes) || hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
        return null;
    }

    return { hours, minutes };
}

function updateDateTime(hours: number, minutes: number): void {
    const nextDateTime = dateTime.value ? new Date(dateTime.value) : new Date();
    nextDateTime.setHours(hours, minutes, nextDateTime.getSeconds(), nextDateTime.getMilliseconds());

    dateTime.value = nextDateTime;
    timeInput.value = formatTime(nextDateTime);
}

function isElementInsideDateInput(element: HTMLElement): boolean {
    const dateInputElement = dateInputRef.value?.$el;

    return dateInputElement instanceof Element ? dateInputElement.contains(element) : false;
}

function formatTime(value: Date | null): string {
    if (value === null) {
        return "";
    }

    return `${padTimePart(value.getHours())}:${padTimePart(value.getMinutes())}`;
}

function padTimePart(value: number): string {
    return value.toString().padStart(2, "0");
}
</script>
