<template>
    <svg
        :aria-hidden="label ? undefined : 'true'"
        :role="label ? 'img' : undefined"
        :style="rootStyle"
        class="vt-loader"
        viewBox="0 0 240 280"
        xmlns="http://www.w3.org/2000/svg"
    >
        <title v-if="label">{{ label }}</title>

        <defs>
            <linearGradient :id="gradientId" x1="0.12" x2="0.9" y1="0.05" y2="0.95">
                <stop offset="0" stop-color="var(--vt-light, #FFC94A)" />
                <stop offset="0.5" stop-color="var(--vt-mid, #E6A921)" />
                <stop offset="1" stop-color="var(--vt-dark, #AE6A1B)" />
            </linearGradient>
        </defs>

        <ellipse v-if="shadow" class="vt-shadow" cx="125" cy="262" fill="var(--vt-shadow-color, #8A5714)" opacity="0.22" rx="54" ry="9" />

        <g class="vt">
            <g class="vt-bob">
                <g class="vt-squash">
                    <g transform="translate(-25,10)">
                        <g class="vt-ant">
                            <path
                                d="M 137 24 c -3 1.5 -2.5 2.5 0.5 3 4 -0 5 3 3 5.5 -2 2.5 0.5 8.5 3.5 8.5 3.5 -0 7 -7.5 4.5 -9.5 -2 -1 0.5 -3.5 4 -3.5 5 -0 3.5 -2 -2 -4 -4 -1.5 -11 -1.5 -13.5 -0z"
                                fill="var(--vt-limb, #111111)"
                            />
                        </g>

                        <g class="vt-leg-a">
                            <path
                                d="M 116.5 198 c -2.5 2 -2 4.5 1 6 3.5 2 6.5 5.5 6.5 8.5 0 0.5 0.5 1.5 0.5 2 0.5 0.5 0.5 0.5 0 1 0 0.5 -0.5 1.5 -0.5 2.5 0 1 -0.5 3 -1 3.5 -0.5 1 -1 2.5 -1 2.5 0 1 -1.5 4 -2 4.5 -0.5 -0 -3 5.5 -3 6.5 0 1 2.5 2.5 6 3.5 1 0.5 3 1 4 1 9 3 12.5 -3.5 4 -8 -5.5 -3 -5.5 -2.5 -3 -7 1 -2.5 2 -5 2 -5.5 0 -1 0 -1.5 0.5 -2 0.5 -0 0.5 -1.5 0.5 -3.5 0 -2 0 -3.5 -0.5 -4 -0.5 -0 -0.5 -0.5 -0.5 -1.5 0 -5.5 -10 -13 -13.5 -10z"
                                fill="var(--vt-limb, #111111)"
                            />
                        </g>

                        <g class="vt-leg-b">
                            <path
                                d="M 160 194.5 c -0.5 0.5 -2 3 -2 4 0 -0 1 1 2 2 4.5 3.5 7 6 7 7.5 0 0.5 0 1 0.5 1 0.5 0.5 0.5 1.5 0.5 3 0 1.5 0 2.5 -0.5 2.5 -0.5 0.5 -0.5 1 -0.5 1.5 0 1 0 1.5 -0.5 1.5 -0.5 0.5 -0.5 1 -0.5 1.5 0 1 -3 7 -4 8.5 -2.5 2.5 -2.5 5.5 -0.5 6 0.5 0.5 2 1 3 1.5 13.5 6 20.5 -1.5 7.5 -8 -3 -1.5 -3.5 -2 -2.5 -3 1 -1 2.5 -3.5 2.5 -4.5 0 -0.5 0 -1 0.5 -1.5 0.5 -0 0.5 -0.5 0.5 -1.5 0 -0.5 0 -1 0.5 -1.5 0.5 -0 0.5 -2 0.5 -4.5 0 -2.5 0 -4.5 -0.5 -5 -0.5 -0 -0.5 -0.5 -0.5 -1 0 -5 -9.5 -12.5 -13 -10z"
                                fill="var(--vt-limb, #111111)"
                            />
                        </g>

                        <g class="vt-arm-r">
                            <path
                                d="M 240.5 77.5 c -0.5 1 -1.5 2.5 -2 3.5 -0.5 1 -1.5 2 -1.5 2.5 -0.5 0.5 -1 1 -1 1.5 0 1 -1 1 -1.5 0.5 -1 -1 -3 -0.5 -3.5 0.5 0 2 3.5 8.5 5.5 10 2.5 1.5 3 3.5 2 4.5 -0.5 0.5 -0.5 1.5 -0.5 2.5 0 0.5 0 1.5 -0.5 1.5 -0.5 0.5 -0.5 1 -0.5 1.5 0 1 0 1.5 -0.5 1.5 -0.5 0.5 -0.5 1 -0.5 1.5 0 1 -2.5 5.5 -3.5 6.5 0 0.5 -0.5 1 -0.5 1 0 1.5 -6 6.5 -9.5 8 -1.5 1 -1.5 1 -1.5 4 0 1.5 0 3 -0.5 3 -1 1 -0.5 6 0.5 6 5.5 -2 15 -8 15 -10.5 0 -0 0.5 -0.5 0.5 -1 1.5 -1 5.5 -9.5 5.5 -11 0 -1 0 -1.5 0.5 -2 0.5 -0 0.5 -1 0.5 -1.5 0 -1 0 -2 0.5 -2.5 0 -0 0.5 -2.5 0.5 -5 0.5 -4.5 0.5 -4.5 2 -6 2 -1 3 -6.5 1.5 -7.5 -0.5 -0 -0.5 -1 -0.5 -2 0 -0.5 0 -1.5 -0.5 -2 -0.5 -0 -0.5 -1 -0.5 -2 0 -0.5 0 -1.5 -0.5 -2 -0.5 -0 -0.5 -0.5 -0.5 -1.5 0 -0.5 0 -1 -0.5 -1.5 -0.5 -0 -0.5 -0.5 -0.5 -1.5 0 -0.5 -0.5 -1 -1 -1.5 -1 -0.5 -1 -0.5 -2.5 1z"
                                fill="var(--vt-limb, #111111)"
                            />
                        </g>

                        <circle :fill="`url(#${gradientId})`" cx="131" cy="124" r="89" />

                        <g class="vt-head">
                            <path
                                d="M 136 62.5 c -0.5 0.5 -1.5 0.5 -2 0.5 -1.5 -0 -3 0.5 -7.5 2 -5.5 1.5 -8.5 3 -10 4 -0.5 0.5 -2 1 -3 1.5 -3 2 -14.5 12 -14.5 13.5 0 -0 -0.5 1 -1 1 -0.5 0.5 -1 1.5 -1 2 0 -0 -0.5 0.5 -0.5 1 -1 1 -4.5 7.5 -4.5 8.5 0 0.5 0 1 -0.5 1 -0.5 0.5 -0.5 1 -0.5 1.5 0 1 0 1.5 -0.5 1.5 -0.5 0.5 -0.5 1.5 -0.5 2 0 1 0 2 -0.5 2 -0.5 0.5 -0.5 2 -0.5 3.5 0 2 0 3.5 -0.5 3.5 -2.5 2 1.5 6.5 7 7.5 1 0.5 2.5 1 4 1 1.5 0.5 5 1 15 2 6 1 53.5 1 61 -0 13 -1 22 -3 24.5 -5.5 1 -0.5 1.5 -6 0.5 -7 -0.5 -0 -0.5 -1.5 -0.5 -3 0 -1 0 -2.5 -0.5 -3 -0.5 -0 -0.5 -1 -0.5 -2 0 -0.5 0 -1.5 -0.5 -2 -0.5 -0 -0.5 -0.5 -0.5 -1 0 -1 -5.5 -12 -6.5 -12.5 0 -0.5 -0.5 -1 -0.5 -1 0 -1 -10 -11 -13.5 -13.5 -7.5 -4.5 -12 -6.5 -22 -8.5 -3.5 -1 -18.5 -1 -19.5 -0.5z"
                                fill="var(--vt-face, #FFFFFF)"
                            />
                            <path
                                d="M 94.5 98 c -4.5 8 -7.5 7.5 50.5 8 54.5 -0 53.5 -0 51.5 -2 0 -0.5 -0.5 -1.5 -0.5 -2.5 0 -0.5 0 -1.5 -0.5 -2 -0.5 -0 -0.5 -0.5 -0.5 -1 0 -3.5 -3 -4.5 -8 -3 -1 -0 -1.5 -0 -2 -0 -1 -0.5 -5.5 -0.5 -5.5 -0 0 0.5 0.5 0.5 1.5 0.5 1 -0 1.5 0.5 2 0.5 0 0.5 0.5 1 2 1 2 -0.5 2.5 1 1 1.5 -1 -0 -1.5 0.5 -1.5 1 -1.5 2 -4.5 2 -4 -0 0 -1.5 -0.5 -1.5 -2 -0 -1.5 1 -2.5 1 -2.5 -0.5 0 -1.5 -1 -1.5 -2.5 -0.5 -1.5 0.5 -2.5 -0 -1.5 -1 1.5 -2 -0.5 -3.5 -3 -2.5 -0.5 0.5 -2.5 0.5 -4 0.5 -2 -0 -3.5 0.5 -5 1 -2.5 1.5 -4.5 1.5 -5 -0 -1 -3 -11.5 -3 -13.5 -0.5 -1 2 -2.5 2 -6 0.5 -1.5 -0.5 -4 -1 -5.5 -1 -2 -0 -3.5 -0 -4 -0.5 -2.5 -1 -4.5 0.5 -3 2.5 1.5 1.5 -0.5 1.5 -2.5 0.5 -3 -2 -4 -1.5 -2 0.5 1 1.5 0 2.5 -2 1 -1.5 -1.5 -2 -1.5 -2 -0 0 1.5 -1.5 2 -3 0.5 -0.5 -0.5 -1.5 -1 -2 -1.5 -2 -1 -1.5 -2 0.5 -1.5 1.5 -0 2 -0.5 2 -1 0.5 -0 1 -0.5 1.5 -0.5 0.5 -0 1 -0 1 -0.5 0 -0.5 -3.5 -0.5 -4.5 -0 -0.5 -0 -1 -0 -2 -0 -5.5 -2 -12 -1 -13.5 2.5z"
                                fill="var(--vt-blush, #EAC3CB)"
                            />
                            <path
                                d="M 127 91.5 c -2.5 1.5 -1 3 2.5 3.5 3.5 0.5 5.5 1 8 2 2 1.5 3.5 0.5 3.5 -1.5 0 -3 -11 -6 -14 -4z"
                                fill="var(--vt-ink, #111111)"
                            />
                            <path
                                d="M 160 92 c -3 1 -5 2.5 -5 4 0.5 1.5 1.5 1.5 5 -0 1 -0.5 3.5 -1 5 -1 2.5 -0 3 -0.5 3 -1.5 0.5 -2.5 -4 -3.5 -8 -1.5z"
                                fill="var(--vt-ink, #111111)"
                            />
                        </g>

                        <g class="vt-arm-l">
                            <path
                                d="M 55 128.5 c -3 2.5 -2.5 8 1 12.5 0.5 0.5 1 1.5 1.5 2.5 0.5 1 1.5 2 1.5 2.5 0.5 0.5 1 1 1 1 0 0.5 0.5 1.5 1.5 2 1 1 1.5 2 1.5 2 0 0.5 1 1.5 2 2.5 1 1.5 2 2.5 2 3 0 0.5 10.5 10.5 13.5 12.5 1 1 2.5 2.5 3.5 3 3 2 11.5 7 15 8 1 0.5 3 1.5 4 2 12.5 4.5 30.5 3 45 -4 3.5 -1.5 3.5 -1.5 4.5 -0.5 4 3.5 8.5 4 9.5 1 0 -1 1 -2 1.5 -2.5 1 -0.5 1.5 -1.5 1.5 -2 0 -0 0.5 -1 1 -1.5 0.5 -0 1 -1 1 -1.5 0 -0 0.5 -0.5 1 -1 0 -0.5 1 -1.5 1.5 -2.5 0.5 -1 1 -2 1.5 -2.5 1 -1 5 -9.5 5 -10.5 0 -1 -2 -1.5 -4 -1 -1 -0 -2.5 0.5 -3.5 0.5 -5.5 1 -9 2 -10 2.5 -1 1 -1.5 -0 -1.5 -2.5 0 -2.5 -1.5 -3.5 -3 -2.5 -1.5 1 -4 5.5 -4 7 0 0.5 0 1 -0.5 1.5 -0.5 -0 -0.5 1 -0.5 2 0 1 -0.5 2.5 -0.5 3 -0.5 0.5 -0.5 1 0 2 1 2 1 5 0.5 5.5 -3.5 2.5 -16 5 -24.5 5 -6 -0 -8 -0 -13 -1.5 -7 -1.5 -15.5 -6 -21.5 -11.5 -6.5 -5.5 -8 -7 -8 -7.5 0 -0.5 -1 -2 -2.5 -3.5 -1.5 -1 -2.5 -2.5 -2.5 -3 0 -0 -0.5 -1 -1.5 -2 -1 -0.5 -1.5 -1.5 -1.5 -2 0 -0 -0.5 -0.5 -1 -1 0 -0.5 -1 -1.5 -1.5 -3 -0.5 -1 -1 -2 -1.5 -2 0 -0.5 -1.5 -3 -3 -5.5 -4 -8.5 -8 -10.5 -12 -6.5z"
                                fill="var(--vt-limb, #111111)"
                            />
                        </g>
                    </g>
                </g>
            </g>
        </g>
    </svg>
</template>

<script setup lang="ts">
const props = defineProps({
    size: { type: [Number, String], default: 160 },
    speed: { type: Number, default: 1 },
    paused: { type: Boolean, default: false },
    shadow: { type: Boolean, default: true },
    label: { type: String, default: "Loading" }
});

let instanceCount = 0;

const gradientId = `vt-body-${++instanceCount}`;

const rootStyle = computed(() => ({
    width: typeof props.size === "number" ? `${props.size}px` : props.size,
    "--vt-cycle": `${(0.9 / (props.speed || 1)).toFixed(4)}s`,
    "--vt-half": `${(0.45 / (props.speed || 1)).toFixed(4)}s`,
    "--vt-play": props.paused ? "paused" : "running"
}));
</script>

<style scoped>
.vt-loader {
    --vt-light: #ffc94a;
    --vt-mid: #e6a921;
    --vt-dark: #ae6a1b;
    --vt-face: #ffffff;
    --vt-blush: #eac3cb;
    --vt-ink: #303a4e;
    --vt-limb: rgb(var(--v-theme-on-surface));
    --vt-shadow-color: #8098a7;

    display: block;
    height: auto;
    overflow: visible;
    margin-inline: auto;
}

.v-theme--dark .vt-loader {
    --vt-limb: #c5c5c5;
    --vt-shadow-color: #000000;
}

.vt *,
.vt-shadow,
.vt-bob,
.vt-squash,
.vt-head,
.vt-ant,
.vt-arm-l,
.vt-arm-r,
.vt-leg-a,
.vt-leg-b {
    transform-box: fill-box;
    animation-play-state: var(--vt-play, running);
}

.vt-bob {
    animation: vt-bob var(--vt-half) cubic-bezier(0.2, 0.6, 0.35, 1) infinite alternate;
}

.vt-squash {
    transform-origin: 50% 100%;
    animation: vt-squash var(--vt-cycle) linear infinite;
}

.vt-head {
    transform-origin: 50% 100%;
    animation: vt-head var(--vt-cycle) ease-in-out infinite;
}

.vt-ant {
    transform-origin: 50% 100%;
    animation: vt-ant var(--vt-cycle) ease-in-out infinite;
    animation-delay: calc(var(--vt-cycle) * -0.156);
}

.vt-arm-r {
    transform-origin: 0% 100%;
    animation: vt-wave var(--vt-cycle) ease-in-out infinite;
}

.vt-arm-l {
    transform-origin: 95% 30%;
    animation: vt-swoosh var(--vt-cycle) ease-in-out infinite;
}

.vt-leg-a {
    transform-origin: 35% 0%;
    animation: vt-step var(--vt-cycle) ease-in-out infinite;
}

.vt-leg-b {
    transform-origin: 35% 0%;
    animation: vt-step var(--vt-cycle) ease-in-out infinite;
    animation-delay: calc(var(--vt-cycle) * -0.5);
}

.vt-shadow {
    transform-origin: 50% 50%;
    animation: vt-shade var(--vt-cycle) linear infinite;
}

@keyframes vt-bob {
    from {
        transform: translateY(0);
    }
    to {
        transform: translateY(-26px);
    }
}

@keyframes vt-squash {
    0% {
        transform: scale(1.1, 0.9);
    }
    16% {
        transform: scale(0.96, 1.05);
    }
    50% {
        transform: scale(1, 1);
    }
    84% {
        transform: scale(0.97, 1.04);
    }
    100% {
        transform: scale(1.1, 0.9);
    }
}

@keyframes vt-head {
    0%,
    100% {
        transform: translateY(2px) scaleX(1.03);
    }
    50% {
        transform: translateY(-2px) scaleX(0.99);
    }
}

@keyframes vt-ant {
    0%,
    100% {
        transform: rotate(-15deg);
    }
    50% {
        transform: rotate(15deg);
    }
}

@keyframes vt-wave {
    0%,
    100% {
        transform: rotate(-7deg);
    }
    50% {
        transform: rotate(15deg);
    }
}

@keyframes vt-swoosh {
    0%,
    100% {
        transform: rotate(-3.5deg);
    }
    50% {
        transform: rotate(3.5deg);
    }
}

@keyframes vt-step {
    0%,
    100% {
        transform: rotate(-20deg);
    }
    50% {
        transform: rotate(18deg);
    }
}

@keyframes vt-shade {
    0%,
    100% {
        transform: scale(1.18);
        opacity: 0.3;
    }
    50% {
        transform: scale(0.7);
        opacity: 0.12;
    }
}

@media (prefers-reduced-motion: reduce) {
    .vt *,
    .vt-shadow,
    .vt-bob,
    .vt-squash,
    .vt-head,
    .vt-ant,
    .vt-arm-l,
    .vt-arm-r,
    .vt-leg-a,
    .vt-leg-b {
        animation: none !important;
    }
}
</style>
