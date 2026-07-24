<template>
  <main class="page">
    <section class="page-header">
      <p class="eyebrow">Briefing simples</p>
      <h1>Nova campanha</h1>
      <p class="subtitle">Informe o essencial. A campanha será gerada automaticamente com conteúdo simulado nesta etapa.</p>
    </section>

    <form class="panel form-grid" @submit.prevent="submit">
      <label>
        Tipo de público
        <select v-model="form.tipoPublico" required>
          <option value="Individual">Individual</option>
          <option value="Casal">Casal</option>
          <option value="Familia">Família</option>
          <option value="Mei">MEI</option>
          <option value="Empresa">Empresa</option>
        </select>
      </label>

      <label>Cidade<input v-model.trim="form.cidade" required maxlength="120" /></label>
      <label>Estado<input v-model.trim="form.estado" required maxlength="2" placeholder="RJ" /></label>
      <label>Bairro ou região<input v-model.trim="form.regiao" maxlength="120" /></label>

      <label>
        Operadora
        <select v-model="form.operadora" required>
          <option>Nenhuma específica</option>
          <option>Amil</option>
          <option>Bradesco Saúde</option>
          <option>SulAmérica</option>
          <option>Unimed</option>
          <option>Outra</option>
        </select>
      </label>

      <label v-if="form.operadora === 'Outra'">Nome da operadora<input v-model.trim="form.operadoraOutra" required maxlength="80" /></label>
      <label>Orçamento diário<input v-model.number="form.orcamentoDiario" required min="1" step="0.01" type="number" /></label>
      <label class="wide">Objetivo ou observação<textarea v-model.trim="form.objetivo" maxlength="500" rows="4" /></label>

      <p v-if="error" class="error">{{ error }}</p>
      <div class="actions">
        <RouterLink to="/campanhas" class="button secondary">Cancelar</RouterLink>
        <button class="button" :disabled="loading">{{ loading ? 'Gerando...' : 'Gerar campanha' }}</button>
      </div>
    </form>
  </main>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { gerarCampanha, type GerarCampanhaRequest } from '../services/api';

const router = useRouter();
const loading = ref(false);
const error = ref('');
const form = reactive<GerarCampanhaRequest>({
  tipoPublico: 'Familia',
  cidade: '',
  estado: '',
  regiao: '',
  operadora: 'Nenhuma específica',
  operadoraOutra: '',
  orcamentoDiario: 20,
  objetivo: ''
});

async function submit() {
  if (loading.value) return;
  loading.value = true;
  error.value = '';

  try {
    const campanha = await gerarCampanha({
      ...form,
      estado: form.estado.toUpperCase(),
      regiao: form.regiao || undefined,
      operadoraOutra: form.operadora === 'Outra' ? form.operadoraOutra : undefined,
      objetivo: form.objetivo || undefined
    });
    router.push(`/campanhas/${campanha.id}`);
  } catch (err: unknown) {
    const response = err as { response?: { data?: { mensagem?: string } } };
    error.value = response.response?.data?.mensagem || 'Não foi possível gerar a campanha. Revise os dados e tente novamente.';
  } finally {
    loading.value = false;
  }
}
</script>
