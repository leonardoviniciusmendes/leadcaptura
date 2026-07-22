<template>
  <ul class="benefits" aria-label="Beneficios">
    <li v-for="benefit in beneficios" :key="benefit">
      <span aria-hidden="true">✓</span>
      {{ benefit }}
    </li>
  </ul>
</template>

<script setup lang="ts">
defineProps<{ beneficios: string[] }>();
</script>
