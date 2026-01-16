# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

- `yarn dev` - Start development server (runs on port 3000)
- `yarn build` - Build for production (runs type-check + build-only)
- `yarn type-check` - Run TypeScript type checking
- `yarn lint` - Run ESLint on all supported file types
- `yarn lint:style` - Run Stylelint on CSS/SCSS/Vue files
- `yarn format` - Format code with Prettier
- `yarn cspell` - Check spelling

## Architecture Overview

This is a Vue 3 time tracking application built with:

### Tech Stack
- **Vue 3** with Composition API and `<script setup>`
- **Vuetify 3** for Material Design UI components
- **Pinia** for state management
- **Vue Router** with auto-imports via unplugin-vue-router
- **TypeScript** with strict typing
- **Vite** as build tool
- **Vue i18n** for internationalization (English/German)

### Core Architecture

**Auto-imports**: The project uses extensive auto-imports via unplugin-auto-import:
- Vue composables, Pinia stores, Vue i18n, VueUse utilities
- All files in `src/contracts/`, `src/stores/`, `src/services/`, `src/composables/`
- Material Design Icons from @mdi/js
- UUID v4 as `uuidv4`

**State Management Pattern**:
- Stores in `src/stores/` follow the composition API pattern with `defineStore()`
- Services in `src/services/` handle data operations (currently mock implementations)
- Contracts in `src/contracts/` define TypeScript interfaces using branded types
- Example: `TimeEntryService` provides CRUD operations, `useTimeEntryStore` manages state

**Component Structure**:
- Components auto-register via unplugin-vue-components with directory-as-namespace
- Layout system using vite-plugin-vue-layouts-next with default layout
- Main app structure: `App.vue` → `AppHeader` + `RouterView`

**TypeScript Configuration**:
- Uses branded types for type safety (see `typings/brand.d.ts`)
- Auto-generated type definitions for components, router, and auto-imports
- Separate tsconfig files for app, node, and vitest

**Styling**:
- SCSS with Vuetify theme customization in `src/styles/settings.scss`
- Global styles in `src/styles/global.scss`
- Custom font loading (Nunito) with local woff/woff2 files

### Key Patterns

**Service Layer**: Services are singleton classes providing async CRUD operations with 3-second mock delays. They return strongly-typed contracts.

**Store Pattern**: Stores use `useAsyncState` from VueUse for reactive async data loading and provide methods for CRUD operations that update local state and call services.

**Contract System**: All data interfaces are defined as contracts with branded types for IDs (e.g., `TimeEntryId`) to prevent type mixing.