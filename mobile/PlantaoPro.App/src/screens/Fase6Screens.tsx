import React from 'react';
import { RefreshControl, ScrollView, Text, View } from 'react-native';

const Card = ({ title, description }: { title: string; description: string }) => (
  <View style={{ padding: 16, borderRadius: 12, backgroundColor: '#ffffff', marginBottom: 12 }}>
    <Text style={{ fontSize: 18, fontWeight: '700' }}>{title}</Text>
    <Text style={{ marginTop: 8, color: '#475569' }}>{description}</Text>
  </View>
);

export const MedicoHomeScreen = () => (
  <ScrollView refreshControl={<RefreshControl refreshing={false} onRefresh={() => undefined} />} style={{ padding: 16, backgroundColor: '#f8fafc' }}>
    <Card title="Agenda médica" description="Plantões, consultas e convites do médico autenticado." />
    <Card title="Pagamentos" description="Resumo financeiro sem exposição de dados clínicos." />
  </ScrollView>
);

export const ClinicaHomeScreen = () => (
  <ScrollView refreshControl={<RefreshControl refreshing={false} onRefresh={() => undefined} />} style={{ padding: 16, backgroundColor: '#f8fafc' }}>
    <Card title="Fila clínica" description="Check-ins, triagens e chamadas do dia." />
    <Card title="Notificações" description="Mensagens genéricas; detalhes exigem sessão autenticada." />
  </ScrollView>
);

export const NotificationsScreen = () => (
  <View style={{ padding: 16 }}>
    <Text style={{ fontSize: 20, fontWeight: '700' }}>Notificações</Text>
    <Text style={{ marginTop: 8 }}>Nenhuma notificação no momento.</Text>
  </View>
);
