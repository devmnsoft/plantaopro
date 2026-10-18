-- R56 (r53b1 §7 item 2): decisão de produto de copy "Dashboard" -> "Painel" (D-R56-1).
-- Catálogo de módulos/capacidades modulos_sistema: renomeia os NOME DE EXIBIÇÃO das linhas cujo
-- nome usava o empréstimo inglês — é esse nome que o tenant vê em Minha Assinatura (Módulos), no
-- fluxo de contratação e nas telas de gestão de permissões. Os códigos permanecem estáveis:
-- grants/permissões referenciam modulo_id/codigo (permissões DASHBOARD.* inalteradas).
-- Convenção da tabela: nomes em ASCII (sem acentos), alinhado às linhas existentes.
-- Idempotente: só atualiza linhas cujo nome ainda não seja o valor renomeado.
update plantaopro.modulos_sistema set nome='Painel' where upper(codigo)='DASHBOARD' and nome <> 'Painel';
update plantaopro.modulos_sistema set nome='Painel Clinico' where upper(codigo)='CLINICA_DASHBOARD' and nome <> 'Painel Clinico';
