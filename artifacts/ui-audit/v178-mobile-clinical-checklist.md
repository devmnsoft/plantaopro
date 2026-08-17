# Checklist mobile clínico — v1.78.0

Viewports contratados: 360×800, 390×844, 430×932, 768×1024, 1024×768, 1366×768, 1440×900 e 1920×1080.

Rotas: Pacientes, Agendamentos, Saúde 360, Triagem, Consultas, Faturamento Clínico, Dashboard e Minha Central. A camada v178 empilha contexto e ações, mantém alvos mínimos de 44 px e impede cards de exceder a grade. A validação visual real permanece pendente de runtime autenticado; o smoke registra overflow, recorte e sobreposição.
