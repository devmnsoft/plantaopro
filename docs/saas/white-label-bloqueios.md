# White label e bloqueios comerciais

## Implementado

- Componente `_ModuleLockedNotice` para indicar módulo bloqueado por plano.
- Componente `_UpgradeCta` para direcionar upgrade.
- Componente `_PlanLimitReachedNotice` para limites atingidos.
- Portal Cliente exibe white label básico no plano Profissional e bloqueia assets avançados com mensagem amigável.

## Regras refletidas na UI

- Essencial: bloqueia white label avançado.
- Profissional: libera configuração básica.
- Enterprise: libera experiência completa.
- Revendedor: habilita operação multi-cliente conforme regra comercial.

## Pendências reais

- Persistir configuração publicada em armazenamento versionado.
- Validar upload de assets por tamanho, formato e antivírus.
