import React from 'react';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import InputField from '../../components/InputField';
import ButtonPrimary from '../../components/ButtonPrimary';
export default function ForgotPasswordScreen() { return <ScreenContainer><Header title="Recuperar senha" /><InputField label="E-mail" /><ButtonPrimary title="Enviar instruções" /></ScreenContainer>; }
