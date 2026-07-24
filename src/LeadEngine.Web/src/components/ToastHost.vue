<template>
  <div class="toast-stack" aria-live="polite">
    <article v-for="toast in toasts" :key="toast.id" class="toast" :class="toast.type">
      <strong>{{ toast.title }}</strong>
      <span v-if="toast.message">{{ toast.message }}</span>
    </article>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue';
import { onToast, type ToastMessage } from './uiEvents';

const toasts = ref<ToastMessage[]>([]);
let unsubscribe: (() => void) | null = null;

onMounted(() => {
  unsubscribe = onToast((toast) => {
    toasts.value = [toast, ...toasts.value].slice(0, 4);
    window.setTimeout(() => {
      toasts.value = toasts.value.filter((item) => item.id !== toast.id);
    }, 3600);
  });
});

onUnmounted(() => unsubscribe?.());
</script>
