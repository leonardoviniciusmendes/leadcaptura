<template>
  <div class="skeleton-group">
    <span v-for="item in count" :key="item" class="skeleton-line" />
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{ count?: number }>(), { count: 3 });
</script>
