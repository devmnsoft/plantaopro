# Matriz v1.70 — empty states

| Contexto | Condição real | Mensagem/conduta |
|---|---|---|
| Drawer | API retorna lista vazia ou filtro sem itens | “Tudo em dia”; nenhum contador é exibido |
| Drawer | 401 | Informa expiração da sessão |
| Drawer | 403 | Informa ausência de permissão |
| Drawer | 404 | Informa indisponibilidade da central no ambiente |
| Drawer | rede/servidor | Exibe erro e ação real de tentar novamente |
| Assinatura | API sem contrato/404 | Informa que billing não retornou contrato; não mostra plano, valor ou vencimento |
| Relatórios | recurso sem persistência | Card desabilitado com dependência explícita, sem CTA |
| Dashboard | coleções reais vazias | Empty states específicos de agenda, cobertura, financeiro e notificações |
