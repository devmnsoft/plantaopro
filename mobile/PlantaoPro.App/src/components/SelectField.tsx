import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import colors from '../theme/colors';
export default function SelectField({ label, value }: { label: string; value?: string }) { return <View style={styles.box}><Text style={styles.label}>{label}</Text><Text style={styles.value}>{value || 'Selecione'}</Text></View>; }
const styles = StyleSheet.create({ box: { backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: 12, padding: 12 }, label: { color: colors.muted, fontSize: 12 }, value: { color: colors.text, marginTop: 4 } });
