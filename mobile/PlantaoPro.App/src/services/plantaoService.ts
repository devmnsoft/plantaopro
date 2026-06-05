import { ConvitePlantao, Plantao } from '../types/plantao.types';
import { getPaged, request } from './api';

export const getPlantoesDisponiveis = (page = 1, pageSize = 20) => getPaged<Plantao>('mobile/plantoes-disponiveis', page, pageSize);
export const getMinhasEscalas = (page = 1, pageSize = 20) => getPaged<Plantao>('mobile/minhas-escalas', page, pageSize);
export const getPlantao = (id: string) => request<Plantao>(`mobile/plantoes/${id}`);
export const solicitarPlantao = (id: string) => request(`mobile/plantoes/${id}/solicitar`, { method: 'POST' });
export const getConvites = (page = 1, pageSize = 20) => getPaged<ConvitePlantao>('mobile/convites', page, pageSize);
export const aceitarConvite = (id: string) => request(`mobile/convites/${id}/aceitar`, { method: 'POST' });
export const recusarConvite = (id: string, motivo: string) => request(`mobile/convites/${id}/recusar`, { method: 'POST', body: JSON.stringify({ motivo }) });
