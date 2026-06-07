import React, { useEffect, useState } from 'react';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import EmptyState from '../../components/EmptyState';
import CardPlantao from '../../components/CardPlantao';
import { Plantao } from '../../types/plantao.types';
import { getMinhasEscalas } from '../../services/plantaoService';
export default function MyPlantoesScreen() { const [items, setItems] = useState<Plantao[]>([]); useEffect(() => { getMinhasEscalas().then((r) => setItems(r.data?.items ?? [])); }, []); return <ScreenContainer><Header title="Minha agenda" />{items.length ? items.map((p) => <CardPlantao key={p.id} plantao={p} />) : <EmptyState title="Nenhuma escala confirmada" />}</ScreenContainer>; }
