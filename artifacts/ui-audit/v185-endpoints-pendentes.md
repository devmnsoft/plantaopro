# Endpoints pendentes — v1.85.0

- Fechamentos: aprovação, devolução e geração financeira continuam sem endpoint novo porque a tela recebe um agregado vazio e não há repository transacional comprovado.
- Financeiro genérico: aprovação/contestação por item aguardam uma identidade única entre origens financeiras.
- Pagamentos: a geração/confirmação existente não foi mascarada por aliases; a UI ainda precisa de capacidades por registro.
- Admin SaaS: preferências aguardam contrato tenant-scoped completo.
- Presença dedicada: a escala é persistida como realizada, mas não existe coluna/tabela própria de timestamp de presença; nenhum schema foi inventado.
