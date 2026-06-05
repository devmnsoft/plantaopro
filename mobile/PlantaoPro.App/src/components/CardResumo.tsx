import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import colors from '../theme/colors';
export default function CardResumo({ label, value }: { label: string; value: string | number }) { return <View style={styles.card}><Text style={styles.label}>{label}</Text><Text style={styles.value}>{value}</Text></View>; }
const styles = StyleSheet.create({ card: { flex: 1, backgroundColor: colors.surface, borderRadius: 16, padding: 16, borderWidth: 1, borderColor: colors.border }, label: { color: colors.muted }, value: { color: colors.text, fontSize: 22, fontWeight: '800', marginTop: 4 } });
