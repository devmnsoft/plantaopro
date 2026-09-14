# PlantãoPro v2.16.8 — conferência, integridade e design

## Base e diagnóstico

Base confirmada: `fb7aa73` (merge da v2.16.7), sem alterações posteriores no início do trabalho. Um commit vazio (`1f51fae`) registrou esse ponto antes da implementação.

| Caminho | Problema confirmado / primeiro erro | Correção | Evidência prevista |
|---|---|---|---|
| Test | `DecideAsync` projetava `PresencaId`, mas o construtor esperava `PresenceId`; a materialização Dapper não tinha correspondência nominal | read models internos mutáveis, aliases citados e conversão explícita | teste PostgreSQL/Dapper e testes de contrato |
| Instalação limpa | o backfill consultava/alterava estado sem distinguir legado de registro já processado; a ordem passou a ser relevante ao tornar o predicado seguro | tabelas de evidência são criadas antes do backfill e o predicado exclui correções/histórico | instalação limpa e validação do consolidado |
| Upgrade | editar a migration aplicada causaria divergência de checksum | fonte histórica byte a byte preservada; v2.16.8 é migration aditiva e o manifesto aponta a fonte histórica congelada | upgrade pelo ledger oficial |
| Replay | `UPDATE` irrestrito reabria `APROVADA`, `CORRECAO_PENDENTE` e `AJUSTE_POS_APURACAO` | backfill do consolidado só inicializa linhas sem evidência de processamento; migration 410 nunca recalcula status | snapshot antes/depois do replay |
| Jornada normal | a consulta começava em `medico_presenca_correcoes` e omitia execuções corretas | consulta passa a começar em `medico_checkins`; aprovação normal ganhou comando versionado | entrada → saída → aprovação operacional |
| Concorrência | decisão validava só a versão da correção e não conferia linhas afetadas | presença e correção são bloqueadas, a versão-base é revalidada e contagens diferentes de 1 abortam a transação | conflito checkout/proposta/decisão |
| Pós-apuração | a mensagem afirmava encaminhamento sem criar ajuste/evento financeiro | mensagem e histórico declaram precisamente uma **pendência** e preservam os valores consolidados | repetição sem nova origem financeira |

## Regras persistidas

- `REGISTRO_INCOMPLETO`: existe entrada sem encerramento; não pode ser aprovada.
- `PENDENTE`: entrada e saída completas, disponível para aprovação operacional normal.
- `CORRECAO_PENDENTE`: proposta criada sobre uma versão específica da presença; checkout posterior preserva essa indicação.
- `APROVADA`: horários operacionais aprovados, sem autorizar pagamento por este comando.
- `AJUSTE_POS_APURACAO`: decisão operacional posterior à consolidação; somente a pendência auditável é registrada e nenhum valor financeiro é modificado.
- O autor da correção não pode decidi-la, mesmo acumulando perfil de gestão.
- Recusa preserva os horários registrados no histórico e retorna a presença à conferência normal.

## Interface

O profissional visualiza datas completas, quatro origens de horário, estado, próxima ação e fuso capturado. A consulta cobre histórico pesquisável de até um ano em vez de somente 24 horas. O link de acompanhamento é real, IDs de ajuda são únicos e o botão é reabilitado pelo ciclo de navegação (`pageshow`), não por sucesso presumido após 15 segundos.

## Resultados desta execução

- Gerador oficial atualizou o SQL consolidado, instalador pgAdmin, hashes e catálogo de fontes.
- Validação de manifesto e equivalência estrutural do consolidado: aprovada.
- Gate C# 10/Razor: aprovado.
- Build/testes .NET e cenários PostgreSQL/E2E: não executados neste agente porque `dotnet` e um PostgreSQL descartável não estão instalados.
- Screenshot da aplicação real: não produzida pela mesma limitação; não foi criada imagem sintética para substituir evidência operacional.

## Roteiro de aceite com PostgreSQL descartável

1. Instalar pelo consolidado e capturar o primeiro erro, se houver.
2. Aplicar upgrade partindo do ledger da v2.16.7; confirmar que o checksum histórico permanece aceito e que somente a 410 é adicionada.
3. Criar presenças representando todos os estados, versões `bigint`, timestamps e nulos; executar novamente o consolidado e comparar estado, versão, valores e histórico.
4. Executar em duas conexões: checkout versus decisão; proposta versus decisão; decisão versus consolidação. Confirmar bloqueio, conflito 409 e rollback sem histórico de sucesso.
5. Com dois clientes e unidades distintas, validar listagem, decisão, autoaprovação e revogação com sessão aberta.
6. No navegador real: login → entrada → saída → aprovação normal; depois nova execução → correção → decisão → acompanhamento → pendência pós-apuração, capturando screenshots desktop e mobile.
