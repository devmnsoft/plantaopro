import { LoginRequest, LoginResponse, UserProfile } from '../types/auth.types';
import { clearToken, request, setToken } from './api';

export async function login(payload: LoginRequest) {
  const response = await request<LoginResponse>('mobile/auth/login', { method: 'POST', body: JSON.stringify(payload) });
  if (response.success && response.data?.token) await setToken(response.data.token);
  return response;
}

export async function logout() {
  const response = await request('mobile/auth/logout', { method: 'POST' });
  await clearToken();
  return response;
}

export const getMe = () => request<UserProfile>('mobile/me');
export const getProfile = () => request<UserProfile>('mobile/perfil');
export const updateProfile = (profile: Partial<UserProfile>) => request<UserProfile>('mobile/perfil', { method: 'PUT', body: JSON.stringify(profile) });
