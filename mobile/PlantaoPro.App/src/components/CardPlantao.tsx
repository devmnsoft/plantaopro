import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import BadgeStatus from './BadgeStatus';
import colors from '../theme/colors';
import { Plantao } from '../types/plantao.types';
import { formatDateTime } from '../utils/formatDate';
import formatCurrency from '../utils/formatCurrency';

export default function CardPlantao({ plantao }: { plantao: Plantao }) {
  return <View style={styles.card}><View style={styles.row}><Text style={styles.title}>{plantao.hospitalNome ?? 'Plantão'}</Text><BadgeStatus status={plantao.status} /></View><Text style={styles.meta}>{plantao.especialidadeNome ?? 'Especialidade'} • {plantao.hospitalCidade ?? '-'} / {plantao.hospitalEstado ?? '-'}</Text><Text style={styles.meta}>{formatDateTime(plantao.dataInicio)} até {formatDateTime(plantao.dataFim)}</Text><Text style={styles.value}>{formatCurrency(plantao.valor)}</Text></View>;
}
const styles = StyleSheet.create({ card: { backgroundColor: colors.surface, borderRadius: 16, borderColor: colors.border, borderWidth: 1, padding: 16, gap: 8 }, row: { flexDirection: 'row', justifyContent: 'space-between', gap: 8 }, title: { color: colors.text, fontWeight: '800', flex: 1 }, meta: { color: colors.muted }, value: { color: colors.primary, fontWeight: '800' } });
