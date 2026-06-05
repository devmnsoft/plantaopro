export interface PagamentoMedico {
  id: string;
  descricao?: string;
  competencia?: string;
  valor: number;
  status: string;
  vencimento?: string;
  pagoEm?: string | null;
}
