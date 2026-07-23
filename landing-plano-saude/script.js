const CONFIG = {
  // Quando sua API estiver pronta, informe a URL abaixo.
  // Exemplo: "https://api.seudominio.com/api/leads"
  apiUrl: "",

  // Informe somente números, com DDI e DDD.
  // Exemplo: "5521999999999"
  whatsappNumber: "552199695262"
};

const form = document.querySelector("#lead-form");
const message = document.querySelector("#form-message");
const submitButton = form.querySelector("button[type='submit']");

document.querySelector("#current-year").textContent = new Date().getFullYear();

function loadTrackingParameters() {
  const params = new URLSearchParams(window.location.search);
  ["utm_source", "utm_medium", "utm_campaign", "utm_term", "gclid"].forEach((key) => {
    const field = document.querySelector(`#${key}`);
    if (field) field.value = params.get(key) || "";
  });
}

function normalizePhone(value) {
  return value.replace(/\D/g, "");
}

function buildWhatsAppMessage(data) {
  return [
    "Olá! Gostaria de receber uma cotação de plano de saúde.",
    "",
    `Nome: ${data.nome}`,
    `WhatsApp: ${data.whatsapp}`,
    `Cidade: ${data.cidade}`,
    `Tipo de plano: ${data.tipoPlano}`,
    `Quantidade de vidas: ${data.vidas}`,
    `Idades: ${data.idades}`
  ].join("\n");
}

async function sendToApi(data) {
  const response = await fetch(CONFIG.apiUrl, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data)
  });

  if (!response.ok) {
    throw new Error(`Erro ao enviar lead: ${response.status}`);
  }
}

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  message.className = "form-message";
  message.textContent = "";

  const data = Object.fromEntries(new FormData(form).entries());
  data.whatsapp = normalizePhone(data.whatsapp);
  data.origem = "landing-page";
  data.pagina = window.location.href;
  data.criadoEm = new Date().toISOString();

  submitButton.disabled = true;
  submitButton.textContent = "Enviando...";

  try {
    if (CONFIG.apiUrl) {
      await sendToApi(data);
      message.classList.add("success");
      message.textContent = "Solicitação enviada. Em breve entraremos em contato.";
      form.reset();
      loadTrackingParameters();
      return;
    }

    if (CONFIG.whatsappNumber) {
      const text = encodeURIComponent(buildWhatsAppMessage(data));
      window.location.href = `https://wa.me/${CONFIG.whatsappNumber}?text=${text}`;
      return;
    }

    localStorage.setItem("ultimoLeadPlanoSaude", JSON.stringify(data));
    message.classList.add("success");
    message.textContent =
      "Página em modo de teste. Configure a API ou o WhatsApp no arquivo script.js.";
  } catch (error) {
    console.error(error);
    message.classList.add("error");
    message.textContent =
      "Não foi possível enviar agora. Tente novamente em alguns instantes.";
  } finally {
    submitButton.disabled = false;
    submitButton.textContent = "Receber cotação gratuita";
  }
});

loadTrackingParameters();
