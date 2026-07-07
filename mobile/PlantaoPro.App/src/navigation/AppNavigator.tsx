import React, { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { useAuth } from '../context/AuthContext';
import LoginScreen from '../screens/auth/LoginScreen';
import HomeScreen from '../screens/home/HomeScreen';
import PlantaoListScreen from '../screens/plantoes/PlantaoListScreen';
import MyPlantoesScreen from '../screens/plantoes/MyPlantoesScreen';
import FinanceiroScreen from '../screens/financeiro/FinanceiroScreen';
import NotificationsScreen from '../screens/notificacoes/NotificationsScreen';
import ProfileScreen from '../screens/perfil/ProfileScreen';
import ScreenContainer from '../components/ScreenContainer';
import Header from '../components/Header';
import EmptyState from '../components/EmptyState';
import ButtonPrimary from '../components/ButtonPrimary';
import colors from '../theme/colors';

type RouteKey = 'home' | 'plantoes' | 'convites' | 'conviteDetalhe' | 'escalas' | 'pagamentos' | 'notificacoes' | 'perfil' | 'disponibilidade' | 'preferencias';

const routes: { key: RouteKey; label: string }[] = [
  { key: 'home', label: 'Início' },
  { key: 'plantoes', label: 'Plantões' },
  { key: 'convites', label: 'Convites' },
  { key: 'escalas', label: 'Escalas' },
  { key: 'pagamentos', label: 'Pagamentos' },
  { key: 'notificacoes', label: 'Notificações' },
  { key: 'perfil', label: 'Perfil' },
  { key: 'disponibilidade', label: 'Disponibilidade' },
  { key: 'preferencias', label: 'Preferências' },
];

function PartialScreen({ title, message }: { title: string; message: string }) {
  return <ScreenContainer><Header title={title} subtitle="MVP mobile médico" /><EmptyState title="Fluxo parcial" message={message} /></ScreenContainer>;
}

function ConvitesScreen({ openDetail }: { openDetail: () => void }) {
  return <ScreenContainer><Header title="Convites" subtitle="Aceite ou recuse convites com confirmação visual." /><EmptyState title="Nenhum convite pendente" message="A lista consumirá /api/mobile/convites do médico autenticado." /><ButtonPrimary title="Ver detalhe demonstrativo" onPress={openDetail} /></ScreenContainer>;
}

function ConviteDetalheScreen() {
  return <PartialScreen title="Detalhe do convite" message="Aceitar/recusar será enviado aos endpoints reais com modal de confirmação, sem diálogo nativo do sistema." />;
}

function renderRoute(route: RouteKey, setRoute: (route: RouteKey) => void) {
  if (route === 'home') return <HomeScreen />;
  if (route === 'plantoes') return <PlantaoListScreen />;
  if (route === 'convites') return <ConvitesScreen openDetail={() => setRoute('conviteDetalhe')} />;
  if (route === 'conviteDetalhe') return <ConviteDetalheScreen />;
  if (route === 'escalas') return <MyPlantoesScreen />;
  if (route === 'pagamentos') return <FinanceiroScreen />;
  if (route === 'notificacoes') return <NotificationsScreen />;
  if (route === 'perfil') return <ProfileScreen />;
  if (route === 'disponibilidade') return <PartialScreen title="Disponibilidade" message="Edição de disponibilidade permanece parcial até homologar endpoints de gravação." />;
  return <PartialScreen title="Preferências" message="Preferências de notificação e agenda usam EXPO_PUBLIC_API_BASE_URL e JWT quando endpoints estiverem ativos." />;
}

export default function AppNavigator() {
  const { signedIn, signOut } = useAuth();
  const [route, setRoute] = useState<RouteKey>('home');
  if (!signedIn) return <LoginScreen />;

  return <View style={styles.shell}>{renderRoute(route, setRoute)}<View style={styles.nav}><ScrollView horizontal showsHorizontalScrollIndicator={false}>{routes.map((item) => <Pressable key={item.key} accessibilityRole="button" onPress={() => setRoute(item.key)} style={[styles.navItem, route === item.key && styles.active]}><Text style={[styles.navText, route === item.key && styles.activeText]}>{item.label}</Text></Pressable>)}<Pressable accessibilityRole="button" onPress={signOut} style={styles.navItem}><Text style={styles.navText}>Sair</Text></Pressable></ScrollView></View></View>;
}

const styles = StyleSheet.create({ shell: { flex: 1, backgroundColor: colors.background }, nav: { borderTopWidth: 1, borderColor: colors.border, backgroundColor: colors.surface, paddingVertical: 8 }, navItem: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: 999, marginHorizontal: 4 }, active: { backgroundColor: colors.primary }, navText: { color: colors.text, fontWeight: '700' }, activeText: { color: '#fff' } });
