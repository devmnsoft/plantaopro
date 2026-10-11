-- PlantãoPro v2.34.4 (R7-E1): preferência de idioma da interface do usuário.
-- A coluna persiste a escolha do USUÁRIO (precedência canônica: usuário > organização >
-- navegador > pt-BR; ver PlantaoPro.CrossCutting.Localization.UiLanguageCatalog). O padrão
-- da ORGANIZAÇÃO vive em tenants.metadata->>'idiomaPadrao' (jsonb existente, sem estrutura nova).
-- O idioma é INDEPENDENTE de moeda/fuso/fiscal: apenas rótulos da interface variam; formatos
-- permanecem pt-BR (CurrentCulture fixada) e conteúdo clínico/XML fiscal nunca é traduzido.

alter table plantaopro.usuarios
    add column if not exists idioma_preferido varchar(8);

alter table plantaopro.usuarios drop constraint if exists ck_usuarios_idioma_preferido;
alter table plantaopro.usuarios
    add constraint ck_usuarios_idioma_preferido
    check (idioma_preferido is null or idioma_preferido in ('pt-BR','en','es','fr'));

comment on column plantaopro.usuarios.idioma_preferido is
    'Idioma da interface escolhido pelo usuario (pt-BR/en/es/fr). Nulo = resolver por organizacao/navegador/pt-BR. R7-E1.';
