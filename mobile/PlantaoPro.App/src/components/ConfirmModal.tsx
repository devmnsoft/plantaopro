import React from 'react';
import { Modal, StyleSheet, Text, View } from 'react-native';
import ButtonOutline from './ButtonOutline';
import ButtonPrimary from './ButtonPrimary';
import colors from '../theme/colors';
export default function ConfirmModal({ visible, title, message, onCancel, onConfirm }: { visible: boolean; title: string; message: string; onCancel: () => void; onConfirm: () => void }) { return <Modal transparent visible={visible} animationType="fade"><View style={styles.overlay}><View style={styles.box}><Text style={styles.title}>{title}</Text><Text style={styles.message}>{message}</Text><ButtonPrimary title="Confirmar" onPress={onConfirm} /><ButtonOutline title="Cancelar" onPress={onCancel} /></View></View></Modal>; }
const styles = StyleSheet.create({ overlay: { flex: 1, backgroundColor: 'rgba(15,23,42,.45)', justifyContent: 'center', padding: 24 }, box: { backgroundColor: colors.surface, borderRadius: 18, padding: 20, gap: 12 }, title: { fontSize: 18, fontWeight: '800', color: colors.text }, message: { color: colors.muted } });
