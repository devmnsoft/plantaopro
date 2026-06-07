import React from 'react';
import { StyleSheet, Text } from 'react-native';
import colors from '../theme/colors';
export default function BadgeStatus({ status = 'PENDENTE' }: { status?: string }) { const s = status.toUpperCase(); const color = s.includes('PAGO') || s.includes('ACEITO') || s.includes('CONFIRM') ? colors.success : s.includes('CANCEL') || s.includes('RECUS') ? colors.danger : colors.warning; return <Text style={[styles.badge, { color, borderColor: color }]}>{s}</Text>; }
const styles = StyleSheet.create({ badge: { alignSelf: 'flex-start', borderWidth: 1, borderRadius: 999, paddingHorizontal: 10, paddingVertical: 4, fontSize: 12, fontWeight: '700' } });
