# v1.62 — checklist de homologação

## Concluído por análise estática

- [x] 17 rotas obrigatórias inventariadas, incluindo Financeiro e Relatórios.
- [x] Sete viewports preservados com largura **e** altura explícitas.
- [x] Smoke verifica overflow, shell, conteúdo, container, sidebar, topbar, footer, cards, ação primária, drawers e toasts.
- [x] Login e Admin SaaS possuem verificações de raiz específicas.
- [x] Screenshots usam diretório versionado `screenshots/v162`.
- [x] Ausência do estado autenticado é falha, nunca falso positivo.
- [x] Views críticas permanecem sem `href="#"`, `alert()` ou `confirm()` segundo os gates estáticos.
- [x] Ações sem backend continuam indisponíveis e explicadas; nenhum dado demonstrativo foi adicionado.

## Pendente de ambiente real

- [ ] Restaurar, compilar e testar a solução .NET (SDK ausente no contêiner).
- [ ] Subir API/Web e validar conectividade com fontes reais.
- [ ] Gerar storage state de uma conta de homologação autorizada.
- [ ] Executar as 119 combinações de rota e viewport.
- [ ] Inspecionar e aprovar visualmente os screenshots públicos e autenticados.
- [ ] Exercitar permissões, loading, sucesso e erro das ações transacionais.
- [ ] Validar contraste AA com ferramenta de navegador.
- [ ] Confirmar retorno de foco, Escape e navegação por teclado no navegador.

## Regra de aceite

Este checklist não autoriza declarar runtime ou screenshots como aprovados enquanto os itens pendentes não forem concluídos no ambiente de homologação.

