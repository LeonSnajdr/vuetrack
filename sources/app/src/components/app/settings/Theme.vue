<template>
    <VCard class="border" elevation="0">
        <VCardTitle>
            <VIcon :icon="mdiShapeOutline" />
            {{ $t("settings.general.theme") }}
        </VCardTitle>
        <VCardText>
            <VRow>
                <VCol v-for="item in themeItems" :key="item.theme" :cols="item.cols">
                    <VCard
                        @click="selectedTheme = item.theme"
                        :class="item.theme === selectedTheme ? 'border border-info border-opacity-100' : 'border'"
                        :theme="item.theme"
                        density="compact"
                        elevation="0"
                    >
                        <VCardTitle>
                            <VIcon :color="item.color" :icon="item.icon" />
                            {{ item.title }}
                        </VCardTitle>
                        <VCardText>
                            {{ item.description }}
                        </VCardText>
                    </VCard>
                </VCol>
            </VRow>
        </VCardText>
    </VCard>
</template>

<script setup lang="ts">
const selectedTheme = defineModel<string>({ required: true });
const { t } = useI18n();

const themeItems = [
    {
        title: t("settings.general.theme.system"),
        description: t("settings.general.theme.system.description"),
        theme: "system",
        icon: mdiLaptop,
        outlined: true,
        cols: 12
    },
    {
        title: t("settings.general.theme.dark"),
        description: t("settings.general.theme.dark.description"),
        theme: "dark",
        icon: mdiWeatherNight,
        color: "secondary",
        cols: 6
    },
    {
        title: t("settings.general.theme.light"),
        description: t("settings.general.theme.light.description"),
        theme: "light",
        icon: mdiWeatherSunny,
        color: "success",
        cols: 6
    }
];
</script>
