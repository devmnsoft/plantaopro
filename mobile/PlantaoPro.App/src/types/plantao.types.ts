export interface Plantao {
  id: string;
  hospitalNome?: string;
  hospitalCidade?: string;
  hospitalEstado?: string;
  especialidadeNome?: string;
  dataInicio: string;
  dataFim: string;
  valor?: number;
  status?: string;
  vagas?: number;
}

export interface ConvitePlantao extends Plantao {
  plantaoId: string;
  mensagem?: string;
  dataEnvio?: string;
  dataResposta?: string | null;
  motivoRecusa?: string;
}
