declare const process: { env?: Record<string, string | undefined> };
import { ApiResponse, PagedResult } from '../types/auth.types';
import storage from '../utils/storage';

export const apiBaseUrl = (typeof process !== 'undefined' ? process.env?.EXPO_PUBLIC_API_BASE_URL : undefined) ?? '';
const tokenKey = 'plantaopro.jwt';

function lowerFirst(key: string) { return key.length ? key.charAt(0).toLowerCase() + key.slice(1) : key; }

export function normalizeKeys<T>(value: unknown): T {
  if (Array.isArray(value)) return value.map((item) => normalizeKeys(item)) as T;
  if (value && typeof value === 'object') {
    return Object.entries(value as Record<string, unknown>).reduce((acc, [key, item]) => {
      acc[lowerFirst(key)] = normalizeKeys(item);
      return acc;
    }, {} as Record<string, unknown>) as T;
  }
  return value as T;
}

export function emptyPagedResult<T>(page = 1, pageSize = 20): PagedResult<T> {
  return { items: [], page, pageSize, totalItems: 0, total: 0, totalPages: 0 };
}

export function friendlyFallback<T>(message: string, statusCode = 404, data: T | null = null): ApiResponse<T> {
  return { success: false, message, data, errors: [message], statusCode, timestamp: new Date().toISOString() };
}

async function readJson(response: Response) {
  const text = await response.text();
  if (!text) return null;
  try { return JSON.parse(text); } catch { return { message: text }; }
}

export async function getToken() { return storage.getItem(tokenKey); }
export async function setToken(token: string) { await storage.setItem(tokenKey, token); }
export async function clearToken() { await storage.removeItem(tokenKey); }

export async function request<T>(path: string, options: RequestInit = {}): Promise<ApiResponse<T>> {
  const token = await getToken();
  const headers: Record<string, string> = { 'Content-Type': 'application/json', ...(options.headers as Record<string, string> | undefined) };
  if (token) headers.Authorization = `Bearer ${token}`;

  try {
    if (!apiBaseUrl) return friendlyFallback<T>('Configure EXPO_PUBLIC_API_BASE_URL para conectar o mobile médico.', 0);
    const response = await fetch(`${apiBaseUrl.replace(/\/+$/, '')}/${path.replace(/^\/+/, '')}`, { ...options, headers });
    const payload = normalizeKeys<ApiResponse<T> | T | null>(await readJson(response));

    if (payload && typeof payload === 'object' && 'success' in payload) {
      const apiResponse = payload as ApiResponse<T>;
      return { ...apiResponse, statusCode: apiResponse.statusCode ?? response.status };
    }

    if (!response.ok) {
      const message = (payload as { message?: string } | null)?.message ?? (response.status === 404 ? 'Endpoint mobile não disponível nesta versão.' : 'Não foi possível concluir a operação.');
      return friendlyFallback<T>(message, response.status);
    }

    return { success: true, message: 'Sucesso', data: payload as T, statusCode: response.status, timestamp: new Date().toISOString() };
  } catch (error) {
    return friendlyFallback<T>(error instanceof Error ? error.message : 'Falha de comunicação com a API mobile.', 0);
  }
}

export async function getPaged<T>(path: string, page = 1, pageSize = 20): Promise<ApiResponse<PagedResult<T>>> {
  const separator = path.includes('?') ? '&' : '?';
  const response = await request<PagedResult<T>>(`${path}${separator}page=${page}&pageSize=${pageSize}`);
  if (!response.success || !response.data) {
    return { ...response, data: emptyPagedResult<T>(page, pageSize) };
  }
  return { ...response, data: { ...emptyPagedResult<T>(page, pageSize), ...response.data, items: response.data.items ?? [] } };
}
