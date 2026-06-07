import React from 'react';
import { StyleSheet, Text, TextInput, TextInputProps, View } from 'react-native';
import colors from '../theme/colors';

export default function InputField({ label, error, ...props }: TextInputProps & { label: string; error?: string }) {
  return <View style={styles.wrapper}><Text style={styles.label}>{label}</Text><TextInput placeholderTextColor={colors.muted} style={[styles.input, error && styles.inputError]} {...props} />{error ? <Text style={styles.error}>{error}</Text> : null}</View>;
}
const styles = StyleSheet.create({ wrapper: { gap: 6 }, label: { color: colors.text, fontWeight: '600' }, input: { backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: 12, padding: 12 }, inputError: { borderColor: colors.danger }, error: { color: colors.danger, fontSize: 12 } });
