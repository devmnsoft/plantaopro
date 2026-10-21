# R80 - Mapa legislativo x código + `parametros_legais` como fonte única (bloco B4)

## Escopo do plano

R80 do plano r66a0: "mapa legislativo x código: o que já existe (DF-e mod-55,
cobranças sandbox, relatórios gerenciais) vs o que falta por domínio; tabela
`parametros_legais` (valor, fonte oficial, vigência, ano) como fonte única".

## Mapa legislativo x código (recon desta rodada)

### DF-e / documentos fiscais
| Já existe | Não existe |
|---|---|
| Parser real de XML NF-e/NFC-e/NFS-e: `PlantaoPro.Infrastructure\Administrativo360\DocumentosXmlRepository.cs` (prioridade `infNFe` > ABRASF > NFS-e; NFS-e modelo 67 por `numNFS`) | Emissão própria NF-e/NFC-e (só recebe/importa e pré-emite) |
| Validação de chave (44 dígitos + modelo 55/65; CT-e mod-57 rejeitado): `PlantaoPro.Domain\Administrativo360\CotacoesXmlDashboard.cs` (`ValidarChaveAcesso`); `FiscalPreEmissoes.cs` (`ChaveAcessoValida`) | Cálculo do dígito verificador módulo 11 e geração de chave; CF-e-SAT, CF-e-Serviços 3.0 |
| Pré-nota interna NPE + parâmetros fiscais por tenant (UF/regime/CFOP em jsonb): migração `v2321_adm360_fiscal_parametros_pre_emitidas`, controller `Adm360FiscalWeb` | Configuração SEFAZ por UF, layout NF-e 4.0/NFC-e, homologação externa (bloqueada sem credenciais - P1) |

### Pagamentos / cobranças
| Já existe | Não existe |
|---|---|
| Motor de cobrança SaaS completo com provider SANDBOX: `PlantaoPro.Api\CobrancaService.cs` (`ICobrancaProvider`, checkout anônimo, webhook HMAC-SHA256, dedupe atômico, máquina de estados PAGA/FALHA/ESTORNADA/CANCELADA); rotas `CobrancaController` | Provedor real (Starter/Pagar.me/banco) |
| PIX apenas como rótulo no payload simulado (`"PIX_SANDBOX"` em `CobrancaService.cs`) | QR Code/EMV/copia-e-cola, end-to-end PIX, SPI/DCI (zero ocorrências) |
| Boleto demonstrativo ilustrativo (flag `demo_boleto`, mensagem "sem cobrança real"): `V114ProdutoService.cs` | CNAB 400/240 (remessa/retorno), linha digitável, FEBEN |

### Tributação PF (INSS/IRRF/salário mínimo/teto)
Inexistente no código atual: zero ocorrências de "inss", "irrf", "salario minimo"
ou "teto" no backend (nem constante espalhada). Existe só o enum de regime fiscal
da empresa (`SIMPLES_NACIONAL/LUCRO_PRESUMIDO/LUCRO_REAL` na `v2321`). Esta rodada
fornece a fundação: tabela única + salário mínimo e piso de isenção IRRF 2026;
vetores progressivos/regressivos entram na R81 lendo daqui.

### Trabalhista / eSocial
| Já existe | Não existe |
|---|---|
| Status "FERIAS" de colaborador; relatório de comissões: `Adm360FinanceiroRelatoriosController.cs` | eSocial (zero "esocial"/S-1000/S-12), folha completa (13º, férias+1/3, horas extras, rescisão), FGTS (zero ocorrências) |

### LGPD
| Já existe | Não existe |
|---|---|
| API `api/lgpd` completa: política, consentimentos, solicitações de titular, exportação, anonimização: `LgpdController.cs` + `LgpdService` (SaasEvolutionServices) | Exportação JSON real (hoje grava status GERADA sem conteúdo), anonimização transversal multi-tabela, retenção configurável por tipo de dado |
| Consentimento clínico gravado no cadastro do paciente (`Saude360ClinicalService.cs`) | Aceite obrigatório por usuário x versão de política |

### Calendário fiscal / obrigações acessórias
Zero ocorrências reais de DAS/DCTF/EFD/obrigações. Existe só notificação
operacional genérica (`NotificacoesController`). Esta rodada planta os dias de
vencimento (DAS 20, FGTS 20, DCTFWeb 15 - valores fixados pelo plano r66a0 na R84)
na fonte única; o motor de obrigações/reminders por tenant é a própria R84.

### Relatórios gerenciais financeiros
Já existe `api/administrativo360/relatorios-financeiros` (policy `Adm360.Financeiro`):
vendas, comissões, margem, vencimentos e **DRE gerencial por cliente em
competência** (repo `Adm360FinanceiroRelatoriosRepository`). Faltam: fluxo de caixa
projetado, P&L consolidado multi-unidade, indicadores EBITDA/ROI.

## O que mudou nesta rodada

### Banco: `database/migrations/2026_10_v2363_r80_parametros_legais.sql`
Tabela **global** (sem tenant - parâmetro legal é por país/ano, não por instituição):
`codigo`, `descricao`, `valor` (texto canônico), `fonte_oficial`, `vigencia_inicio/fim`,
`ano`, `created_at/updated_at`, `reg_status`. Índice único parcial `(codigo, ano)`
e índice por `ano`. Idempotente (`create if not exists` + `on conflict do nothing`,
padrão v2362). Registrada em `database/migration-manifest.json` com checksum real
SHA256 (LF normalizado), `dependsOn` em v2362, status ativo.

**Seed 2026 (somente valores com origem citável):**

| Código | Valor | Fonte oficial | Vigência |
|---|---|---|---|
| SALARIO_MINIMO | 1518.00 | MTE/MF - diário oficial (salário mínimo 2026) | 01/01-31/12/2026 |
| PISO_ISENCAO_IRRF_MENSAL | 4554.00 | RIR (DL 3.240/1941) art. 8 - derivado 3 x mínimo | 01/01-31/12/2026 |
| FGTS_ALIQUOTA_MENSAL | 8.00 | Lei 5.107/1966 (CLT) art. 15 | fixo |
| FGTS_VENCIMENTO_DIA | 20 | CLT art. 15 - dia fixado no plano r66a0 (R84) | fixo |
| DAS_SIMPLES_VENCIMENTO_DIA | 20 | LC 123/2006 art. 71 | fixo |
| DCTFWEB_VENCIMENTO_DIA | 15 | IN RFB 2.110/2022 - dia fixado no plano r66a0 (R84) | fixo |

Decisões registradas:
- **valor em texto canônico** (ponto decimal; dígito puro para dia do mês): a
  formatação de exibição (BRL, "dia 20") é do consumidor; evita cast frágil em
  Dapper/Npgsql e preserva exatidão textual do valor oficial.
- **Dias de vencimento seguem os valores fixados pelo próprio plano (R84)** e a
  fonte registra isso explicitamente; quando o motor de calendário (R84) for
  construído, consome essas linhas e revisa a fonte caso o texto normativo exija.
- **Vetores de tributação (faixas INSS/IRRF, teto) ficam na R81**: esta rodada
  entrega a estrutura e só semearia o que tem citação estável hoje; os vetores
  chegam com contrato de vetores oficiais próprio (plano r66a0 R81).
- **Somente leitura via API nesta rodada**: mutações passam por migração
  versionada com fonte citável (trilha já prevista nas colunas updated_at/reg_status).

### Código: leitura da fonte única
- `PlantaoPro.Application\Legislacao\IParametrosLegaisRepository.cs`: contrato
  (`ListarAtivosAsync(ano?)`, `ObterPorCodigoAsync(codigo, ano?)`) + DTO.
- `PlantaoPro.Infrastructure\Legislacao\ParametrosLegaisRepository.cs`: Dapper
  (CommandDefinition, vigência lida em ISO `YYYY-MM-DD` ou "" para fixos).
- `PlantaoPro.Api\Controllers\LegislacaoController.cs`: `GET api/legislacao/parametros`
  (filtro opcional `?ano=`) e `GET api/legislacao/parametros/{codigo}` (ano mais
  alto quando não informado); `[Authorize]` simples - referência jurídica de
  leitura, não dado sensível de tenant.
- DI em `PlantaoPro.Api\Program.cs` (padrão GetConn, ao lado do R72).

### Contrato: `backend/PlantaoPro.Tests/R80ParametrosLegaisContratoTests.cs`
3 fatos: (1) manifesto com hash real + dependência + códigos da seed no SQL;
(2) banco - reexecução idempotente, valores oficiais 2026 conferidos um a um,
fonte obrigatória, vigência anual vs fixo; (3) repositório - filtro por ano,
última linha por código, ISO de vigência e ausência tratada. Suite 1696 -> 1699.

## Próximos passos (B4)

R81 lê SALARIO_MINIMO/PISO daqui e adiciona faixas INSS/IRRF 2026 com vetores
oficiais; R83 (Simples) consome DAS; R84 (calendário) vira motor de obrigações
por tenant sobre FGTS/DAS/DCTFWeb. Nenhuma linha nova entra sem fonte citável.

## Gates desta rodada

- Build A/B Release sem novos avisos e sem erros.
- Suíte completa com apps parados (total agora 1699 com os 3 fatos novos).
- Docscan sobre os arquivos novos/alterados com delta zero.
- Higiene reexecutada após este documento.
