import React from 'react';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import InputField from '../../components/InputField';
import ButtonPrimary from '../../components/ButtonPrimary';
export default function RegisterDoctorScreen() { return <ScreenContainer><Header title="Cadastro médico" /><InputField label="Nome" /><InputField label="CRM" /><ButtonPrimary title="Solicitar cadastro" /></ScreenContainer>; }
