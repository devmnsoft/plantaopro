import { ApiResponse, PagedResult } from '../types/auth.types';
import { ConvitePlantao, Plantao } from '../types/plantao.types';
import { getPaged, request } from './api';

type PlantaoApiDto = Plantao & {
  plantaoId?: string;
  escalaId?: string;
  vagasDisponiveis?: number;
};

type ConviteApiDto = ConvitePlantao & {
  plantaoId: string;
};

function mapPagedItems<TIn, TOut>(response: ApiResponse<PagedResult<TIn>>, mapper: (item: TIn) => TOut): ApiResponse<PagedResult<TOut>> {
  if (!response.data) {
    return response as unknown as ApiResponse<PagedResult<TOut>>;
  }

  return {
    ...response,
    data: {
      ...response.data,
      items: (response.data.items ?? []).map(mapper),
    },
  };
}

function normalizePlantao(item: PlantaoApiDto): Plantao {
  return {
    ...item,
    id: item.id ?? item.plantaoId ?? item.escalaId ?? '',
    vagas: item.vagas ?? item.vagasDisponiveis,
  };
}

function normalizeConvite(item: ConviteApiDto): ConvitePlantao {
  return {
    ...normalizePlantao(item),
    ...item,
    id: item.id,
    plantaoId: item.plantaoId,
  };
}

export async function getPlantoesDisponiveis(page = 1, pageSize = 20) {
  const response = await getPaged<PlantaoApiDto>('mobile/plantoes-disponiveis', page, pageSize);
  return mapPagedItems(response, normalizePlantao);
}

export async function getMinhasEscalas(page = 1, pageSize = 20) {
  const response = await getPaged<PlantaoApiDto>('mobile/minhas-escalas', page, pageSize);
  return mapPagedItems(response, normalizePlantao);
}

export async function getPlantao(id: string) {
  const response = await request<PlantaoApiDto>(`mobile/plantoes/${id}`);
  return response.data ? { ...response, data: normalizePlantao(response.data) } : response as ApiResponse<Plantao>;
}

export const solicitarPlantao = (id: string) => request(`mobile/plantoes/${id}/solicitar`, { method: 'POST' });

export async function getConvites(page = 1, pageSize = 20) {
  const response = await getPaged<ConviteApiDto>('mobile/convites', page, pageSize);
  return mapPagedItems(response, normalizeConvite);
}

export const aceitarConvite = (id: string) => request(`mobile/convites/${id}/aceitar`, { method: 'POST' });
export const recusarConvite = (id: string, motivo: string) => request(`mobile/convites/${id}/recusar`, { method: 'POST', body: JSON.stringify({ motivo }) });
