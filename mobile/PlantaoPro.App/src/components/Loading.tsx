import React from 'react';
import { ActivityIndicator, View } from 'react-native';
import colors from '../theme/colors';
export default function Loading() { return <View style={{ padding: 24 }}><ActivityIndicator color={colors.primary} /></View>; }
