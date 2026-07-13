import { request } from './api';

export type MedicoDashboardV114 = {
  dashboard?: string;
  proximosPlantoes?: number;
  convites?: number;
  pagamentos?: number;
  pagamentosPendentes?: number;
  pagamentosConfirmados?: number;
  repassesPrevistos?: number;
  contestacoes?: number;
  atalhos?: string[];
};

export function getMedicoDashboardV114() {
  return request<MedicoDashboardV114>('v114/mobile/medico/dashboard');
}

export function aceitarConviteV114(id: string) {
  return request<Record<string, unknown>>(`mobile/convites/${id}/aceitar`, { method: 'POST' });
}

export function recusarConviteV114(id: string, motivo: string) {
  return request<Record<string, unknown>>(`mobile/convites/${id}/recusar`, { method: 'POST', body: JSON.stringify({ motivo }) });
}
