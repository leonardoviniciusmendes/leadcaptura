<template>
  <section class="empty-state">
    <strong>{{ title }}</strong>
    <p>{{ message }}</p>
    <slot />
  </section>
</template>

<script setup lang="ts">
defineProps<{ title: string; message: string }>();
</script>
