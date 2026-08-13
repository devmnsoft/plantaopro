# Checklist v1.70.2 — admin, billing e notificações

- [x] BFF interrompe a chamada protegida sem token e responde 401 sem expor detalhes internos.
- [x] BFF preserva verbo, query string e corpo e encaminha Bearer obtido de chaves atuais ou legadas.
- [x] Drawer consulta a API, possui loading, vazio e mensagens específicas para 401, 403, 404, rede e servidor.
- [x] Conteúdo da API é inserido com `textContent`; destinos externos são descartados.
- [x] Contador permanece oculto sem quantidade real; não existem notificações de exemplo.
- [x] Minha Assinatura consulta `GET api/minha-assinatura` e exibe somente campos retornados.
- [x] Respostas ausentes geram estado vazio honesto, sem plano, preço, limite, uso ou cobrança fictícios.
- [x] Admin SaaS direciona apenas a controllers/actions existentes e identifica o contexto sem métricas estimadas.
- [x] Relatórios oferecem somente jornadas implementadas; favoritos e agendamento ficam indisponíveis com motivo.
- [x] Configurações direcionam a áreas implementadas e Minha Assinatura.
