import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import EmptyState from '../../components/EmptyState';
import BadgeStatus from '../../components/BadgeStatus';
import ButtonOutline from '../../components/ButtonOutline';
import ButtonPrimary from '../../components/ButtonPrimary';
import ConfirmModal from '../../components/ConfirmModal';
import Loading from '../../components/Loading';
import { PagamentoMedico } from '../../types/financeiro.types';
import { contestarPagamentoV115, getMeusPagamentos, getRepassesV115, responderContestacaoV115 } from '../../services/financeiroService';
import formatCurrency from '../../utils/formatCurrency';
import colors from '../../theme/colors';

export default function FinanceiroScreen() {
  const [items, setItems] = useState<PagamentoMedico[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selected, setSelected] = useState<PagamentoMedico | null>(null);

  useEffect(() => {
    Promise.all([getRepassesV115(), getMeusPagamentos()]).then(([repasses, pagamentos]) => {
      const merged = [...(repasses.data?.items ?? []), ...(pagamentos.data?.items ?? [])];
      setItems(merged);
      setError(repasses.success || pagamentos.success ? '' : repasses.message);
    }).catch((err) => setError(err instanceof Error ? err.message : 'Falha ao carregar financeiro médico.')).finally(() => setLoading(false));
  }, []);

  const contestar = () => {
    if (!selected) return;
    contestarPagamentoV115(selected.id, 'Contestação aberta pelo médico no mobile v1.15').then(() => setSelected(null));
  };

  const responder = (item: PagamentoMedico) => {
    responderContestacaoV115(item.id, 'Resposta enviada pelo mobile médico v1.15').then(() => undefined);
  };

  return <ScreenContainer><Header title="Financeiro médico" subtitle="Repasses previstos, pagamentos pendentes, confirmados e contestações v1.15." />{loading ? <Loading /> : null}{error ? <EmptyState title="Não foi possível carregar" message={error} /> : null}{!loading && !error && !items.length ? <EmptyState title="Nenhum repasse encontrado" message="Quando houver plantão/consulta faturada, os repasses configuráveis aparecerão aqui." /> : null}<View style={styles.list}>{items.map((p) => <View key={p.id} style={styles.card}><View style={styles.row}><Text style={styles.title}>{p.descricao ?? 'Repasse médico'}</Text><BadgeStatus status={p.status} /></View><Text style={styles.value}>{formatCurrency(p.valor)}</Text><Text style={styles.meta}>Competência: {p.competencia ?? 'A definir'} • Vencimento: {p.vencimento ?? 'A definir'}</Text><View style={styles.actions}><ButtonOutline title="Contestar" onPress={() => setSelected(p)} /><ButtonPrimary title="Responder" onPress={() => responder(p)} /></View></View>)}</View><ConfirmModal visible={Boolean(selected)} title="Contestar pagamento" message="A contestação será registrada sem diálogo nativo e sem expor dados clínicos sensíveis." onCancel={() => setSelected(null)} onConfirm={contestar} /></ScreenContainer>;
}
const styles = StyleSheet.create({ list: { gap: 12 }, card: { backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: 16, padding: 16, gap: 8 }, row: { flexDirection: 'row', justifyContent: 'space-between', gap: 8 }, title: { color: colors.text, fontWeight: '800', flex: 1 }, value: { color: colors.primary, fontWeight: '800', fontSize: 20 }, meta: { color: colors.muted }, actions: { gap: 8 } });
