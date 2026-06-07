import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import colors from '../theme/colors';

export default function EmptyState({ title = 'Nenhum registro encontrado', message = 'Tente ajustar os filtros ou atualizar a lista.' }: { title?: string; message?: string }) {
  return <View style={styles.box}><Text style={styles.icon}>🩺</Text><Text style={styles.title}>{title}</Text><Text style={styles.message}>{message}</Text></View>;
}
const styles = StyleSheet.create({ box: { backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: 16, padding: 24, alignItems: 'center', gap: 8 }, icon: { fontSize: 30 }, title: { color: colors.text, fontWeight: '700', fontSize: 16 }, message: { color: colors.muted, textAlign: 'center' } });
