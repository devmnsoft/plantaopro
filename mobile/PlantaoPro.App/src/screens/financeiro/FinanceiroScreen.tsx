import React, { useEffect, useState } from 'react';
import { Text } from 'react-native';
import ScreenContainer from '../../components/ScreenContainer';
import Header from '../../components/Header';
import EmptyState from '../../components/EmptyState';
import BadgeStatus from '../../components/BadgeStatus';
import { PagamentoMedico } from '../../types/financeiro.types';
import { getMeusPagamentos } from '../../services/financeiroService';
import formatCurrency from '../../utils/formatCurrency';
export default function FinanceiroScreen() { const [items, setItems] = useState<PagamentoMedico[]>([]); useEffect(() => { getMeusPagamentos().then((r) => setItems(r.data?.items ?? [])); }, []); return <ScreenContainer><Header title="Financeiro" />{items.length ? items.map((p) => <Text key={p.id}>{formatCurrency(p.valor)} <BadgeStatus status={p.status} /></Text>) : <EmptyState title="Nenhum pagamento encontrado" />}</ScreenContainer>; }
