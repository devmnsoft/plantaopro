import React, { useEffect, useState } from 'react';
import { Text, View } from 'react-native';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import CardResumo from '../../components/CardResumo';
import EmptyState from '../../components/EmptyState';
import { getMedicoDashboardV114 } from '../../services/v114Service';

export default function HomeScreen() {
  const [message, setMessage] = useState('Carregando dashboard mobile...');
  useEffect(() => { getMedicoDashboardV114().then((r) => setMessage(r.success ? `${r.message} Atalhos: ${(r.data?.atalhos ?? []).join(', ')}` : `Fallback: ${r.message}`)); }, []);
  return <ScreenContainer><Header title="PlantãoPro" subtitle="Dashboard médico v1.14 com plantões, convites, pagamentos e atalhos" /><View style={{ flexDirection: 'row', gap: 12 }}><CardResumo label="Plantões" value="--" /><CardResumo label="Notificações" value="--" /></View><Text>{message}</Text><EmptyState title="Dados em tempo real" message="Ao autenticar, o app consome os endpoints JWT /api/mobile com paginação e fallback amigável." /></ScreenContainer>;
}
