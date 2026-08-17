# Análise da Jornada Financeira MVP — v1.80.0

| Tela | Rota | Controller/action | Origem | Regra esperada | Ação real | Sem backend | Correção | Pendência |
|---|---|---|---|---|---|---|---|---|
| Faturamento Clínico | `/FaturamentoClinico` | `FaturamentoClinico.Index` | Atendimento/Referência retornado | não estimar valor/status | listar, filtrar, abrir origem com ID | aprovar, glosar, exportar | jornada e próxima ação explícitas | endpoints transacionais |
| Financeiro | `/Financeiro` | `Financeiro.Index/Details` | Escala/plantão do pagamento | nulo não vira zero | consultar e detalhar; confirmar/cancelar no detalhe compatível | glosa/repasse | detalhe preserva ausência | DTO financeiro mais amplo |
| Pagamentos | `/Pagamentos` | `Pagamentos.Index` | Plantão/escala retornado | ausência não vira pendência | listar e filtrar | comprovante, contestação, exportação | empty state honesto | endpoints e vínculos |
| Fechamentos | `/Fechamentos` | `Fechamentos.Index` | Plantão operacional | gerar somente aprovado e persistido | consultar fonte | mutações e vínculo financeiro | motivos já explícitos | contrato operacional-financeiro |
| Relatórios | `/Relatorios` | `Relatorios.Index` | fontes por categoria | gerar/exportar somente no backend | relatórios já implementados | biblioteca financeira | ações desabilitadas | endpoints auditáveis |
| Dashboard/Central | `/Home/Dashboard`, `/MinhaCentral` | `Home.Dashboard`, `MinhaCentral.Index` | agregados reais | nenhum KPI inferido | atalhos e dados retornados | novos agregados financeiros | limitação documentada | DTOs por perfil |
