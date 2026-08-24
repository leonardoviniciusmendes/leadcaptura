<template>
  <main class="login-page">
    <section class="login-panel panel">
      <p class="eyebrow">LeadEngine</p>
      <h1>Entrar</h1>
      <p class="subtitle">Acesse o painel administrativo.</p>

      <form class="login-form" @submit.prevent="submit">
        <label>E-mail<input v-model.trim="email" type="email" autocomplete="username" required /></label>
        <label>Senha<input v-model="password" type="password" autocomplete="current-password" required /></label>
        <p v-if="error" class="error">{{ error }}</p>
        <button class="button" :disabled="loading">{{ loading ? 'Entrando...' : 'Entrar' }}</button>
      </form>
    </section>
  </main>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { login } from '../services/api';
import { setCurrentUser } from '../services/auth';

const route = useRoute();
const router = useRouter();
const email = ref('');
const password = ref('');
const loading = ref(false);
const error = ref('');

async function submit() {
  if (loading.value) return;
  loading.value = true;
  error.value = '';
  try {
    const user = await login({ email: email.value, password: password.value });
    setCurrentUser(user);
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/';
    await router.replace(redirect);
  } catch {
    error.value = 'Credenciais invalidas.';
  } finally {
    loading.value = false;
  }
}
</script>
