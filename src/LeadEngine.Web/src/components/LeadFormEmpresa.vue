<template>
  <form class="lead-form" @submit.prevent="submit">
    <label>Nome do responsavel<input v-model.trim="form.nome" required maxlength="150" autocomplete="name" /></label>
    <label>WhatsApp<input :value="form.whatsApp" required inputmode="tel" autocomplete="tel" @input="onPhone" /></label>
    <label>E-mail<input v-model.trim="form.email" type="email" maxlength="180" autocomplete="email" /></label>
    <label>Nome da empresa<input v-model.trim="form.nomeEmpresa" required maxlength="180" /></label>
    <label>CNPJ<input :value="form.cnpj" inputmode="numeric" @input="onCnpj" /></label>
    <label>Cidade ou CEP<input v-model.trim="form.cidadeOuCep" maxlength="120" /></label>
    <label>Funcionarios ou vidas<input v-model.number="form.quantidadeFuncionarios" required min="1" type="number" /></label>
    <label>Operadora desejada<input v-model.trim="form.operadoraDesejada" maxlength="120" /></label>
    <label class="checkbox"><input v-model="form.possuiPlanoAtual" type="checkbox" /> Possui plano empresarial atual?</label>
    <label v-if="form.possuiPlanoAtual">Plano atual<input v-model.trim="form.planoAtual" maxlength="120" /></label>
    <input v-model="form.honeypot" class="hidden-field" tabindex="-1" autocomplete="off" aria-hidden="true" />
    <label class="checkbox consent"><input v-model="form.consentimentoContato" required type="checkbox" /> Autorizo o contato por telefone e WhatsApp sobre esta solicitacao.</label>
    <PrivacyNotice />
    <p v-if="error" class="error">{{ error }}</p>
    <button class="submit" type="submit" :disabled="loading">{{ loading ? 'Enviando...' : 'Receber cotacao' }}</button>
  </form>
</template>

<script setup lang="ts">
import { reactive, ref, watch } from 'vue';
import PrivacyNotice from './PrivacyNotice.vue';
import { digits, maskCnpj, maskPhone, type TipoLead } from './forms';
import { buildOrigem, pushDataLayer } from '../services/tracking';
import { enviarLead, type CapturaLeadResponse } from '../services/api';

const props = defineProps<{ tipo: TipoLead; operadora?: string }>();
const emit = defineEmits<{ success: [CapturaLeadResponse, TipoLead] }>();
const loading = ref(false);
const error = ref('');
const started = ref(false);

const form = reactive({
  nome: '',
  whatsApp: '',
  email: '',
  nomeEmpresa: '',
  cnpj: '',
  cidadeOuCep: '',
  quantidadeFuncionarios: 2,
  operadoraDesejada: props.operadora || '',
  possuiPlanoAtual: false,
  planoAtual: '',
  consentimentoContato: false,
  honeypot: ''
});

watch(form, () => {
  if (!started.value) {
    started.value = true;
    pushDataLayer({ event: 'lead_form_start', leadType: props.tipo });
  }
});

function onPhone(event: Event) {
  form.whatsApp = maskPhone((event.target as HTMLInputElement).value);
}

function onCnpj(event: Event) {
  form.cnpj = maskCnpj((event.target as HTMLInputElement).value);
}

async function submit() {
  if (loading.value) return;
  loading.value = true;
  error.value = '';
  const cepCandidate = digits(form.cidadeOuCep);
  try {
    const response = await enviarLead({
      tipo: props.tipo,
      nome: form.nome,
      whatsApp: digits(form.whatsApp),
      email: form.email || undefined,
      nomeEmpresa: form.nomeEmpresa,
      cnpj: digits(form.cnpj) || undefined,
      cidade: cepCandidate.length === 8 ? undefined : form.cidadeOuCep,
      cep: cepCandidate.length === 8 ? cepCandidate : undefined,
      quantidadeFuncionarios: Number(form.quantidadeFuncionarios),
      quantidadeVidas: Number(form.quantidadeFuncionarios),
      operadoraDesejada: form.operadoraDesejada || undefined,
      possuiPlanoAtual: form.possuiPlanoAtual,
      planoAtual: form.planoAtual || undefined,
      consentimentoContato: form.consentimentoContato,
      honeypot: form.honeypot,
      origem: buildOrigem()
    });
    emit('success', response, props.tipo);
  } catch {
    pushDataLayer({ event: 'lead_form_error', errorType: 'validation' });
    error.value = 'Nao foi possivel enviar agora. Confira os dados e tente novamente.';
  } finally {
    loading.value = false;
  }
}
</script>
