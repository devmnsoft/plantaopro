import React, { useEffect, useState } from 'react';
import { Text } from 'react-native';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import EmptyState from '../../components/EmptyState';
import { NotificationItem } from '../../types/notification.types';
import { getNotifications } from '../../services/notificationService';
export default function NotificationsScreen() { const [items, setItems] = useState<NotificationItem[]>([]); useEffect(() => { getNotifications().then((r) => setItems(r.data?.items ?? [])); }, []); return <ScreenContainer><Header title="Notificações" />{items.length ? items.map((n) => <Text key={n.id}>{n.titulo ?? 'Aviso'} - {n.mensagem}</Text>) : <EmptyState title="Sem notificações" />}</ScreenContainer>; }
