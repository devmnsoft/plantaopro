import { NotificationItem } from '../types/notification.types';
import { getPaged, request } from './api';

export const getNotifications = (page = 1, pageSize = 20) => getPaged<NotificationItem>('mobile/notificacoes', page, pageSize);
export const getUnreadCount = () => request<{ total: number }>('mobile/notificacoes/contador');
export const markAsRead = (id: string) => request(`mobile/notificacoes/${id}/lida`, { method: 'PUT' });
export const markAllAsRead = () => request('mobile/notificacoes/lidas', { method: 'PUT' });
