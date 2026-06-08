# Manual de Uso Interativo

O manual foi implementado como módulo Web/API, não apenas documentação estática.

## Recursos

- Tópicos por perfil.
- Artigos com links para telas do sistema.
- Busca por termo.
- Checklists de primeiros passos.
- Feedback “Foi útil?”.
- Conteúdos para administrador global, coordenação, médico, financeiro e hospital.

Endpoints principais: `GET /api/ajuda/topicos`, `GET /api/ajuda/artigos`, `GET /api/ajuda/buscar`, `POST /api/ajuda/artigos/{id}/feedback`.

## Complemento 2026-06-08 — ajuda contextual SaaS

O SQL incremental da rodada inclui tópicos e artigos iniciais por perfil para o manual interativo: administrador global, coordenação, médico, financeiro e hospital. Os artigos orientam uso do dashboard SaaS, inteligência, publicação de plantões, aceite de convites, confirmação de pagamentos e acompanhamento hospitalar, com links diretos para telas do sistema.
