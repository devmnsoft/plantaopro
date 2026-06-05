import React, { useEffect, useState } from 'react';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import EmptyState from '../../components/EmptyState';
import CardPlantao from '../../components/CardPlantao';
import { Plantao } from '../../types/plantao.types';
import { getPlantoesDisponiveis } from '../../services/plantaoService';

export default function PlantaoListScreen() { const [items, setItems] = useState<Plantao[]>([]); useEffect(() => { getPlantoesDisponiveis().then((r) => setItems(r.data?.items ?? [])); }, []); return <ScreenContainer><Header title="Plantões disponíveis" subtitle="Lista paginada e filtrável" />{items.length ? items.map((p) => <CardPlantao key={p.id} plantao={p} />) : <EmptyState />}</ScreenContainer>; }
