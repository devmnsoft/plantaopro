import React from 'react';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import InputField from '../../components/InputField';
import ButtonPrimary from '../../components/ButtonPrimary';
export default function EditProfileScreen() { return <ScreenContainer><Header title="Editar perfil" /><InputField label="Nome" /><InputField label="Telefone" /><ButtonPrimary title="Salvar alterações" /></ScreenContainer>; }
