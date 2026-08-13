# v1.67 — checklist de design e função

- [x] 20 rotas críticas e 8 viewports declarados no smoke.
- [x] `/Planos` classificada como pública e saída em `screenshots/v167`.
- [x] overflow com tolerância de 2 px, limites de cards e sobreposição de shell verificados.
- [x] forms visíveis, dialogs acessíveis e overlays fora do fluxo verificados em runtime pelo smoke.
- [x] Financeiro usa página, KPIs sem números inventados, consolidação dos itens retornados e cards mobile.
- [x] scripts estáticos protegem rotas, viewports e contratos v1.67.
- [x] sem `!important`, `alert()`, `confirm()` ou `href="#"` novos.
- [ ] build/teste/runtime: bloqueados porque `dotnet` não está instalado.
- [ ] screenshots: deliberadamente não geradas sem aplicação executável e sessão autenticada.
