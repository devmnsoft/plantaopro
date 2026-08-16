# Funcionalidades abertas — v1.75.0

| Funcionalidade | Tela | Situação atual | Ação real existente | Ação desabilitada e motivo | Pendência backend | Pendência UX | Prioridade |
|---|---|---|---|---|---|---|---|
| Exportação do faturamento | Faturamento Clínico | Sem endpoint confirmado | Filtros e navegação contextual | Exportar permanece indisponível quando não há rota real | Implementar exportação autorizada | Validar arquivo e feedback no runtime | Alta |
| Pagamento sem vínculo | Pagamentos | Dados podem chegar sem identificador | Consulta dos registros reais | Abertura desabilitada quando falta vínculo | Garantir identificador no contrato | Homologar motivo exibido | Alta |
| Upgrade/downgrade | Minha Assinatura | Depende do provedor de billing | Consulta da assinatura real | CTA indisponível sem destino configurado | Integrar checkout/portal | Homologar retorno do provedor | Alta |
| Geração de relatórios | Relatórios | Disponibilidade varia por relatório/permissão | Catálogo autorizado | Gerar/exportar indisponível sem action | Implementar jobs e download | Feedback de processamento | Média |
| Smoke autenticado | Todas as telas privadas | Runner pronto, não executado | Comando com storage state real | Não cria usuário ou sessão fake | Disponibilizar runtime/SDK | Capturar e revisar screenshots | Alta |
