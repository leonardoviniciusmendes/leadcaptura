<template>
  <form class="lead-form" @submit.prevent="submit">
    <label>Nome<input v-model.trim="form.nome" required maxlength="150" autocomplete="name" /></label>
    <label>WhatsApp<input :value="form.whatsApp" required inputmode="tel" autocomplete="tel" @input="onPhone" /></label>
    <label>CEP<input :value="form.cep" inputmode="numeric" autocomplete="postal-code" @input="onCep" /></label>
    <label>Quantidade de pessoas<input v-model.number="form.quantidadeVidas" required min="1" max="20" type="number" /></label>

    <div class="ages">
      <div class="label-row">
        <span>Idades</span>
        <button type="button" @click="addAge" :disabled="form.idades.length >= 20">Adicionar</button>
      </div>
      <label v-for="(_, index) in form.idades" :key="index" class="age-row">
        <span>Idade {{ index + 1 }}</span>
        <input v-model.number="form.idades[index]" min="0" max="120" type="number" />
        <button type="button" @click="removeAge(index)" aria-label="Remover idade">x</button>
      </label>
    </div>

    <label>Hospital desejado<input v-model.trim="form.hospitalDesejado" maxlength="120" /></label>
    <label>Operadora desejada<input v-model.trim="form.operadoraDesejada" maxlength="120" /></label>
    <label class="checkbox"><input v-model="form.possuiPlanoAtual" type="checkbox" /> Possui plano atual?</label>
    <label v-if="form.possuiPlanoAtual">Nome do plano atual<input v-model.trim="form.planoAtual" maxlength="120" /></label>
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
import { digits, maskCep, maskPhone, type TipoLead } from './forms';
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
  cep: '',
  quantidadeVidas: 1,
  idades: [30] as number[],
  hospitalDesejado: '',
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

function onCep(event: Event) {
  form.cep = maskCep((event.target as HTMLInputElement).value);
}

function addAge() {
  if (form.idades.length < 20) form.idades.push(0);
}

function removeAge(index: number) {
  form.idades.splice(index, 1);
}

async function submit() {
  if (loading.value) return;
  loading.value = true;
  error.value = '';
  try {
    const response = await enviarLead({
      tipo: props.tipo,
      nome: form.nome,
      whatsApp: digits(form.whatsApp),
      cep: digits(form.cep),
      quantidadeVidas: Number(form.quantidadeVidas),
      idades: form.idades.map(Number),
      hospitalDesejado: form.hospitalDesejado || undefined,
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
