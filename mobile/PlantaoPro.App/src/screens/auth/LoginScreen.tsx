import React, { useState } from 'react';
import { Alert } from 'react-native';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import InputField from '../../components/InputField';
import ButtonPrimary from '../../components/ButtonPrimary';
import { useAuth } from '../../context/AuthContext';
import { isEmail, isRequired } from '../../utils/validators';

export default function LoginScreen() {
  const { signIn } = useAuth();
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const submit = async () => {
    if (!isEmail(email) || !isRequired(senha)) { Alert.alert('Validação', 'Informe e-mail válido e senha.'); return; }
    const error = await signIn({ email, senha });
    if (error) Alert.alert('Login', error);
  };
  return <ScreenContainer><Header title="Entrar" subtitle="Acesso seguro via JWT" /><InputField label="E-mail" autoCapitalize="none" keyboardType="email-address" value={email} onChangeText={setEmail} /><InputField label="Senha" secureTextEntry value={senha} onChangeText={setSenha} /><ButtonPrimary title="Entrar" onPress={submit} /></ScreenContainer>;
}
