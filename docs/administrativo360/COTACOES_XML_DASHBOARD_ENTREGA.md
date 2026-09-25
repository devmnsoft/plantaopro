# Administrativo 360 - Central de Cotações, Central de XML Recebidos e Dashboard Gerencial

**Data de Conclusão e Homologação:** 25/09/2026  
**Data Alvo de Apresentação:** 26/09/2026  
**Status dos Testes Automatizados:** 647 Aprovados / 0 Falhas / 0 Regressões  

---

## 1. Visão Geral da Entrega

A ampliação do módulo **Administrativo 360** no **PlantãoPro** incorpora as capacidades estratégicas de aquisições cirúrgicas e controle fiscal baseadas na referência funcional NORTEVITALIS / VS|OPME, implementadas como código nativo e sustentável no ecossistema PlantãoPro, preservando a segregação multi-tenant, persistência real no PostgreSQL e contratos canônicos estritos.

As três áreas entregues são:
1. **Central de Cotações Pré-Cirúrgicas:**
   - Integração canônica preparada para **OPMENEXO / BIONEXO** e **INPART Saúde**.
   - Importação e relacionamento (De/Para) de itens e produtos OPME com fator de conversão de unidades.
   - Enlace automático com o orçamento cirúrgico (`plantaopro.adm360_orcamentos`).
   - Fila de outbox com idempotência, snapshot imutável da proposta e política de retry seguro (sem retransmissão cega para cotações concluídas).
2. **Central de XML Recebidos (NF-e Modelo 55):**
   - Recepção estrita de documentos fiscais eletrônicos modelo 55 emitidos contra estabelecimentos autorizados do tenant.
   - Leitor seguro com proibição explícita de DTD e entidades externas (mitigação definitiva de ataques XXE).
   - Quarentena técnica imediata para XML malformado, chaves divergentes ou CNPJ de destinatário não autorizado.
   - Rastreabilidade com eventos de manifestação do destinatário e conferência cega/física integrada sem duplicação de contas a pagar ou recebimento.
   - Sincronização DF-e (`NFeDistribuicaoDFe`) com controle de NSU e respeito à janela de intervalo da SEFAZ.
3. **Dashboard Gerencial e Monitor 360:**
   - Indicadores agregados em tempo real diretamente via consultas analíticas otimizadas no banco de dados.
   - Filtros por período, estabelecimento e canal/provedor com isolamento multi-tenant rígido.
   - Exportação de dados operacionais e analíticos para CSV.
   - Toggle por tenant no cadastro de capacidades contratadas (`PORTAIS_COTACAO`, `XML_RECEBIDOS`, `DASHBOARD_GERENCIAL`).

---

## 2. Status das Integrações com Portais

| Portal / Conector | Protocolo / Formato | Status Atual | Comportamento Operacional |
| :--- | :--- | :--- | :--- |
| **OPMENEXO / BIONEXO** | REST JSON / API Oficial | `BLOQUEADA` / `NAO_CONFIGURADA` | Exige preenchimento de credenciais válidas e homologação oficial. Sem credenciais, nenhuma chamada fictícia de sucesso é realizada. |
| **INPART Saúde** | SOAP XML / API Parceiro | `BLOQUEADA` / `NAO_CONFIGURADA` | Exige certificado digital e token de autorização. Bloqueado por padrão para proteção das rotinas hospitalares. |
| **Importação Manual / Arquivo** | XML NF-e 4.00 / CSV / JSON | `ATIVO` | Importação direta com validação estrutural completa, quarentena técnica e geração de log de auditoria. |
| **SEFAZ DF-e** | SOAP NFeDistribuicaoDFe | `AGUARDANDO_CERTIFICADO` | Validação de NSU e intervalo de 60 minutos entre consultas ativas; exige Certificado A1 ICP-Brasil para comunicação em produção. |

---

## 3. Estrutura de Tabelas e Migrações

- **Arquivo de Migração:** `database/migrations/2026_09_v2196_administrativo360_cotacoes_xml_dashboard.sql`
- **Script Completo Consolidado:** `database/scrpt_completo.sql`
- **Manifesto de Migrações:** `database/migrations/migration-manifest.json`
- **Script Demo para pgAdmin Query Tool:** `database/pgadmin/administrativo360_demo_completo.sql`

### Principais Tabelas:
- `plantaopro.adm360_capacidades_contratadas`: Controle de módulos contratados por tenant.
- `plantaopro.adm360_portal_contas`: Contas e credenciais criptografadas por canal de cotação.
- `plantaopro.adm360_cotacoes`: Cabeçalho das cotações pré-cirúrgicas capturadas.
- `plantaopro.adm360_cotacao_itens`: Itens cotados, relacionamento De/Para e conversão de unidades.
- `plantaopro.adm360_cotacao_anexos`: Anexos técnicos e laudos das cotações.
- `plantaopro.adm360_cotacao_respostas`: Fila outbox de propostas enviadas ou agendadas para envio.
- `plantaopro.adm360_documentos_recebidos`: Documentos fiscais (NF-e modelo 55) recebidos e conferidos.
- `plantaopro.adm360_documento_itens`: Itens fiscais vinculados aos produtos internos com fator de conversão.
- `plantaopro.adm360_documento_eventos`: Histórico e auditoria de eventos e manifestações do destinatário.
- `plantaopro.adm360_dfe_sincronizacoes`: Controle de NSU e histórico de sincronizações SEFAZ.

---

## 4. Credenciais de Demonstração (Santa Casa)

Para validar o fluxo completo e realizar a demonstração executiva:
- **E-mail:** `gestor@santacasa-demo.example`
- **Senha Inicial:** `SantaCasa!Demo2026#Gestor`
- **Tenant:** `Santa Casa Matriz` (`d3f6584c-2c64-4e5a-9ea9-4e1428647502`)
- **Papel:** `GESTOR_ADMINISTRATIVO_360` (com todas as políticas de permissão do Administrativo 360 liberadas).

---

## 5. Instruções para Execução do Roteiro de Demonstração no pgAdmin

1. Abra o **pgAdmin 4** e conecte-se à instância PostgreSQL (porta 5432).
2. Selecione a base de dados `plantaopro_test` (ou a base do ambiente de homologação).
3. Abra a ferramenta **Query Tool**.
4. Carregue o arquivo:
   `database/pgadmin/administrativo360_demo_completo.sql`
5. Execute o script (`F5`).
   - O script é 100% puro SQL (sem comandos específicos de terminal como `\c` ou `\i`).
   - É idempotente: pode ser executado repetidas vezes sem conflito de chaves primárias.
   - Ao final, exibirá o resumo com a contagem de registros criados, cotações, orçamentos, documentos fiscais e usuário gestor validado.

---

## 6. Cobertura dos 18 Critérios de Aceitação

A suíte de testes em [Administrativo360CotacoesXmlDashboardTests.cs](file:///c:/MNSOFT/plantaopro/backend/PlantaoPro.Tests/Administrativo360CotacoesXmlDashboardTests.cs) comprova:
- **Aceite 01:** Capacidade desligada bloqueia endpoints e interfaces.
- **Aceite 02:** Usuário demo autentica com hash BCrypt oficial e permissões completas.
- **Aceite 03:** Isolamento estrito multi-tenant (dados da Santa Casa invisíveis a outros tenants).
- **Aceite 04:** Cotações OPMENEXO e INPART capturadas com idempotência e sem duplicação.
- **Aceite 05:** Anexos com metadados e tipo MIME válidos.
- **Aceite 06:** Relacionamento De/Para com conversão correta de unidades.
- **Aceite 07:** Itens pendentes ou não atendidos sem justificativa barram envio de resposta.
- **Aceite 08:** Cotação aprovada vinculada a orçamento cirúrgico e na fila outbox com snapshot imutável.
- **Aceite 09:** Retry respeita outbox e não retransmite cotações já aceitas.
- **Aceite 10:** Transição de status bloqueia retrocessos inválidos.
- **Aceite 11:** Sem credenciais externas não existe sucesso fictício (integração bloqueada).
- **Aceite 12:** Importação de XML repetido é idempotente e não duplica registros.
- **Aceite 13:** XML destinado a CNPJ não autorizado do tenant entra em quarentena técnica.
- **Aceite 14:** XML malformado ou com entidades externas (XXE) é rejeitado e entra em quarentena técnica.
- **Aceite 15:** Importação de XML não cria contas a pagar ou recebimento automaticamente.
- **Aceite 16:** Dashboard gerencial calcula indicadores a partir de consultas SQL reais.
- **Aceite 17:** Exportação em CSV reflete exatamente os filtros e dados do tenant.
- **Aceite 18:** Regras de domínio e validação estrita de chave de acesso NF-e modelo 55.
