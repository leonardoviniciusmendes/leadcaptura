declare module '*.vue' {
  import type { DefineComponent } from 'vue';
  const component: DefineComponent<Record<string, unknown>, Record<string, unknown>, unknown>;
  export default component;
}

interface Window {
  dataLayer?: Array<Record<string, unknown>>;
}

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_CORRETOR_WHATSAPP?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
