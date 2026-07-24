<template>
  <RouterView v-if="isPublic" />
  <AppShell v-else />
  <ToastHost />
  <ConfirmDialog />
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import AppShell from './components/AppShell.vue';
import ConfirmDialog from './components/ConfirmDialog.vue';
import ToastHost from './components/ToastHost.vue';

const route = useRoute();
const isPublic = computed(() => Boolean(route.meta.public));
</script>
