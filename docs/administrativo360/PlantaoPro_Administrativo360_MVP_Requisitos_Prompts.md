# Administrativo 360 — requisitos, prioridade e plano finito de MVP

Data: 24/09/2026. Meta de apresentação: sábado, 26/09/2026.
Projeto: https://github.com/devmnsoft/plantaopro
Baseline inspecionada: `23b01afecb2b9815b0f1290df211f1ab5cdb2709`.

**Estado deste documento:** especificação e plano de implementação. Não comprova software implementado, credenciais provisionadas nem homologação. A prioridade proposta substitui novos avanços de escopo geral; correções críticas de base continuam obrigatórias. As regras comerciais abaixo são decisões propostas para o MVP, com valores configuráveis quando indicado.

## 1. Resultado a entregar e limite da promessa

Um único módulo contratável, código proposto `ADMINISTRATIVO_360`, com páginas próprias organizadas em Cadastros, Comercial, Suprimentos, Distribuição Cirúrgica, Financeiro, Fiscal/Contábil e Relatórios. Não criar uma página universal nem um segundo login.

O usuário entra pelo login existente, no tenant autorizado, e visualiza o módulo se tiver contratação ativa e permissão. A operação cirúrgica integra vendas, compras, lotes e financeiro dentro deste módulo. Não depende de contratação de Saúde 360 ou Plantões. A consulta opcional de clientes existentes respeita o contexto e as permissões atuais.

**Jornada obrigatória de demonstração:** cadastrar fornecedor e produto → comprar → receber em quarentena → inspecionar e liberar lote → orçar cirurgia → aprovar e reservar → separar e consignar ao hospital → registrar consumo e devolução → valorizar vale → confirmar venda interna → gerar contas a receber → registrar recebimento manual → apurar comissão → consultar caixa, margem e rastreabilidade.

Compra também deve gerar obrigação em contas a pagar e permitir pagamento manual. Venda direta sem cirurgia usa o mesmo estoque e financeiro. Inventário e conferência pelo celular completam a demonstração.

Não é realista tratar um ERP fiscal, bancário e contábil completo como pronto em dois dias. O marco é um MVP demonstrável em homologação, com dados reais no PostgreSQL e operações internas consistentes. Não é liberação irrestrita para produção sanitária/fiscal.

### Prioridade e cortes explícitos

| Prioridade | Entrega | Critério de corte |
|---|---|---|
| P0 — bloqueador | Build, instalação/upgrade, login, isolamento, contratação e permissões | Nenhuma jornada declarada pronta enquanto falhar |
| P0 — núcleo | Cadastros, compras, recebimento, qualidade, estoque/lotes, orçamento, cirurgia, consignação, vendas | Fluxo completo persistido; sem telas apenas descritivas |
| P0 — fechamento | AP/AR, baixas manuais, caixa, fluxo de caixa, custos diretos e comissão simples | Conciliação dos valores e estornos |
| P0 — apresentação | Relatórios CSV/impressão, mobile web, seed SQL, ajuda, feedback e auditoria | Roteiro reproduzível por usuário de teste |
| P1 — após o MVP | Emissão NF-e integrada, cobrança e pagamento bancário eletrônico | Credenciais, provedor/layout, ambiente e aceite externo |
| P1 — após o MVP | Motor tributário, escrituração fiscal e contábil completa | Requisitos do regime/UF e validação pelo responsável |
| P2 | Offline/sincronização, OCR, previsão, automações complexas, portal externo, comissões multinível | Não entram no marco de sábado |

As páginas de controle fiscal, contábil e bancário do MVP terão cadastros/consultas internas úteis. Não exibir “NF-e autorizada”, “boleto registrado” ou “transferência realizada” sem resposta válida do sistema externo. Não criar botões que sempre retornam sucesso.

## 2. Evidência atual e reaproveitamento

Inspeção direcionada ao novo módulo, SaaS, arquitetura e interface; não representa auditoria integral de todas as jornadas.

| Evidência | Implicação |
|---|---|
| Busca por Administrativo 360, consignação, instrumental e quarentena em backend/database sem resultados nesta baseline | O domínio solicitado precisa ser construído; não considerar pronto por existir Saúde 360 |
| `backend/PlantaoPro.Api/ModuleContractingService.cs` | Reutilizar catálogo, solicitação, aprovação e contratação; não duplicar billing |
| `backend/PlantaoPro.Web/Services/FeatureCatalogService.cs` | Registrar a entrada e os recursos do módulo no catálogo canônico |
| `backend/PlantaoPro.Web/Services/Security/MenuBuilderService.cs` | Hoje limita 12 entradas antes de filtrar acesso; verificar visibilidade do novo módulo e corrigir a ordem sem quebrar menus existentes |
| `backend/PlantaoPro.Web/Views/Shared/_ScreenGuide.cshtml` e `_ConfirmModal.cshtml` | Reutilizar apresentação/feedback; conteúdo de ajuda específico de cada nova página |
| Application e Infrastructure atualmente muito pequenas; serviço de contratação contém SQL na API | Implementar o novo domínio nas camadas adequadas; não repetir concentração de regras na API ou Razor |
| `database/seeds/development/122_usuarios_homologacao_plantaopro.sql` | Padrão de seed e BCrypt existente; criar seed separado e opt-in para o novo cliente |
| `docs/m5-auditoria-rc1-2026-09-24.md` | O próprio relatório informa M5 não concluído e RC1 não liberada |
| CI `36047353701`: Build e Run real database upgrade test falharam; runtime e swagger pulados | Diagnosticar logs atuais antes de avançar; não atribuir a causa a falhas antigas |
| CI `36047353803`: database-one-click-v1951 concluída com sucesso | Evidência parcial; não substitui build e teste de upgrade |

CI: https://github.com/devmnsoft/plantaopro/actions/runs/36047353701

O ambiente desta análise não possui `dotnet` nem `psql` no PATH. Não foram executados build, migrations, seed ou testes funcionais do novo módulo. O próximo executor precisa de ambiente .NET/PostgreSQL operacional; não ocultar esse gate.

## 3. Contratação, contexto e perfis

### Acesso

O catálogo pode apresentar o módulo ao administrador habilitado a contratar. Solicitar não ativa. A aprovação usa o fluxo SaaS existente. Preço não definido deve ficar sob proposta; não inventar preço nem marcar gratuito em produção.

Entrada operacional exige simultaneamente: sessão válida, usuário e tenant ativos, módulo contratado/habilitado/vigente e permissão para o recurso/ação. Aplicar no servidor a listagens, detalhes, comandos, pesquisa, anexos e exportações. Esconder menu não é autorização.

Permissões propostas: `ADM360.<RECURSO>.<ACAO>`, adaptadas ao modelo canônico sem criar autorização paralela. Ações: VER, CRIAR, EDITAR, INATIVAR, APROVAR, EXPEDIR, LIBERAR_QUALIDADE, BAIXAR, ESTORNAR, EXPORTAR, ADMINISTRAR. Permissão de leitura não concede exportação nem escrita.

| Perfil | Escopo permitido | Restrições importantes |
|---|---|---|
| Administrador do módulo | Configuração e todas as operações do próprio tenant | Não administra tenants ou planos globais |
| Comercial | Parceiros, orçamentos e vendas | Não libera quarentena nem baixa pagamentos |
| Vendedor | Carteira atribuída, vendas próprias e comissão própria | Sem custo de compra/margem global ou comissão alheia |
| Compras | Fornecedores, pedidos e acompanhamento de recebimento | Não aprova qualidade ou executa pagamento |
| Estoquista | Receber, separar, transferir, inventariar e conferir | Sem liberação sanitária e sem alterar saldos diretamente |
| Qualidade | Inspeção, bloqueio/liberação, não conformidade e rastreabilidade | Sem baixas financeiras; decisão sempre justificada |
| Coordenador cirúrgico | Agenda, orçamento, reserva, vale, retorno e instrumental | Sem acesso a prontuário; sem execução bancária |
| Financeiro | AP/AR, baixas, caixa, custos e comissões | Não altera lotes ou registros de qualidade |
| Fiscal | Documentos fiscais, parâmetros e conferência | Não configura credenciais globais; emissão externa apenas quando habilitada |
| Contábil | Plano de contas, lançamentos, fechamento/exportação | Não altera operações de origem já confirmadas |
| Auditor | Consulta e exportação explicitamente concedidas | Nenhuma mutação; leitura de logs sem segredos |

Permitir acumular papéis dentro do tenant, sem atalhos baseados no nome do usuário. Administrador demonstrativo pode acumular as permissões para apresentar o fluxo; perfis restritos precisam ser testados. Administrador global gerencia contratação, mas acesso operacional de suporte exige contexto explícito e auditado conforme política existente.

### Cliente não é tenant

Tenant é a empresa assinante. Parceiro comercial é comprador, fornecedor, hospital ou prestador dentro dessa empresa. Criar cadastros próprios do domínio quando os existentes não representam isso. Consulta opcional ao cadastro canônico de clientes usa somente registros acessíveis no tenant, sem pesquisa global nem aceitação de tenant enviado pelo navegador. Importar uma referência e snapshot mínimo; não mover o cadastro original nem compartilhar saldo. Médicos e hospitais são referências operacionais, não usuários obrigatórios.

Na suspensão do contrato, bloquear novas operações pelas regras SaaS. Política de leitura/exportação após suspensão precisa ser explícita e coberta por teste, nunca concedida por acidente. Preservar registros e auditoria.

## 4. Páginas e requisitos verificáveis

Prefixo Web proposto `/Administrativo360`; prefixo API `/api/administrativo360`. Cada recurso tem rota, controller/viewmodel, comandos e consultas próprios. “CRUD” abaixo permite exclusão apenas de rascunho sem vínculos; registros históricos são inativados ou estornados.

### A. Cadastros e visão geral

| ID / página | Contexto e campos essenciais | Regras, ações e aceite |
|---|---|---|
| A01 Painel | Período, empresa, contas vencidas, cirurgias, lotes a vencer, quarentena e vales pendentes | Consultas reais filtradas por perfil. Cada indicador abre a lista correspondente. Sem acesso financeiro, não revelar valores. Valores vazios são zero real, com período informado |
| A02 Parceiros | Nome, tipo(s), documento quando aplicável, contato, endereço, situação, referência externa opcional | CRUD, busca e inativação; mesmo parceiro pode comprar e fornecer. Documento normalizado único no tenant quando preenchido. Hospital não é automaticamente devedor; operação escolhe responsável financeiro |
| A03 Produtos | SKU, descrição, unidade, categoria, código de barras, fabricante, controle de lote/série, comercialização e regularização | SKU único no tenant. Não desligar controle de lote em produto movimentado. Preço/custo históricos ficam nos documentos. Cadastro comercial não libera lote |
| A04 Regularização Anvisa | Produto, tipo de regularização, número quando aplicável, fabricante/detentor, situação, fonte/data da conferência e evidência | Registro, notificação ou condição aplicável: não exigir ficticiamente o mesmo regime para todo material. Situação desconhecida/suspensa bloqueia expedição dos itens que exigem verificação até decisão autorizada. Não afirmar validação automática da Anvisa |
| A05 Locais de estoque | Código, nome, interno/externo, hospital/custodiante, endereço, ativo | CRUD; local com saldo/reserva não pode ser excluído. Titularidade e localização separadas. Local externo não implica venda |
| A06 Médicos e hospitais por cirurgia | Procedimento, médico/hospital, produtos/quantidades sugeridas, kits e instruções logísticas | Modelos versionados. Aplicar copia itens para orçamento; não altera orçamento já aprovado nem substitui julgamento clínico. Sem prontuário nesta tela |
| A07 Instrumentais/patrimônios | Identificação única, composição da caixa, itens seriados, localização, disponibilidade, inspeção/manutenção e condição documental de uso | CRUD de composição versionada e checklist. Uma caixa não pode estar reservada em horários sobrepostos. Retorno incompleto gera ocorrência e indisponibilidade. Reutilizável não é consumido como implante |
| A08 Configurações e acessos | Perfis, permissões, formas de pagamento, contas, centros de custo, regra de comissão e prazos de alerta | Somente administrador autorizado. Versionar alterações financeiras. Preço de assinatura não é parâmetro editável pelo tenant |

### B. Suprimentos, qualidade e coleta

| ID / página | Contexto e campos essenciais | Regras, ações e aceite |
|---|---|---|
| A09 Compras | Fornecedor, itens, quantidades, unidade, valor, desconto, frete, previsão, condição de pagamento | Rascunho → aprovado → parcial → recebido → encerrado. Aprovação congela valores. Recebimento parcial não encerra pedido. Impedir receber mais que pedido sem aditivo autorizado. Cancelamento só da parte não recebida |
| A10 Recebimentos | Pedido, documento, local, quantidades recebidas, lote, fabricação/validade quando aplicáveis, divergência e evidência | Conferência real por item; item sujeito a controle entra em quarentena. Não se torna vendável ao salvar. Confirmação gera movimento e obrigação financeira conforme regra A22; repetir requisição não duplica |
| A11 Inspeções e quarentena | Recebimento/lote, integridade, identificação, documentação, quantidade aprovada/reprovada, responsável e evidência | Pendente → em inspeção → aprovado/parcial/reprovado. Estoquista não pode liberar. Quantidade inspecionada não excede recebida; decisão por quantidade/local com rastreio. Separar condição do lote e condição física da porção |
| A12 Não conformidades/ocorrências | Origem, lote/kit, local, tipo, quantidade, gravidade, evidência, ação, responsável e prazo | Aberta → em tratamento → resolvida → encerrada. Bloqueio imediato autorizado pode abranger saldo interno e externo. Encerrar exige destino dos itens e justificativa. Registro de ocorrência não apaga estoque |
| A13 Estoque e lotes | Produto, lote, validade, local, condição, saldo físico, reservado e disponível | Somente movimentos geram saldo. Entrada, saída, transferência, bloqueio e estorno auditados. Sem ajuste direto no formulário de produto. Filtros por lote/local/hospital, alertas e exportação |
| A14 Inventários | Local/escopo, data, versão, contagens, divergências, motivo e aprovador | Aberto → contagem → revisão → aprovado. Congelar movimentação no escopo enquanto conta no MVP, com mensagem clara; cancelar libera sem ajustar. Aprovação cria ajuste único, nunca sobrescreve saldo. Permissão de contar não concede aprovação |
| A15 Coleta móvel | Selecionar tarefa de inventário, separação ou recebimento; leitor/câmera e entrada manual | Browser responsivo com autorização da tarefa. Código identifica produto; pedir lote/série se código não contém. Ambiguidade exige escolha. Confirmar linha/quantidade; nova leitura intencional pode somar, reenvio da mesma leitura não. Sem offline no MVP |

### C. Comercial e distribuição cirúrgica

| ID / página | Contexto e campos essenciais | Regras, ações e aceite |
|---|---|---|
| A16 Orçamentos de cirurgia | Hospital, médico, procedimento, pagador, validade, materiais, kits, quantidades, valores/descontos e vendedor | Rascunho → enviado → aprovado/rejeitado/expirado. Aprovado não muda silenciosamente: nova revisão. Aprovar preço não movimenta estoque. Impresso identificado como orçamento, sem valor fiscal |
| A17 Agenda cirúrgica | Referência, hospital, médico, procedimento, início/fim, situação, orçamento, reserva e identificação mínima necessária | Prevista → confirmada → em atendimento → realizada/cancelada. Alerta/bloqueio de conflito de kit e indisponibilidade antes da confirmação. Cancelar libera reservas não expedidas; vale expedido exige retorno. Não armazenar diagnóstico desnecessário |
| A18 Consignações / vales | Cirurgia/hospital, destinatário, itens/lotes/quantidades, kit, saída prevista/real, custódia, devolução prevista e aceite | Rascunho → reservado → separado → expedido → retorno parcial → reconciliado → valorizado → encerrado. Expedir transfere interno para externo sob mesma propriedade. Só pode consumir/devolver o expedido ainda pendente. Mostrar remanescente no hospital |
| A19 Retorno e valorização | Vale, consumido, devolvido, perdido/avariado, em custódia, preço acordado e evidências | Fechar exige reconciliação quantitativa. Retorno passa por avaliação/quarentena conforme produto, sem liberar material automaticamente. Valorização usa consumo efetivo e snapshot comercial. Perda exige decisão específica, não vira consumo/venda escondida. Confirmação gera uma venda vinculada, sem duplicar saída já registrada |
| A20 Vendas | Venda direta ou originada de vale; comprador, pagador, vendedor, itens, preços, descontos e parcelas | Rascunho → confirmado → atendimento parcial/concluído → encerrado; cancelamento por comandos. Venda direta reserva/expede pelas mesmas regras do estoque. Venda de vale não baixa o mesmo material duas vezes. AR apenas da parcela comercial confirmada; nota fiscal é processo distinto |

### D. Financeiro, fiscal e contábil

| ID / página | Contexto e campos essenciais | Regras, ações e aceite |
|---|---|---|
| A21 Contas a receber | Origem, pagador, documento, parcela, competência, vencimento, principal, ajustes, baixas e saldo | Aberto → parcial → quitado; vencido calculado pela data/saldo. Recebimento manual parcial/total com conta, data e comprovante. Excedente bloqueado no MVP. Cancelamento não apaga baixa: estorno rastreado |
| A22 Contas a pagar | Fornecedor, recebimento/despesa, parcelas, centro de custo, vencimento, aprovação e saldo | Pedido gera previsão; recebimento/documento confirmado gera obrigação efetiva, com chave de origem para não duplicar. Despesas manuais exigem motivo. Pagamento manual após aprovação e dentro do saldo. Não marcar como transmissão bancária |
| A23 Caixa e contas financeiras | Conta caixa/banco, abertura, entradas/saídas, transferências, fechamento e diferença | Baixas geram movimento na mesma transação. Transferência cria duas pernas e não altera caixa consolidado. Fechamento informa saldo calculado/declarado e motivo de diferença; após fechado, corrigir por estorno autorizado/período aberto |
| A24 Fluxo de caixa | Período, contas, saldo inicial, previsto e realizado | Separar compromissos futuros e movimentos efetivados. Baixa remove saldo previsto correspondente; não somar receita de venda novamente ao caixa. Saldo final = inicial + entradas − saídas. Transferências internas zeram no consolidado |
| A25 Custos e resultado gerencial | Venda/cirurgia, custo do lote, despesas diretas, frete, descontos, comissão e centro de custo | MVP usa custo documentado do lote e despesas atribuídas uma única vez. Margem = receita líquida − custo consumido − despesas diretas − comissão. Mostrar metodologia; não chamar margem de lucro líquido ou balanço contábil |
| A26 Comissões | Vendedor, regra versionada, base elegível, percentual, recebimentos, estornos e pagamento | Um comissionado por venda no MVP. Proposta: comissão sobre principal líquido efetivamente recebido, sem juros/multa. Apropriação proporcional às baixas; estorno reduz comissão. Comissão paga gera AP/saída uma única vez. Sem somar módulos cirúrgico/comercial em duplicidade |
| A27 Cobrança bancária | Títulos, instrução de cobrança, vencimento, canal, situação e referência externa | MVP: preparar/acompanhar instruções e exportar relatório CSV identificado como relatório. Não é arquivo CNAB homologado. P1: adaptador de banco/provedor, registro, retorno, cancelamento e idempotência; liquidação só após confirmação conciliada |
| A28 Pagamentos eletrônicos | Obrigações aprovadas, beneficiário, conta, valor, data e lote de instruções | MVP: preparar e aprovar instruções internas; pagamento manual registrado separadamente. P1: aprovação segregada, transmissão, consulta e retorno. Timeout fica pendente, não pago; não retransmitir cegamente |
| A29 Documentos / NF-e | Origem, emitente, destinatário, itens, dados fiscais, ambiente, chave/protocolo e situação | MVP: pré-documento interno e conferência de referências; sem emissão autorizada. P1: validar tributação, assinar, transmitir, consultar, cancelar e armazenar XML/protocolo. Status externo somente com evidência válida. Não produzir DANFE fictício |
| A30 Controle fiscal | Cadastros fiscais, documentos por competência, pendências e exportação de conferência | MVP registra parâmetros/documentos e pendências; não calcula automaticamente todos os tributos. P1 após definição de UF/regime/operação e revisão profissional: apuração e obrigações aplicáveis. Registro interno não equivale a escrituração fiscal entregue |
| A31 Controle contábil | Plano de contas, centros de custo, lançamentos de conferência, competência e exportação | MVP: lançamentos internos com débitos = créditos, origem única e estorno; exportação para conferência. Não auto-postar origem sem mapeamento válido. P1: integração/fechamento e obrigações com responsável contábil |
| A32 Relatórios | Estoque/lotes, vencimentos, custódia, consumo, compras/vendas, AP/AR, caixa, custos, comissões e qualidade | Consultas paginadas, filtros persistidos, totais equivalentes à exportação. CSV UTF-8 com proteção contra fórmula; impressão pelo template existente. Permissão aplicada às colunas sensíveis |
| A33 Auditoria | Ator, tenant, recurso, operação, antes/depois mínimo, resultado, motivo e correlação | Consulta por período/entidade/usuário, sem edição/exclusão pela interface. Não gravar senha, token, certificado, dados bancários completos ou detalhes clínicos nos logs |

## 5. Regras transversais que evitam bugs

### Estoque, lotes e consignação

1. Quantidades usam decimal e unidade explícita. MVP opera em uma unidade de estoque por produto; conversão de embalagem precisa ser configurada e validada, nunca inferida pelo leitor.
2. Chave de saldo: tenant + produto + lote/série + local + condição + titularidade aplicável. Validade não é identificador único. Um lote pode estar em vários locais.
3. Disponível = físico liberado − reservas ativas sobre esse físico. Quarentena, vencidos, bloqueados e avariados não entram no físico liberado. Não subtrair a mesma restrição duas vezes.
4. FEFO sugere lotes pelo menor vencimento. Expedição exige validade suficiente para a data prevista de uso; prazo adicional é parâmetro, não regra sanitária inventada. Revalidar no servidor ao reservar, separar e expedir.
5. Reservar/expedir executa dentro de transação com lock/controle de concorrência. Duas sessões não podem consumir a última unidade. Saldo negativo proibido.
6. Vale: expedido = consumido + devolvido + perdas/destinações aprovadas + ainda em custódia. Estados parciais são legítimos; encerramento exige zero pendente ou transferência formal para novo vale.
7. Consignação muda local/custodiante, não receita/propriedade automaticamente. Consumo baixa externo; devolução transfere externo para interno sob condição apropriada. Kit tem custódia própria e retorno de componentes.
8. Bloqueio de lote deve alcançar todos os locais e reservas afetadas. Relatório de rastreio mostra fornecedor/recebimento → lote → local → vale → cirurgia/referência → consumo/devolução → venda.
9. Eventos confirmados são imutáveis. Correções geram eventos de estorno vinculados, respeitando movimentos dependentes; não permitir estorno de entrada já consumida sem reconciliar dependências.

### Valores, títulos e idempotência

1. BRL no MVP. Usar decimal, nunca float/double para dinheiro. Proposta: arredondamento monetário a 2 casas, midpoint away from zero, com regra única no domínio; quantidades/custos unitários podem manter precisão maior documentada.
2. Total comercial = soma dos itens após descontos + acréscimos explícitos. Parcelas somam exatamente o total, com resíduo de centavos na última. Desconto não pode tornar item negativo.
3. Custo do lote mantém origem documental. Frete de compra rateado proporcionalmente ao valor dos itens, com resíduo no último elegível; documentos sem base exigem critério explícito. Não reavaliar passado silenciosamente ao editar produto.
4. Separar confirmação comercial de emissão fiscal. Um documento interno não autoriza circulação fiscal de mercadoria; homologação usa dados fictícios. Ativação real depende de validação da operação fiscal.
5. Origem de cada título, movimento e comissão é única por tenant/documento/item/parcela conforme o caso. Idempotency-Key é vinculada ao tenant, operação e hash do payload; mesma chave com conteúdo diferente retorna conflito.
6. Baixa + movimento de caixa + apropriação de comissão + auditoria são atômicos. Falha em qualquer etapa reverte tudo. Reenvio retorna a operação original.
7. Edição concorrente exige versão e retorna conflito com orientação para recarregar. Nunca aceitar silenciosamente o último salvamento.
8. Comissão paga não é apagada por devolução posterior: ajuste/compensação com trilha e aprovação. Não gerar automaticamente cobrança ao vendedor sem regra contratual.

### Logs, privacidade e experiência

Todos os métodos operacionais novos/alterados de controller, serviço, repository e adaptador devem ter observabilidade de início/fim/duração/resultado por `ILogger<T>` ou decorator; exceções correlacionadas sem repetição excessiva. Funções puras de cálculo mantêm pureza e são cobertas pelo log do caso de uso; não inserir efeitos colaterais em getters/DTOs. Auditoria de negócio é persistente e diferente do log técnico.

Cada mutação mostra confirmação com entidade e impacto; ao concluir, informação de sucesso somente após commit e releitura. Em falha, conservar campos e permitir tentar de novo com idempotência. Consulta/filtro mostra carregamento, vazio ou erro; não interromper cada leitura com modal. Destrutivas, aprovação, expedição, baixa, estorno e fechamento exigem confirmação explícita e motivo quando aplicável.

Usar componentes atuais, navegação interna por grupos e páginas separadas. Formulários com labels, validação junto ao campo e resumo acessível, foco no erro, teclado e escape em modal, botão desabilitado durante envio e recuperação no finally. Validar 360, 768 e 1440 px: sem ações fora da tela, seletor pesquisável por nome e tabelas adaptadas.

Cada página contém “Para que serve”, “Quem pode usar”, “Como fazer”, “O que acontece ao confirmar” e “Próximo passo”, com texto específico. Logs/telas de auditoria não expõem dados clínicos e segredos. Paciente, se indispensável, usa referência mínima protegida; não importar prontuário para este módulo.

## 6. Arquitetura e contratos

Preservar .NET 10/C#10, MVC/Razor, PostgreSQL e componentes atuais. Não migrar frontend nem framework para este prazo.

| Camada / localização proposta | Responsabilidade | Proibição |
|---|---|---|
| `PlantaoPro.Domain/Administrativo360/{Estoque,Comercial,Cirurgias,Financeiro,Qualidade}` | Entidades, estados, invariantes, cálculo de saldo/vale/valores | HTTP, SQL e dependência de Razor |
| `PlantaoPro.Application/Administrativo360/{Contratos,CasosDeUso,Consultas}` | DTOs específicos, interfaces de persistência, autorização de caso de uso, transação/orquestração | Dictionary universal e DTO único para todas as páginas |
| `PlantaoPro.Infrastructure/Administrativo360/{Persistencia,Integracoes}` | SQL parametrizado, repositories, transações e adaptadores externos | Decisão comercial escondida na query ou sucesso fictício de integração |
| `PlantaoPro.Api/Controllers/Administrativo360` | Endpoints curtos, validação de contrato, identidade e tradução de erro | Controller único com todos os módulos ou centenas de linhas de regras |
| `PlantaoPro.Web/Controllers/Administrativo360`, Models e Views correspondentes | Navegação, viewmodels, formulários e consumo dos contratos | SQL/cálculo de comissão/regra de liberação no Razor/JavaScript |
| Composition root/DI existente | Referências entre projetos e registro de serviços | Dependência circular ou Application referenciando API |

Uma página de detalhe pode ter abas da própria entidade; não concentrar CRUDs de domínios diferentes em uma única página. Compartilhar componentes visuais, não estado de negócio global entre páginas.

### Contratos mínimos propostos

| Contrato/caso de uso | Entrada essencial | Saída e invariantes |
|---|---|---|
| `CriarProdutoRequest` / `ProdutoResponse` | SKU, unidade e política de lote | ID/versão; tenant vem da sessão |
| `ConfirmarRecebimentoCommand` | Pedido, itens, lotes, quantidades, local, versão e chave | Recebimento, movimentos e obrigações vinculadas, atomicamente |
| `DecidirInspecaoCommand` | Recebimento/porção, aprovada/reprovada, motivo, versão | Condições/saldos recalculados; exige Qualidade |
| `ReservarValeCommand` | Cirurgia, itens/lotes, quantidades, versão | Reserva válida sem saldo negativo |
| `ExpedirValeCommand` | Vale, itens separados, destinatário e versão | Transferências/custódia, sem faturamento automático |
| `ReconciliarValeCommand` | Consumo, devolução, perdas, evidências, versão | Quantidades reconciliadas e pendências |
| `ValorizarValeCommand` | Vale, versão comercial, confirmação | Venda única vinculada ao consumo |
| `BaixarTituloCommand` | Título, conta, valor, data, referência, versão e chave | Baixa, caixa, saldo e comissão; tudo ou nada |
| `RegistrarLeituraCommand` | Tarefa, scanId, código, lote, quantidade | Linha persistida, rejeição clara ou ambiguidade |
| `ConsultarRastreabilidadeQuery` | Produto/lote/referência e filtros | Cadeia autorizada do tenant |
| `EmitirDocumentoFiscal` / adaptador | Documento validado e ambiente configurado | P1; protocolo real ou erro/pendência, nunca simulação de autorizado |

API retorna 400 para formato inválido, 401/403 conforme autenticação/permissão, 404 para recurso inacessível por tenant sem revelar existência, 409 para versão/estado/idempotência conflitantes. Erro de regra segue convenção canônica documentada. Erros trazem código, mensagem útil, erros de campo e correlationId, sem stack trace público. Consultas paginadas com ordenação permitida e limites de exportação.

## 7. Banco, migrações e dados demonstrativos

Tabelas normalizadas propostas, sob schema canônico, prefixo `adm360_`: parceiros; produtos; regularizacoes_produto; locais; lotes; saldos; movimentos; reservas; pedidos_compra/itens; recebimentos/itens; inspecoes; nao_conformidades; medicos; perfis_cirurgia/itens; kits/componentes; cirurgias; orcamentos/itens; vales/itens/eventos; vendas/itens; titulos; baixas; contas_financeiras; movimentos_caixa; centros_custo; despesas; regras_comissao; comissoes; instrucoes_cobranca/pagamento; documentos_fiscais; plano_contas; lancamentos_contabeis/partidas; inventarios/itens; leituras; auditoria; comandos_idempotentes. Reaproveitar tabela existente somente após comprovar equivalência sem misturar finanças clínicas ou assinatura SaaS.

Todas as tabelas de negócio têm tenant obrigatório, identificador, versão, datas/ator e FK coerente. Referências entre entidades devem impedir vínculos cruzados por tenant também no banco, com chaves compostas quando necessário. Índices para tenant/status/data, lote/local, vencimento, origem e consultas reais. Unique para SKU, origem de lançamento e chaves de idempotência. Não usar JSON genérico como substituto das relações essenciais.

Criar migrations novas seguindo manifesto vigente. Não editar migrations aplicadas, alterar checksums históricos ou criar tabelas durante request. Regenerar consolidado e artefatos usando ferramentas canônicas. Testar instalação limpa, upgrade representativo e reaplicação do instalador. Backup/restauração real em banco descartável antes do marco final.

### Cliente e login a provisionar pelo seed

| Campo | Valor proposto |
|---|---|
| Empresa fictícia | NorteMed Distribuição Cirúrgica — Demonstração |
| Identificador | NORTEMED_DEMO |
| E-mail | `admin@nortemed.example` |
| Senha demonstrativa | `NorteMed@Demo2026!` |
| Perfil | Administrador do Administrativo 360 no próprio tenant |
| Contratação | ADMINISTRATIVO_360 ativo exclusivamente no ambiente demonstrativo |

**Esta conta ainda não foi criada nem testada.** O prompt 1 deve provisioná-la pelo SQL e validar login real antes de divulgar como acesso utilizável. Senha compartilhada serve apenas para dados sintéticos de homologação. Guardar no SQL somente hash BCrypt compatível com o verificador atual; não em controller, JS ou seed em memória.

Seed separado em `database/seeds/development`, explicitamente opt-in e recusado em produção por configuração de ambiente confiável. Não inserir clientes fictícios na migration estrutural de produção. UUIDs estáveis, transação e reaplicação sem duplicidade. Não sobrescrever senha existente na reaplicação; reset demonstrativo separado e restrito. Nenhum mock de operação, saldo, relatório ou usuário no código.

Além do administrador, seed deve criar usuários sintéticos restritos de Estoque, Qualidade, Financeiro e Auditor, com credenciais registradas no documento de testes após criação. Criar um segundo tenant para provar isolamento, sem permissão no primeiro. Não conceder papel global à conta demonstrativa.

### Massa e oráculo numérico

Seed versionado inclui 2 fornecedores, 2 hospitais, 2 médicos fictícios, 1 vendedor, 6 produtos, 2 locais internos, 2 externos, 2 kits e lotes em situações livre/quarentena/vencido. Datas derivadas de uma data-base explícita do seed para cenários reproduzíveis, sem alterar histórico ao reaplicar. Regularizações sintéticas identificadas como demonstração; nenhuma afirmação de aprovação real.

Cenário principal isolado, sem frete, tributo ou desconto: receber 10 unidades do lote DEMO-L001 a R$100 cada; liberar as 10; consignar 6; consumir 4 e devolver 2; após inspeção do retorno, saldo interno 6 e externo 0. Vender as 4 por R$150: receita R$600, custo consumido R$400, AR R$600. Receber R$300: AR R$300 e caixa +R$300. Comissão de 5% sobre principal recebido: R$15. AP da compra R$1.000; pagar R$200: AP R$800 e caixa líquido +R$100, partindo de zero e sem outras operações. Custo comercial previsto de comissão integral R$30 deve aparecer separado da comissão apropriada R$15; margem comercial prevista R$170, enquanto resultado com comissão apropriada é R$185, com método identificado para não misturar regimes.

Preparar pelo SQL um cenário inicial para executar na apresentação e outro cenário concluído, claramente identificado, para relatórios. Os saldos da massa concluída precisam ser derivados/coerentes com movimentos e títulos, não números soltos. Ao final do seed, assertions SQL verificam identidades, vínculos, totais e ausência de permissões globais.

## 8. Relatórios mínimos

| Relatório | Filtros e colunas mínimas | Conferência |
|---|---|---|
| Estoque por local/lote | Produto, lote, validade, local, condição, físico, reservado, disponível | Soma dos movimentos; externos separados |
| Rastreabilidade | Produto/lote, fornecedor, entrada, transferências, vale, cirurgia, consumo/retorno | Nenhum elo de outro tenant |
| Vencimentos e quarentena | Data de corte, dias a vencer, local e responsável | Vencido nunca disponível |
| Consignações pendentes | Hospital, cirurgia, vale, expedido, consumido, retornado, pendente, prazo | Equação do vale fecha |
| Comercial/compras | Período, parceiro, documento, status, itens, valores | Cancelados/rascunhos separados |
| AP/AR e caixa | Vencimento, parceiro, conta, principal, baixas, saldo, previsto/realizado | Sem dupla contagem |
| Cirurgia e margem | Cirurgia, receita, custo, despesa, comissão e método | Origem consultável em cada parcela |
| Comissões | Vendedor, venda, baixa, base, taxa, apropriado, pago e estorno | Regra histórica preservada |
| Qualidade | Lote/local, inspeção, decisão, NC, ação e prazo | Decisão e responsável identificados |

## 9. Execução em cinco prompts com fim definido

Não pedir novas funcionalidades após o prompt 5 para considerar o MVP encerrado. Pendências P1/P2 ficam no backlog; defeitos P0 são corrigidos antes de apresentar. Cronograma é alvo condicionado à execução dos gates, não garantia de prazo.

| Bloco | Janela alvo | Dependência | Saída obrigatória |
|---|---|---|---|
| 1 — base e acesso | 24/09 | Nenhuma | Baseline verde, módulo contratável, perfis, login seed e CRUDs básicos |
| 2 — suprimentos | 24–25/09 | 1 aprovado | Compra, recebimento, qualidade, estoque e coleta |
| 3 — distribuição | 25/09 | 2 aprovado | Cirurgia, orçamento, reserva, vale, retorno e venda |
| 4 — fechamento | 25/09 | 3 aprovado | Financeiro, comissão, relatórios e controles internos |
| 5 — homologação | 26/09 antes da apresentação | 1–4 aprovados | Roteiro executado, evidências, backup e versão candidata |

### Prompt 1 — executar primeiro: base, contratação e cadastros

```text
Priorize o Administrativo 360 no repositório devmnsoft/plantaopro. Leia integralmente docs/administrativo360/MVP_REQUISITOS_E_PROMPTS.md; este arquivo é o escopo canônico do MVP de 26/09/2026. Execute somente o bloco 1, com código funcional, migrations e testes. Não entregue apenas plano.

1. Confirme branch, commit, AGENTS.md, alterações locais, stack e instruções do projeto. Preserve o trabalho existente. Na baseline 23b01af a CI 36047353701 falhou em Build e upgrade; leia os logs atuais e corrija a causa, sem presumir causas de versões antigas. Rode build e teste real de instalação/upgrade em PostgreSQL descartável. Sem runtime/banco, registre bloqueio e não declare a base liberada.
2. Reutilize ModuleContractingService, catálogo de features, guardas de módulo/permissão e login atuais. Cadastre ADMINISTRATIVO_360 como um único módulo independente de Saúde360/Plantões. Não invente preço: use proposta quando necessário. Solicitação não ativa; aprovação canônica ativa. Teste não contratado, contratado, suspenso, usuário sem permissão e outro tenant.
3. Corrija a navegação se o limite aplicado antes dos filtros impedir acesso ao módulo. Crie uma entrada e navegação interna por domínio. Preserve os demais perfis. Nenhum acesso deve depender apenas do menu ou de nome de usuário.
4. Crie camadas próprias Domain/Application/Infrastructure para Administrativo360 e ajuste referências/DI sem ciclos. API fina, ViewModels e Razor separados. Não concentre SQL/regra em página ou controller universal.
5. Implemente A02, A03, A04, A05 e A08 com CRUD real, validação, inativação, persistência, pesquisa e autorização. A01 mostra apenas indicadores já implementados. Cadastros de regularização são declaratórios com fonte/data, não aprovação automática. Não mostre itens futuros como concluídos.
6. Crie migrations incrementais e atualize manifests/consolidado conforme o projeto. Seed SQL separado opt-in cria NorteMed Demonstração, admin@nortemed.example, senha NorteMed@Demo2026! com hash BCrypt compatível, contratação e perfis mínimos; sem papel global. Crie tenant de isolamento e usuários restritos. Senha já existente não muda na reaplicação. Massa fictícia fica no SQL, nunca no código.
7. Reutilize design, forms, modal, toast e ajuda atuais. Cada página explica propósito, perfil, passos e efeito. Valide 360/768/1440 px. Confirmação nas mutações, loading/erro e releitura após commit. Logs estruturados e auditoria por operação, sem dados sensíveis.
8. Prove login real da nova conta, persistência após reiniciar, operações CRUD, negações por tenant/perfil/contratação, seed reaplicável e ausência de regressão do login/menus existentes. Testes de strings não substituem fluxo HTTP e banco.

Entregue arquivos alterados, comandos e resultados reais, evidências e estado de cada aceite; informe URL do ambiente somente se existir e tiver sido testada. Não publique/mescle automaticamente. Termine o bloco 1 sem implementar escopos posteriores pela metade. Atualize o quadro de execução com concluído/parcial/bloqueado e prossiga para o próximo bloco apenas após o gate aprovado.
```

### Prompt 2 — compras, qualidade, estoque e coleta móvel

```text
Leia docs/administrativo360/MVP_REQUISITOS_E_PROMPTS.md e a evidência do bloco 1. Execute o bloco 2 mantendo arquitetura, autorização, logs, design, seed SQL e critérios transversais do documento. Não refaça o que já está comprovadamente pronto.

Implemente A09–A15: pedido com aprovação e recebimento parcial; conferência por lote; entrada em quarentena; inspeção com liberação parcial/reprovação; ocorrência/NC; saldo por local/lote/condição; reservas/transações; inventário com escopo congelado e ajuste aprovado; coleta mobile web de recebimento, contagem e separação. Câmera quando suportada, leitor e digitação como alternativas; tarefa/scanId persistidos, lote obrigatório quando necessário. Sem offline e sem números em memória.

Crie interfaces, comandos, consultas e páginas separados. Estoquista não libera qualidade. Reenvio não duplica entrada/obrigação/movimento. Lote bloqueado/vencido não pode ser reservado ou expedido. Mostre FEFO, quantidade disponível, validade e motivo do bloqueio. Não edite saldo diretamente. Compra aprovada gera previsão; recebimento confirmado registra origem para obrigação financeira, sem duplicar quando o financeiro for conectado.

Amplie seed SQL com fornecedores, produtos, locais, lotes válidos/quarentena/vencidos e cenários de divergência. Teste no PostgreSQL duas reservas concorrentes da última unidade, repetição de recebimento, liberação parcial, bloqueio externo, inventário concorrente e estorno com dependências. Valide coleta em viewport móvel, código desconhecido/ambíguo e reenvio da leitura.

Entregue evidência dos saldos e rastreio, build/integração/migrations, screenshots e pendências. Bloco concluído somente com persistência e negações reais; não transforme inspeção em checkbox sem regra.
```

### Prompt 3 — cirurgia, consignação, retorno e venda

```text
Leia o escopo canônico docs/administrativo360/MVP_REQUISITOS_E_PROMPTS.md e valide os gates 1–2. Execute A06, A07 e A16–A20, reutilizando o estoque e contratos já implementados.

Entregue médicos/hospitais e modelos por procedimento; kits com composição/patrimônio e agenda; orçamento versionado; cirurgia com conflitos de kit; reserva e separação por lote; expedição por vale; custódia externa; retorno parcial, consumo, perda/avaria com NC; valorização e venda vinculada. Venda direta usa o mesmo estoque. Crie páginas e contratos próprios; sem classe universal.

Aprovar orçamento não baixa estoque. Expedir vale transfere para externo e não gera receita. Consumo baixa externo uma vez. Devolução passa pela condição de recebimento apropriada. Cancelar cirurgia libera reserva não expedida; o expedido exige reconciliação. Conferência de kit incompleto impede disponibilidade automática. Fechar vale exige expedido = consumido + devolvido + destinações aprovadas, sem pendência escondida. Valorizar usa snapshot aprovado, gera venda única e não baixa o material novamente.

Amplie seed SQL e execute o cenário de 10 unidades recebidas, 6 consignadas, 4 consumidas, 2 devolvidas, saldo interno final 6 e externo zero. Prove falhas de validade, conflito, quantidade excessiva, tenant errado, edição concorrente e reenvio. Relatório de rastreabilidade deve ligar origem, lote, local, cirurgia, vale, consumo e venda. Nenhum prontuário clínico importado.

Entregue fluxo navegável e persistido, impressão identificada de orçamento/vale, testes reais, validação responsiva e quadro de aceite atualizado. Não declare emissão fiscal pronta.
```

### Prompt 4 — financeiro, relatórios e controles de apoio

```text
Leia docs/administrativo360/MVP_REQUISITOS_E_PROMPTS.md e evidências anteriores. Execute A21–A33 no nível MVP descrito, concluindo a venda/compra até o caixa. Integrações bancárias, emissão NF-e real e escrituração completa continuam P1, explicitamente identificadas.

Implemente AP/AR por origem e parcela; baixas manuais parciais/totais; estornos; contas, abertura/fechamento e transferências; caixa previsto/realizado; despesas e custos por cirurgia/venda; comissão versionada sobre principal recebido; auditoria e relatórios filtráveis/exportáveis/imprimíveis. Baixa, caixa e comissão são transacionais/idempotentes. Preservar snapshots e histórico. Contabilidade interna só confirma lançamento balanceado e mapeado; nunca inventar classificação tributária.

Nas páginas bancárias permita preparar/aprovar instruções internas e conferir situação; não rotule relatório CSV como CNAB homologado. Nas páginas fiscais permita pré-documento/referência e pendências; não simule protocolo/autorização. Adaptador não configurado retorna indisponibilidade explícita. Não implemente provedor falso que retorne sucesso. Registre requisitos externos pendentes por integração.

Reproduza o oráculo do documento: venda R$600, custo R$400, recebimento R$300, AR R$300, comissão apropriada R$15, AP original R$1.000, pagamento R$200, AP R$800 e caixa líquido +R$100. Separe comissão prevista integral de apropriada no relatório de margem. Tela e CSV devem bater com banco nos mesmos filtros.

Teste baixa duplicada/concorrente, estorno, transferência sem inflar consolidado, parcelas/centavos, permissão de exportação e vendedor limitado à própria carteira. Atualize seed somente por SQL. Entregue evidência de cada operação; não use cards descritivos como substituto de CRUD/relatório.
```

### Prompt 5 — marco final de apresentação e homologação

```text
Feche o MVP Administrativo360 descrito em docs/administrativo360/MVP_REQUISITOS_E_PROMPTS.md. Suspenda novas funcionalidades e execute a matriz de homologação do documento em ambiente isolado com build/API/Web/PostgreSQL reais.

Corrija regressões P0/P1 da jornada e prove instalação limpa, upgrade, seed reaplicado, login NorteMed, contratação/perfis e tenant de isolamento. Execute compra → quarentena → liberação → cirurgia/orçamento → reserva → expedição → consumo/retorno → valorização/venda → títulos → baixa → caixa/comissão → relatórios. Execute também venda direta, pagamento manual, inventário e leitura móvel. Confira números do oráculo e rastreabilidade.

Teste estoque concorrente, idempotência, conflitos, falha transacional, cancelamentos e estornos. Teste teclado e 360/768/1440 px, ajuda específica, confirmação, erros de servidor e recuperação do loading. Confira que logs possuem correlação e não vazam segredos. Revalide login e menu dos módulos existentes.

Restaure backup em banco isolado e repita smoke. Produza roteiro de 15 minutos, credenciais demonstrativas realmente testadas, matriz com evidências, limitações P1/P2 e procedimento de recuperação. Identifique commit da candidata e ambiente. Não publique nem declare homologação aprovada pelo cliente sem execução/aceite correspondente.

O fim deste ciclo é MVP demonstrável: todos os aceites obrigatórios passam, nenhuma operação retorna sucesso fictício e nenhuma falha crítica de acesso, saldo, dinheiro ou persistência permanece. Se um gate falhar, registre NO-GO e causa concreta; corrija, não dilua o aceite nem acrescente escopo. Após aprovação, encerre o ciclo e mantenha NF-e/bancos/obrigações completas em backlog finito separado.
```

## 10. Matriz de homologação e definição de pronto

| Caso | Ação | Resultado obrigatório |
|---|---|---|
| H01 | Instalar vazio e fazer upgrade de cópia representativa | Sem perda de dados, manifests coerentes e aplicação inicia |
| H02 | Aplicar seed duas vezes; reiniciar aplicação | Sem duplicar, login funciona e dados persistem |
| H03 | Usuário NorteMed acessa módulo | Só tenant e permissões concedidas |
| H04 | Outro tenant, contrato suspenso e perfil restrito acessam API/CSV por ID | Acesso negado sem vazamento |
| H05 | CRUD e reconsulta após refresh | Dados gravados e validação de duplicidade/vínculo |
| H06 | Recebimento parcial e reenvio | Movimento único em quarentena e pedido ainda parcial |
| H07 | Estoquista tenta liberar; Qualidade libera parte | Primeiro negado; parte correta disponível |
| H08 | Reservar vencido/quarentena e duas sessões na última unidade | Bloqueios; nunca saldo negativo |
| H09 | Expedir/consumir/devolver cenário 10/6/4/2 | Interno final 6 após liberação; externo 0; cadeia completa |
| H10 | Cancelar cirurgia com vale expedido e retornar kit incompleto | Reconciliação exigida; kit indisponível |
| H11 | Valorizar vale duas vezes | Uma venda e uma origem de títulos, sem dupla saída |
| H12 | Receber/pagar cenário financeiro e reenvio | AR300, AP800, caixa100 e comissão15 |
| H13 | Estorno/erro intermediário em baixa | Efeitos revertidos coerentemente; nada parcialmente confirmado |
| H14 | Transferência de conta e relatório consolidado | Duas pernas, resultado consolidado inalterado |
| H15 | Inventário aprovado e leitura repetida por retry | Ajuste único e leitura idempotente |
| H16 | Filtro/exportação e vendedor restrito | Totais iguais ao banco; sem carteira/comissão alheia |
| H17 | API/banco indisponível durante envio | Sem sucesso falso; campos preservados e retry seguro |
| H18 | Telas em 360/768/1440 px e teclado | Formulário, ações, modal, ajuda e erro utilizáveis |
| H19 | Fiscal/banco sem provedor | Nenhuma autorização/liquidação externa fictícia |
| H20 | Backup/restauração e smoke do sistema existente | Recuperação comprovada; login/menu preservados |

GO para apresentação: H01–H20 aprovados com evidência real pertinente; não basta teste textual. Sem defeito crítico de segurança/integridade e sem defeito alto que interrompa a jornada. Limitações P1 expostas no roteiro. Aceite de apresentação não equivale a autorização de operação fiscal/sanitária em produção.

Roteiro de 15 minutos: 1 min login/perfil → 2 min compra e qualidade → 3 min orçamento/cirurgia/vale → 3 min consumo/retorno/valorização → 3 min baixas/comissão/caixa → 2 min rastreio/relatórios/mobile → 1 min limites e próximos marcos.

## 11. Backlog posterior fechado

1. NF-e: definir UF, regime, operações de venda/remessa/retorno e responsável fiscal; selecionar integração, credenciais/certificado e ambiente; implementar rejeições, consulta de pendência, eventos e armazenamento; homologar antes de produção. Não hardcodar CFOP/tributação universal.
2. Bancos: definir banco/provedor, contrato e layout/versionamento; cobrança registrada, retorno e conciliação; pagamentos com aprovação, confirmação, reconciliação e tratamento de timeout. Exportação interna não substitui homologação de remessa.
3. Fiscal/contábil: plano/mapeamento validado, apuração e obrigações aplicáveis, integração contábil, fechamento/reabertura e conciliação formal.
4. Qualidade ampliada: matriz regulatória completa com responsável técnico, gestão documental, recolhimento, evidências/retenção, validação de procedimentos e operação dos kits.
5. Evoluções de produtividade: app/offline, sincronização, leitura avançada e comissões múltiplas, somente depois do núcleo estável.

## 12. Referências e limites regulatórios

A Anvisa descreve a RDC 665/2022 como consolidação de boas práticas de fabricação, distribuição e armazenamento de produtos médicos e diagnóstico in vitro. A rastreabilidade, quarentena e registros aqui propostos são requisitos de produto para apoiar o processo; não constituem declaração de conformidade integral nem certificação do software. Aplicabilidade e procedimentos precisam ser avaliados pelo responsável técnico.

Fonte oficial: https://www.gov.br/anvisa/pt-br/assuntos/noticias-anvisa/2022/rdc-665-de-2022

Emissão fiscal envolve certificado, credenciamento e ambiente apropriado; homologação não tem o mesmo efeito jurídico da produção. A página oficial consultada da SEFAZ/SP é histórica e sustenta apenas essa distinção geral; não deve ser usada como especificação técnica atual de layout/tributação. O bloco de integração deve consultar documentação vigente da UF/provedor.

Fontes oficiais: https://www.fazenda.sp.gov.br/nfe/credenciamento.asp e https://www.nfe.fazenda.gov.br/portal/

## 13. Quadro de execução inicial

| Item | Estado nesta entrega |
|---|---|
| Auditoria direcionada e atualização da baseline | Concluídas |
| Requisitos, páginas, contratos, regras, prioridades e prompts | Especificados neste documento |
| Código funcional do Administrativo360 | Não implementado nesta entrega |
| Migrations/seed do Administrativo360 | Não implementados nem executados |
| Conta NorteMed e senha | Definidas; ainda não provisionadas/testadas |
| Build/banco/UI do novo módulo | Não executados |
| Publicação/PR/merge | Não realizados |
| Próxima atividade | Executar prompt 1 e obter baseline funcional |

