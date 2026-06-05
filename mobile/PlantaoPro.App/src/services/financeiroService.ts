import { PagamentoMedico } from '../types/financeiro.types';
import { getPaged, request } from './api';

export const getMeusPagamentos = (page = 1, pageSize = 20) => getPaged<PagamentoMedico>('mobile/meus-pagamentos', page, pageSize);
export const getPagamento = (id: string) => request<PagamentoMedico>(`mobile/meus-pagamentos/${id}`);
