import { ApiResponse, PagedResult } from '../types/auth.types';
import { PagamentoMedico } from '../types/financeiro.types';
import { getPaged, request } from './api';

type PagamentoApiDto = PagamentoMedico & {
  pagamentoId?: string;
  valorPrevisto?: number;
  valorPago?: number | null;
  dataPlantao?: string;
  dataPrevista?: string;
  dataPagamento?: string | null;
  formaPagamento?: string;
  hospitalNome?: string;
  especialidadeNome?: string;
};

function normalizePagamento(item: PagamentoApiDto): PagamentoMedico {
  const valor = item.valor ?? item.valorPago ?? item.valorPrevisto ?? 0;

  return {
    ...item,
    id: item.id ?? item.pagamentoId ?? '',
    descricao: item.descricao ?? ([item.hospitalNome, item.especialidadeNome].filter(Boolean).join(' • ') || undefined),
    competencia: item.competencia ?? item.dataPlantao,
    valor,
    vencimento: item.vencimento ?? item.dataPrevista,
    pagoEm: item.pagoEm ?? item.dataPagamento ?? null,
  };
}

function mapPagedPagamentos(response: ApiResponse<PagedResult<PagamentoApiDto>>): ApiResponse<PagedResult<PagamentoMedico>> {
  if (!response.data) {
    return response as unknown as ApiResponse<PagedResult<PagamentoMedico>>;
  }

  return {
    ...response,
    data: {
      ...response.data,
      items: (response.data.items ?? []).map(normalizePagamento),
    },
  };
}

export async function getMeusPagamentos(page = 1, pageSize = 20) {
  const response = await getPaged<PagamentoApiDto>('mobile/meus-pagamentos', page, pageSize);
  return mapPagedPagamentos(response);
}

export async function getPagamento(id: string) {
  const response = await request<PagamentoApiDto>(`mobile/meus-pagamentos/${id}`);
  return response.data ? { ...response, data: normalizePagamento(response.data) } : response as ApiResponse<PagamentoMedico>;
}


export async function contestarPagamentoV115(id: string, motivo: string) {
  return request<Record<string, unknown>>(`v115/repasses-medicos/${id}/contestar`, { method: 'POST', body: JSON.stringify({ motivo }) });
}

export async function responderContestacaoV115(id: string, motivo: string) {
  return request<Record<string, unknown>>(`v115/repasses-medicos/${id}/resolver`, { method: 'POST', body: JSON.stringify({ motivo }) });
}

export async function getRepassesV115(page = 1, pageSize = 20) {
  const response = await getPaged<PagamentoApiDto>('v115/repasses-medicos', page, pageSize);
  return mapPagedPagamentos(response);
}

export async function getDashboardMedicoV116() {
  return request<Record<string, unknown>>('v116/relatorios/operacional');
}

export async function getTimelineFinanceiraV116(repasseId: string) {
  return request<Record<string, unknown>>(`v116/timelines/repasse/${repasseId}`);
}

export async function getNotificacoesOperacionaisV116() {
  return request<Record<string, unknown>>('v116/notificacoes-operacionais');
}

export async function registrarContestacaoHistoricoV116(repasseId: string, motivo: string) {
  return request<Record<string, unknown>>(`v116/timelines/repasse/${repasseId}/comentario`, { method: 'POST', body: JSON.stringify({ observacao: motivo }) });
}
