# PlantãoPro — Profissionalização SaaS, permissões, UX e white label

Issue de referência: #195

## Objetivo

Transformar o PlantãoPro em um SaaS B2B profissional, vendável, white label, multi-tenant, parametrizável e pronto para demonstração comercial.

Esta rodada deve corrigir o principal problema atual do produto: aparência simples, navegação confusa, permissões inconsistentes e sensação de sistema inacabado.

## Entregas obrigatórias

- Corrigir permissões do `ADMINISTRADOR_GLOBAL` em todas as telas e APIs.
- Padronizar permissões por perfil.
- Implementar ou consolidar uma matriz visual de permissões.
- Criar design system SaaS profissional.
- Reformular layout principal, sidebar, topbar e navegação.
- Reorganizar home por perfil.
- Profissionalizar telas críticas.
- Reforçar white label com preview, templates e aplicação visual real.
- Criar bloqueios profissionais por plano e módulo com CTA de upgrade.
- Fechar fluxos principais do produto.
- Executar QA de permissões, UX, white label, menus e fluxos.

## Perfis a validar

- `ADMINISTRADOR_GLOBAL`
- `ADMINISTRADOR_CLIENTE`
- `ADMINISTRADOR`
- `COORDENADOR`
- `FINANCEIRO`
- `MEDICO`
- `HOSPITAL`
- `PARCEIRO`
- `SUPORTE`
- `AUDITOR`
- `COMERCIAL`
- `CUSTOMER_SUCCESS`

## Fluxos a validar

### Comercial

Landing -> Planos -> Simulador -> Lead -> Proposta -> Conversão -> Tenant -> Admin Cliente -> Onboarding

### Cliente

Login admin cliente -> Portal -> Uso do plano -> Usuários -> Perfis -> White Label -> Hospital -> Médico -> Plantão

### Coordenação

Login coordenação -> Central de escala -> Criar plantão -> Convidar médico -> Confirmar escala

### Médico

Login médico -> Convites -> Aceitar -> Agenda -> Pagamentos -> Solicitar substituição

### Financeiro

Login financeiro -> Pagamentos pendentes -> Confirmar pagamento -> Exportar relatório

### Parceiro

Login parceiro -> Leads -> Propostas -> Clientes vinculados -> Comissões

## Critérios de aceite

- Build verde da API.
- Build verde do Web.
- Admin global acessa toda a plataforma.
- Admin cliente acessa apenas seu tenant.
- Menus respeitam perfil, plano, módulo e tenant.
- API bloqueia acesso indevido.
- White label visual é aplicado de forma perceptível.
- Telas críticas têm acabamento SaaS profissional.
- Bloqueios por plano são claros e comerciais.
- Fluxos principais funcionam de ponta a ponta.
- Auditoria registra ações críticas.
- Documentação reflete apenas funcionalidades implementadas.

## Comandos de validação

```bash
dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj

dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

Se houver testes:

```bash
dotnet test
```

## Observação

Este PR nasce como draft de acompanhamento para a rodada #195. Ele não deve ser marcado como pronto enquanto as implementações reais de código, UX, permissões e QA não forem concluídas.
