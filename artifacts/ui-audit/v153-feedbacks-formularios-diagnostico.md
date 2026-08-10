# Diagnóstico v1.53 — feedbacks e formulários

## Método
Auditoria estática dos componentes compartilhados, login, recuperação de conta e jornadas prioritárias. A revisão considerou leitura, formulário, validação, iconografia, feedback, confirmação e comportamento responsivo. Não foram criados dados demonstrativos.

## Componentes compartilhados
- `_ConfirmModal`: tinha estrutura Bootstrap funcional, porém sem ícone semântico, impacto explícito ou relações acessíveis completas. Recebeu cabeçalho semântico, área de impacto, `role="dialog"`, descrição e variante destrutiva.
- `_ToastMessages`, `_ToastRegion` e `plantaopro-toast.js`: o contrato aceitava apenas mensagem. Agora aceita título, ícone, severidade, duração, fechamento e sete estados, preservando a API anterior.
- `form-experience.js`: associava apenas erros já renderizados. Agora acompanha mudanças do validador, liga erros por `aria-describedby`, valida intervalos, foca o primeiro erro e informa alterações não salvas.
- `forms.css`, `feedback.css`, `overlays.css` e v1.51: eram camadas pequenas ou específicas. A camada v1.53 consolida componentes funcionais sem apagar compatibilidade.
- `_AuthLayout`, sidebar, topbar e workspace header: shell consistente, mas a autenticação não carregava o mesmo runtime de feedback. O layout de autenticação agora inclui toast e experiência de formulário.
- `_StatusBadge` e `_EmptyState`: contratos reutilizáveis existentes; preservados e incorporados ao mapa v1.53.

## Auditoria por jornada
| Tela | Leitura | Formulário | Validação | Ícone | Feedback | Confirmação | Mobile | Correção aplicada |
|---|---|---|---|---|---|---|---|---|
| Login | Hierarquia boa, erro parecia Bootstrap | Campos pouco agrupados | Erro sem padrão v1.53 | Presente | Loading já existia | N/A | Narrativa longa | Painel de erro, campos e recuperação padronizados; benefícios condensados no mobile |
| ForgotPassword | Card cru e estreito | Sem ajuda | Sem resumo | Bootstrap | Sem contexto de segurança | N/A | Inline style | Nova jornada segura responsiva, resumo e helper |
| ResetPassword | Card cru | Campos sem seção | Sem resumo | Bootstrap | Sem orientação | N/A | Inline style | Card premium, política, autocomplete e mensagens conectadas |
| Plantões | Conteúdo legível | Card específico | Erros Bootstrap | Mistos | AJAX existente | Compartilhada | Rodapé podia dispersar | Formulário/alerta/rodapé sticky v1.53 e estado não salvo |
| Escalas | Densidade operacional | Fluxos espalhados | Predominantemente servidor | Mistos | Confirmação compartilhada | Genérica em legado | Tabelas densas | Beneficia-se de modal, toast e leitura global; migração de markup pendente |
| Convites | Lista orientada a status | Ações em linha | Servidor | Mistos | TempData | Compartilhada | Overflow possível | Runtime compartilhado aplicado; cards mobile específicos pendentes |
| Pagamentos | Alta densidade | Filtros mistos | Servidor | Mistos | TempData | Necessária em ações críticas | Tabela | Toast/modal v1.53 disponíveis; composição financeira específica pendente |
| Pacientes | Formulário em uma única grade | Sem seções | Resumo parcial | Ausentes | Sem dirty state | N/A | Ações pequenas | Seções, LGPD, máscaras, erros conectados e footer responsivo |
| Agendamentos | Campos sem narrativa | Grade crua | Datas sem regra client-side | Ausentes | Sem conflito contextual | N/A | Ações pequenas | Seções, validação temporal, helpers, dirty state e footer |
| Triagem | Informação clínica densa | Campos dependentes | Requer domínio clínico | Mistos | Alertas locais | Finalização crítica | Layout clínico | Componentes v1.53 disponíveis; validação clínica de domínio permanece no backend |
| Consultas | Jornada longa | Seções heterogêneas | Servidor | Mistos | AJAX parcial | Finalização | Rolagem longa | Rodapé/dirty/error runtime disponíveis; migração integral pendente |
| Configurações | Muitos contextos | Formulários isolados | Servidor | Mistos | Alertas locais | Sensíveis | Cards variáveis | Banners e confirmação v1.53 disponíveis; adoção por configuração pendente |
| Relatórios | Filtros e cards variados | Filtros ad hoc | Pouca validação | Mistos | Geração variável | N/A | Tabelas | Loading/empty/update disponíveis; migração por relatório pendente |

## Decisões
1. Compatibilidade incremental: APIs existentes de toast e confirmação continuam válidas.
2. Sem sucesso fictício: mensagens de sucesso continuam disparadas somente por respostas reais/TempData.
3. Falhas humanas: formulários apontam o campo e orientam a revisão, sem stack trace.
4. Mobile: ações viram pilha, modal usa largura útil, toast respeita navegação inferior e touch targets.
