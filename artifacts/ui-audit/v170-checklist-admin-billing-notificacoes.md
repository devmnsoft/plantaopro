# Checklist v1.70.1 — admin, billing e notificações

- Notificações: fonte real via BFF, contador oculto sem dados, destino limitado à mesma origem e estados 401/403/404/rede.
- Assinatura: `GET api/minha-assinatura`, suporte ao envelope padrão e estado honesto quando não vinculada.
- BFF operacional: token atual/legado/claims, 401 antes da chamada, método e query preservados.
- Admin SaaS: revisão estática sem inclusão de plano, limite ou cobrança fictícios.
- Evidência runtime: pendente de aplicação e credenciais locais válidas.
