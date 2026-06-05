import React, { PropsWithChildren } from 'react';
import { SafeAreaView, ScrollView, StyleSheet } from 'react-native';
import colors from '../theme/colors';
import spacing from '../theme/spacing';

export default function ScreenContainer({ children }: PropsWithChildren) {
  return <SafeAreaView style={styles.safe}><ScrollView contentContainerStyle={styles.content}>{children}</ScrollView></SafeAreaView>;
}
const styles = StyleSheet.create({ safe: { flex: 1, backgroundColor: colors.background }, content: { padding: spacing.lg, gap: spacing.lg } });
