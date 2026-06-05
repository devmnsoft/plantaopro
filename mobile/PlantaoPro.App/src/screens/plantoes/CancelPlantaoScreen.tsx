import React from 'react';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import InputField from '../../components/InputField';
import ButtonPrimary from '../../components/ButtonPrimary';
export default function CancelPlantaoScreen() { return <ScreenContainer><Header title="Recusar convite" /><InputField label="Motivo" multiline /><ButtonPrimary title="Enviar recusa" /></ScreenContainer>; }
