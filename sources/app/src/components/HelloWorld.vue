<template>
  <VContainer
    class="fill-height"
    maxWidth="900"
  >
    <div>
      <VImg
        class="mb-4"
        height="150"
        src="@/assets/logo.png"
      />

      <div class="mb-8 text-center">
        <div class="text-body-2 font-weight-light mb-n1">
          Welcome to
        </div>
        <h1 class="text-h2 font-weight-bold">
          Vuetify
        </h1>
      </div>

      <VRow>
        <VCol cols="12">
          <VCard
            class="py-4"
            color="surface-variant"
            image="https://cdn.vuetifyjs.com/docs/images/one/create/feature.png"
            prependIcon="mdi-rocket-launch-outline"
            rounded="lg"
            variant="tonal"
          >
            <template #image>
              <VImg position="top right" />
            </template>

            <template #title>
              <h2 class="text-h5 font-weight-bold">
                Get started
              </h2>
            </template>

            <template #subtitle>
              <div class="text-subtitle-1">
                Change this page by updating <VKbd>{{ `<HelloWorld />` }}</VKbd> in <VKbd>components/HelloWorld.vue</VKbd>.
              </div>
            </template>
          </VCard>
        </VCol>

        <VCol
          v-for="link in links"
          :key="link.href"
          cols="6"
        >
          <VCard
            :href="link.href"
            :prependIcon="link.icon"
            :subtitle="link.subtitle"
            :title="link.title"
            appendIcon="mdi-open-in-new"
            class="py-4"
            color="surface-variant"
            rel="noopener noreferrer"
            rounded="lg"
            target="_blank"
            variant="tonal"
          />
        </VCol>
      </VRow>
    </div>
  </VContainer>
</template>

<script setup lang="ts">
  const links = [
    {
      href: 'https://vuetifyjs.com/',
      icon: 'mdi-text-box-outline',
      subtitle: 'Learn about all things Vuetify in our documentation.',
      title: 'Documentation',
    },
    {
      href: 'https://vuetifyjs.com/introduction/why-vuetify/#feature-guides',
      icon: 'mdi-star-circle-outline',
      subtitle: 'Explore available framework Features.',
      title: 'Features',
    },
    {
      href: 'https://vuetifyjs.com/components/all',
      icon: 'mdi-widgets-outline',
      subtitle: 'Discover components in the API Explorer.',
      title: 'Components',
    },
    {
      href: 'https://discord.vuetifyjs.com',
      icon: 'mdi-account-group-outline',
      subtitle: 'Connect with Vuetify developers.',
      title: 'Community',
    },
  ]
</script>
