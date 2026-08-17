# Módulo atual e próximos módulos — v1.77.0

## Módulo atual
**Central de Fechamento do Produto MVP.** O objetivo é converter o inventário v176 em uma sequência verificável de encerramento, tratando primeiro build/rotas e o núcleo de entrada e navegação.

## Fechado nesta PR
- Dashboard permanece disponível ao médico e explicita a prioridade de Admin, Coordenação, Médico, Hospital, Financeiro e Operador.
- Falha/ausência da API deixa de renderizar KPIs zero como se fossem dados reais.
- Smoke, runners, CSS aditivo e gates passam a possuir contrato v177.
- Backlog, regras, rotas, permissões, empty states e bloqueio de homologação foram consolidados.

## Pendente
Build, runtime, screenshots e transições por status exigem SDK .NET, serviços reais e sessões válidas. Endpoints parciais de triagem, consulta, plantões, escalas, fechamento e relatórios não foram mascarados.

## Próximo módulo recomendado e ordem
1. **Base/shell e homologação P0**; 2. **Login, cadastro e dashboard P1**; 3. **Jornada clínica**; 4. **Jornada operacional**; 5. **Jornada financeira**; 6. **Admin SaaS**; 7. **Relatórios/notificações**; 8. **mobile/polimento**.

A ordem reduz risco cedo: primeiro prova que o produto inicia e navega, depois fecha aquisição e orientação, e só então homologa transações dependentes. O ganho esperado é um caminho vendável e testável de ponta a ponta. Se não for seguida, polimento pode esconder falhas de autorização, vínculo e transição, elevando retrabalho e risco clínico/financeiro.
