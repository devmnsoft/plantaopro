import React from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';
import colors from '../theme/colors';

export default function ButtonOutline({ title, onPress }: { title: string; onPress?: () => void }) {
  return <Pressable accessibilityRole="button" onPress={onPress} style={styles.button}><Text style={styles.text}>{title}</Text></Pressable>;
}
const styles = StyleSheet.create({ button: { borderColor: colors.primary, borderWidth: 1, borderRadius: 14, padding: 14, alignItems: 'center' }, text: { color: colors.primary, fontWeight: '700' } });
