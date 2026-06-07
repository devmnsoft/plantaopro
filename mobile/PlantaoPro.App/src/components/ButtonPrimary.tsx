import React from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';
import colors from '../theme/colors';

export default function ButtonPrimary({ title, onPress, disabled }: { title: string; onPress?: () => void; disabled?: boolean }) {
  return <Pressable accessibilityRole="button" disabled={disabled} onPress={onPress} style={[styles.button, disabled && styles.disabled]}><Text style={styles.text}>{title}</Text></Pressable>;
}
const styles = StyleSheet.create({ button: { backgroundColor: colors.primary, borderRadius: 14, padding: 14, alignItems: 'center' }, disabled: { opacity: 0.5 }, text: { color: '#fff', fontWeight: '700' } });
