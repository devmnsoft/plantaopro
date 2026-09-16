# Correção da rota e evolução da disponibilidade

## Diagnóstico e endpoint canônico

A inicialização falhava porque `MedicoAgendaMeController` e `MedicosMeDisponibilidadeController` publicavam o mesmo par método/caminho, `GET /api/medicos/me/disponibilidade`. As duas actions chamavam o mesmo `OperationalAutomationService`, sem diferenças de parâmetros, resposta ou regra de consulta. A primeira usava somente a claim `uid` e restringia papéis; a segunda já concentrava no mesmo controller as escritas, aceitava o identificador e o fallback `NameIdentifier` da identidade autenticada e mantinha a resolução do médico no servidor.

O endpoint do `MedicosMeDisponibilidadeController` foi mantido como canônico, agora com filtros opcionais `inicio` e `fim`. O GET redundante foi removido sem remover `MedicoAgendaMeController`, cuja agenda e substituições continuam válidas. O contrato `ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>`, os status e a URL pública foram preservados. Não foi encontrado outro conflito entre POST, PUT ou DELETE. O `ApiRouteStartupValidator` permanece ativo após `MapControllers`.

## Regras funcionais e segurança

* A consulta aceita o período completo, limitado a 366 dias, e usa interseção de intervalos `[início, fim)`; intervalos adjacentes não conflitam.
* O titular é sempre obtido da identidade autenticada. O payload não possui `medicoId`, e hospital/unidade é validado no cliente do vínculo.
* Criação repetida é idempotente para o mesmo intervalo e contexto. Criação e edição são serializadas por médico com transação serializável e advisory lock, impedindo corridas na verificação de sobreposição.
* A edição pode enviar `versaoEsperada`; uma versão desatualizada retorna 409 em vez de sobrescrever. Exclusão repetida e acesso a registro alheio retornam 404, sem revelar seu proprietário.
* Disponibilidades e indisponibilidades são planejamento. Nenhuma dessas operações cria escala, confirma convite, gera pagamento ou apaga compromisso. A seleção de cobertura continua revalidando disponibilidade no momento da decisão.
* O histórico existente registra criação, edição e remoção. Cancelamento é lógico (`reg_status='I'`).

## Experiência Web

“Minha disponibilidade” passou a ter página própria com contexto autenticado, fuso detectado, filtro de período, lista cronológica, formulário de inclusão/edição e diálogo descritivo de cancelamento. Há estados de carregamento, vazio, erro, acesso negado, gravação, sucesso e conflito. Requisições antigas são descartadas por sequência; botões voltam ao estado disponível em falhas. O layout prioriza a lista e formulários de uma coluna no mobile, tem foco visível e usa texto junto às cores semânticas.

A ligação com “Minha agenda” é explícita, e a ajuda informa que disponibilidade não confirma e indisponibilidade não cancela plantões.

## Testes e resultados

O teste de integração dos descritores reais da aplicação agora exige exatamente um GET canônico e verifica o metadata de autorização, além do gate global de unicidade. Os testes contratuais existentes cobrem transação serializável, idempotência, preservação de atribuições e revalidação da cobertura.

Os comandos e respectivos resultados executados nesta entrega constam na descrição do pull request.

## Limitações restantes

O modelo atual separa disponibilidade e indisponibilidade em tabelas/endpoints, portanto o novo formulário edita disponibilidade; indisponibilidades continuam no fluxo existente. O banco não possui coluna de fuso nem chave de idempotência fornecida pelo cliente: instantes seguem o padrão `DateTime` vigente e duplicidade exata é idempotente. A seleção de unidade permanece opcional até que o catálogo autorizado seja exposto ao portal sem GUID manual. Testes E2E com persistência dependem de PostgreSQL isolado e usuário semeado.
