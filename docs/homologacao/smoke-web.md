# Smoke Web — PlantãoPro

Data da rodada: 2026-07-07.

## Status real do ambiente

Classificação: **Bloqueado por ambiente** para execução local completa nesta rodada.

O executor atual não possui `dotnet`, portanto não foi possível subir `backend/PlantaoPro.Web` para validar respostas HTTP reais das telas. O roteiro abaixo fica reproduzível para o próximo executor com .NET 10 SDK preview instalado.

## Comando de subida

```bash
dotnet run --project backend/PlantaoPro.Web
```

## Rotas obrigatórias do menu principal

Executar com usuário admin/coordenação e confirmar que nenhuma rota retorna 404 ou 500:

| Tela | Rota esperada | Resultado desta rodada |
| --- | --- | --- |
| Login | `/Account/Login` | Bloqueado: `dotnet` não encontrado |
| Dashboard | `/Dashboard` | Bloqueado: `dotnet` não encontrado |
| Pacientes | `/Pacientes` | Bloqueado: `dotnet` não encontrado |
| Agendamentos | `/Agendamentos` | Bloqueado: `dotnet` não encontrado |
| Plantões | `/Plantoes` | Bloqueado: `dotnet` não encontrado |
| Escalas | `/Escalas` | Bloqueado: `dotnet` não encontrado |
| Financeiro | `/Financeiro` | Bloqueado: `dotnet` não encontrado |
| Convênios | `/Convenios` | Bloqueado: `dotnet` não encontrado |
| Ajuda | `/Ajuda` | Bloqueado: `dotnet` não encontrado |

## Critério de aprovação

A rodada só pode ser marcada como homologável quando todas as rotas acima responderem 200/302 esperado, o login admin abrir dashboard e o menu visível não produzir 404/500.
