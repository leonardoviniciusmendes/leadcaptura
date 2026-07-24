<template>
  <div v-if="payload" class="modal-backdrop">
    <section class="panel confirm-dialog" role="dialog" aria-modal="true">
      <h2>{{ payload.title }}</h2>
      <p>{{ payload.message }}</p>
      <div class="actions">
        <button class="button secondary" @click="answer(false)">{{ payload.cancelLabel || 'Cancelar' }}</button>
        <button class="button" @click="answer(true)">{{ payload.confirmLabel || 'Confirmar' }}</button>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue';
import { onConfirm } from './uiEvents';

type Payload = Parameters<Parameters<typeof onConfirm>[0]>[0];

const payload = ref<Payload | null>(null);
let unsubscribe: (() => void) | null = null;

onMounted(() => {
  unsubscribe = onConfirm((next) => {
    payload.value = next;
  });
});

onUnmounted(() => unsubscribe?.());

function answer(value: boolean) {
  payload.value?.resolve(value);
  payload.value = null;
}
</script>
