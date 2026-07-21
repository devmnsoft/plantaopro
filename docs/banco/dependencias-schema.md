# Mapa de dependências do schema

- `planos` → `clientes`, `tenants`, `plano_recursos`, `plano_modulos`, `plano_precos`, `plano_limites`, `assinaturas`.
- `clientes` → `tenants`, `usuarios`, `hospitais`, `medicos`, módulos SaaS, clínicos e financeiros.
- `tenants` → white label, onboarding, permissões, relatórios e auditoria.
- `usuarios/perfis/permissoes` → autorização, auditoria e relatórios.
- `hospitais/especialidades/medicos` → plantões, escalas, pagamentos, disponibilidade e substituições.
- `pacientes/agendamentos/consultas` → triagem, prescrições, CID, convênios, guias e relatórios clínicos.
- `contas_receber/caixas/repasses/glosas` → relatórios financeiros, eventos financeiros e outbox.

A ordem oficial no manifesto cria bases SaaS antes de qualquer `ALTER TABLE plantaopro.planos`, tabelas clínicas antes de views/relatórios e tabelas financeiras antes de triggers/lotes.
