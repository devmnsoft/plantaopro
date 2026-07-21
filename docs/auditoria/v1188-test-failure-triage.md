# Triagem v1.18.8 — falhas reais v1.18.7

| Gate/teste | Classificação | Causa raiz | Correção v1.18.8 |
|---|---|---|---|
| database-complete-script-clean-install | PRODUCT_GAP | Manifesto de instalação concatenava SQL histórico bruto e criava `perfis` reduzido antes do índice canônico. | Separação de `install-manifest.json`, schema canônico de identidade e gerador apontando para instalação limpa. |
| database-complete-script-idempotency | PRODUCT_GAP | DDL/seed histórico não era idempotente em todos os objetos. | Fontes canônicas com `CREATE IF NOT EXISTS`, `ALTER ... ADD COLUMN IF NOT EXISTS` e seeds protegidos por existência. |
| database-upgrade | PRODUCT_GAP | Migrações de compatibilidade não eram separadas do caminho de instalação limpa. | `migration-manifest.json` oficial para upgrades legados. |
| database-schema-equivalence | DOCUMENTATION_GAP | Comparação misturava instalação limpa e histórico concatenado. | Artefatos dedicados para plano, dependências e conflitos. |
| database-legacy-compatibility | PRODUCT_GAP | Bases legadas com `perfis` reduzido não recebiam colunas canônicas antes dos índices. | Blocos compatíveis antes de `NOT NULL` e índices. |
| build-test | PATH_BUG | Testes montavam caminhos como caminho duplicado de backend para PlantaoPro.Api. | `RepositoryPathResolver` centralizado. |
| repository-security | SECURITY_VIOLATION | `appsettings.json` continha senha local demonstrativa. | Placeholder `__SET_VIA_USER_SECRETS__`. |
| runtime-from-complete-script/auth-e2e/security-access-e2e/swagger-contract | PRODUCT_GAP | Jobs eram pulados após falhas de banco. | Readiness real e ferramenta oficial de banco para desbloquear runtime. |
| implantation-e2e | PRODUCT_GAP | Não existia Central de Implantação. | API `/api/implantacao/*`, view `/Implantacao` e tabelas canônicas. |

Não foram criados TRX sintéticos; as falhas acima foram classificadas para orientar correção real dos gates.
