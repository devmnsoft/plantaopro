import React, { useEffect, useState } from 'react';
import { Text } from 'react-native';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import { UserProfile } from '../../types/auth.types';
import { getProfile } from '../../services/authService';
export default function ProfileScreen() { const [profile, setProfile] = useState<UserProfile | null>(null); useEffect(() => { getProfile().then((r) => setProfile(r.data)); }, []); return <ScreenContainer><Header title="Perfil" /><Text>{profile?.nome ?? 'Médico'}</Text><Text>{profile?.email ?? 'perfil@plantaopro.local'}</Text></ScreenContainer>; }
