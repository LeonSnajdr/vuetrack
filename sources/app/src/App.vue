<template>
    <VApp>
        <VMain class="h-screen nav-left">
            <AppNotificationView />
            <AppHeader />
            <RouterView />
        </VMain>
    </VApp>
</template>

<script lang="ts" setup></script>

<style lang="scss">
@use "@/styles/settings";
@use "@/styles/global";

$header-height: 48px;
$container-padding: settings.$container-padding-x;

.v-main {
    &.nav-left {
        margin-left: $container-padding;
        width: calc(100% - #{$container-padding}) !important;

        .v-navigation-drawer {
            &--left {
                margin-left: $container-padding;
                margin-top: $container-padding;
                height: calc(100% - #{$header-height + $container-padding * 2}) !important;
            }
        }
    }

    &.nav-right {
        margin-right: $container-padding;
        width: calc(100% - #{$container-padding}) !important;

        .v-navigation-drawer {
            &--right {
                margin-right: $container-padding;
                margin-top: $container-padding;
                height: calc(100% - #{$header-height + $container-padding * 2}) !important;
            }
        }
    }

    &.nav-left.nav-right {
        width: calc(100% - #{$container-padding * 2}) !important;
    }

    .v-app-bar {
        margin-left: $container-padding;
        width: calc(100% - #{$container-padding * 2}) !important;
    }
}
</style>
