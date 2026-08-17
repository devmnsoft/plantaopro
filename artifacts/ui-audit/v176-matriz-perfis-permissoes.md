# Matriz de perfis e permissões — v1.76.0

Esta matriz documenta a intenção de UX. Ela **não cria autorização**: controllers e políticas do servidor continuam sendo a fonte de verdade, e uma rota proibida deve responder 403 mesmo quando o menu não é exibido.

| Perfil | Menus e leitura | Ações esperadas | Restrições/403 |
|---|---|---|---|
| Admin | Admin SaaS, Configurações, Relatórios, Planos, Minha Assinatura, Auditoria e visão global | Governança e configuração quando houver policy real | Operação clínica não deve ser implicitamente liberada; 403 explica falta de permissão sem expor dados |
| Coordenação | Plantões, escalas, convites, substituições, fechamentos e pendências | Gerenciar cobertura e devolver divergência com motivo | Financeiro sensível somente se policy autorizar |
| Médico | Agenda, plantões, convites, consultas, pagamentos e pendências próprias | Responder convite e atuar somente em vínculos próprios | Dados de outros profissionais/unidades bloqueados no servidor |
| Hospital | Cobertura, equipes, escalas, solicitações e fechamentos da unidade | Acompanhar e operar dentro do escopo da unidade | Sem acesso cross-tenant; dados financeiros conforme policy |
| Financeiro | Faturamento, glosas, fechamentos aprovados, repasses, pagamentos e relatórios financeiros | Conferir e avançar transições financeiras válidas | Sem editar prontuário; origem clínica em leitura mínima |
| Operador | Agenda, check-in, pacientes, triagem e Minha Central | Executar rotinas operacionais conforme vínculo e status | Sem aprovação financeira ou administração SaaS |

## Contrato de apresentação

- Menu ou CTA condicionado ao perfil melhora a descoberta, mas nunca substitui `[Authorize]`, policy, tenant e escopo do recurso.
- Ação somente leitura não recebe aparência de ação mutável.
- Ação sem endpoint fica desabilitada com `title` ou `aria-describedby` indicando a dependência.
- Operações destrutivas ou devoluções exigem diálogo acessível e motivo quando a regra assim determinar; não usam `confirm()`.
- A resposta 403 deve informar que o perfil não possui acesso e oferecer retorno a uma rota real, sem revelar existência ou conteúdo do recurso.
