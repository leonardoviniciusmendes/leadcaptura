import axios from 'axios';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080',
  timeout: 15000
});

export interface CapturaLeadResponse {
  sucesso: boolean;
  leadId: string;
  mensagem: string;
  duplicado?: boolean;
}

export async function enviarLead(payload: unknown): Promise<CapturaLeadResponse> {
  const { data } = await api.post<CapturaLeadResponse>('/api/leads', payload);
  return data;
}
