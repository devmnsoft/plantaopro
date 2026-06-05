import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import colors from '../theme/colors';
export default function Header({ title, subtitle }: { title: string; subtitle?: string }) { return <View><Text style={styles.title}>{title}</Text>{subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}</View>; }
const styles = StyleSheet.create({ title: { color: colors.text, fontWeight: '800', fontSize: 26 }, subtitle: { color: colors.muted, marginTop: 4 } });
