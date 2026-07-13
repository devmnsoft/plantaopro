import React, { useState } from 'react';
import { Text } from 'react-native';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import InputField from '../../components/InputField';
import ButtonPrimary from '../../components/ButtonPrimary';
import { useAuth } from '../../context/AuthContext';
import { isEmail, isRequired } from '../../utils/validators';
import colors from '../../theme/colors';

export default function LoginScreen() {
  const { signIn } = useAuth();
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const submit = async () => {
    if (!isEmail(email) || !isRequired(senha)) { setMessage('Informe e-mail válido e senha.'); return; }
    setLoading(true);
    const error = await signIn({ email, senha });
    setLoading(false);
    setMessage(error ?? null);
  };
  return <ScreenContainer><Header title="Entrar" subtitle="Acesso seguro via JWT" />{message ? <Text style={{ color: colors.danger }}>{message}</Text> : null}<InputField label="E-mail" autoCapitalize="none" keyboardType="email-address" value={email} onChangeText={setEmail} /><InputField label="Senha" secureTextEntry value={senha} onChangeText={setSenha} /><ButtonPrimary title={loading ? 'Entrando...' : 'Entrar'} onPress={submit} disabled={loading} /></ScreenContainer>;
}
