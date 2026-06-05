import { request } from './api';

export const getDisponibilidade = () => request('mobile/disponibilidade');
export const updateDisponibilidade = (payload: unknown) => request('mobile/disponibilidade', { method: 'PUT', body: JSON.stringify(payload) });
export const getPreferencias = () => request('mobile/preferencias');
export const updatePreferencias = (payload: unknown) => request('mobile/preferencias', { method: 'PUT', body: JSON.stringify(payload) });
export const getRecomendacoes = (limite = 10) => request(`mobile/recomendacoes?limite=${limite}`);
