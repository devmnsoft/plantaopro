# Build e runtime — v1.73.0

- Ambiente auditado: contêiner Codex Linux.
- SDK .NET: indisponível (`dotnet: command not found`).
- Restore, build e testes .NET: bloqueados pela ausência do SDK; **não declarados como PASS**.
- Runtime web e screenshots: não executados, pois não foi possível iniciar a aplicação.
- Validação necessária: abrir `backend/PlantaoPro.sln` no Visual Studio/Windows com o SDK compatível, restaurar, compilar em Release e executar `PlantaoPro.Tests`.
- Mitigação estrutural: controller clínico consolidado e gate estático dedicado contra CS0263, CS0101 e CS0111 causados por duplicidade de controllers/actions.
