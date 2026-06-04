import React, { useState } from 'react';
import { SafeAreaView, ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { theme } from './src/theme/theme';

const services = ['Corte Masculino', 'Barba Tradicional', 'Corte + Barba', 'Sobrancelha', 'Hidratação Capilar', 'Manicure'];
const professionals = ['Rafael Barber', 'Lucas Navalha', 'Bruno Estilo', 'Camila Beauty', 'Amanda Nails'];

export default function App() {
  const [service, setService] = useState(services[0]);
  const [professional, setProfessional] = useState(professionals[0]);
  return (
    <SafeAreaView style={theme.screen}>
      <ScrollView contentContainerStyle={theme.content}>
        <Text style={theme.eyebrow}>BarberSync Mobile</Text>
        <Text style={theme.title}>Agenda, fidelidade e comanda na palma da mão.</Text>
        <View style={theme.card}><Text style={theme.cardTitle}>Próximo horário demo</Text><Text style={theme.text}>Hoje às 16:30 · {service} com {professional}</Text></View>
        <Text style={theme.section}>Serviços</Text>{services.map(item => <TouchableOpacity key={item} style={[theme.pill, item === service && theme.pillActive]} onPress={() => setService(item)}><Text style={theme.pillText}>{item}</Text></TouchableOpacity>)}
        <Text style={theme.section}>Profissionais</Text>{professionals.map(item => <TouchableOpacity key={item} style={[theme.pill, item === professional && theme.pillActive]} onPress={() => setProfessional(item)}><Text style={theme.pillText}>{item}</Text></TouchableOpacity>)}
      </ScrollView>
    </SafeAreaView>
  );
}
