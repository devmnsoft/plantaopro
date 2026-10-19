# R61 D-R61-1 (fonte definitiva): toda pagina fora do catalogo canonico aporta ViewData["Title"]
# no topo da propria view (mesmo mecanismo das 327 views ja conformes; a topbar lê antes do body).
# Idempotente: pula arquivos que ja tem ViewData["Title"]. Escreve UTF-8 sem BOM (base sem BOM).
$ErrorActionPreference='Stop'
$viewsRoot = Join-Path (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))) 'backend\PlantaoPro.Web\Views'

$pares = @(
  @('Agendamentos','AgendaDia','Agenda do dia'),
  @('Agendamentos','AgendaMedico','Agenda por medico'),
  @('Agendamentos','Calendario','Calendario clinico'),
  @('Agendamentos','Cancelamentos','Cancelamentos'),
  @('Agendamentos','Details','Detalhes do agendamento'),
  @('ClinicaDashboard','Index','Painel da clinica'),
  @('ClinicaFinanceiro','Caixa','Caixa'),
  @('ClinicaFinanceiro','ContasReceber','Contas a receber'),
  @('ClinicaFinanceiro','FechamentoCaixa','Fechamento de caixa'),
  @('ClinicaFinanceiro','Glosas','Glosas'),
  @('ClinicaFinanceiro','Index','Financeiro da clinica'),
  @('ClinicaFinanceiro','Receber','Receber'),
  @('ClinicaFinanceiro','Relatorios','Relatorios financeiros'),
  @('ClinicaFinanceiro','Repasses','Repasses'),
  @('ClinicaUnidades','Create','Nova unidade'),
  @('ClinicaUnidades','Edit','Editar unidade'),
  @('ClinicaUnidades','Index','Unidades'),
  @('Consultas','Index','Consultas'),
  @('Convenios','Autorizacoes','Autorizacoes de convenio'),
  @('Convenios','Contratos','Contratos de convenio'),
  @('Convenios','Create','Novo convenio'),
  @('Convenios','Dashboard','Painel de convenios'),
  @('Convenios','Details','Detalhes do convenio'),
  @('Convenios','Edit','Editar convenio'),
  @('Convenios','Faturamento','Faturamento com convenios'),
  @('Convenios','Glosas','Glosas de convenio'),
  @('Convenios','Index','Convenios'),
  @('Convenios','Planos','Planos dos convenios'),
  @('Convenios','Tabelas','Tabelas dos convenios'),
  @('Escalas','Details','Detalhes da escala'),
  @('Escalas','Substituir','Substituicao na escala'),
  @('MinhaAssinatura','Downgrade','Reduzir plano'),
  @('MinhaAssinatura','Faturas','Faturas da assinatura'),
  @('MinhaAssinatura','Upgrade','Ampliar plano'),
  @('Pacientes','Buscar','Localizar paciente'),
  @('Pacientes','Details','Ficha do paciente'),
  @('Pacientes','Historico','Historico do paciente'),
  @('Pacientes','Prontuario','Prontuario do paciente'),
  @('Pacientes','ResumoClinico','Resumo clinico do paciente'),
  @('PainelChamada','Configuracoes','Configuracoes do painel'),
  @('PainelChamada','Guiches','Guiches'),
  @('PainelChamada','Historico','Historico de chamadas'),
  @('PainelChamada','Salas','Salas'),
  @('PainelChamada','Setores','Setores'),
  @('PainelChamada','Tv','Tela TV'),
  @('Parametrizacoes','Index','Parametros do sistema'),
  @('PendenciasClinicas','Index','Pendencias clinicas'),
  @('Perfis','Details','Detalhes do perfil'),
  @('PlanosSaude','Autorizacoes','Autorizacoes do plano de saude'),
  @('PlanosSaude','Coberturas','Coberturas do plano de saude'),
  @('PlanosSaude','Create','Novo plano de saude'),
  @('PlanosSaude','Details','Detalhes do plano de saude'),
  @('PlanosSaude','Edit','Editar plano de saude'),
  @('PlanosSaude','Index','Planos de saude'),
  @('PlanosSaude','Pacientes','Pacientes do plano de saude'),
  @('Triagem','ClassificacaoRisco','Classificacao de risco'),
  @('Triagem','Create','Novo atendimento de triagem'),
  @('Triagem','Details','Detalhes da triagem'),
  @('Triagem','Edit','Editar triagem'),
  @('Triagem','Fila','Fila de triagem'),
  @('Triagem','HistoricoPaciente','Historico na triagem')
)
# Acentos (o canal corrompe acentos em ASCII; sobrepoe apos o array):
$a = @{
  'Agendamentos/AgendaMedico'='Agenda por médico'
  'Agendamentos/Calendario'='Calendário clínico'
  'ClinicaDashboard/Index'='Painel da clínica'
  'ClinicaFinanceiro/Index'='Financeiro da clínica'
  'ClinicaFinanceiro/Relatorios'='Relatórios financeiros'
  'Convenios/Autorizacoes'='Autorizações de convênio'
  'Convenios/Contratos'='Contratos de convênio'
  'Convenios/Create'='Novo convênio'
  'Convenios/Dashboard'='Painel de convênios'
  'Convenios/Details'='Detalhes do convênio'
  'Convenios/Edit'='Editar convênio'
  'Convenios/Faturamento'='Faturamento com convênios'
  'Convenios/Glosas'='Glosas de convênio'
  'Convenios/Index'='Convênios'
  'Convenios/Planos'='Planos dos convênios'
  'Convenios/Tabelas'='Tabelas dos convênios'
  'Escalas/Substituir'='Substituição na escala'
  'Pacientes/Historico'='Histórico do paciente'
  'Pacientes/Prontuario'='Prontuário do paciente'
  'Pacientes/ResumoClinico'='Resumo clínico do paciente'
  'PainelChamada/Configuracoes'='Configurações do painel'
  'PainelChamada/Guiches'='Guichês'
  'PainelChamada/Historico'='Histórico de chamadas'
  'Parametrizacoes/Index'='Parâmetros do sistema'
  'PendenciasClinicas/Index'='Pendências clínicas'
  'PlanosSaude/Index'='Planos de saúde'
  'PlanosSaude/Autorizacoes'='Autorizações do plano de saúde'
  'PlanosSaude/Coberturas'='Coberturas do plano de saúde'
  'PlanosSaude/Create'='Novo plano de saúde'
  'PlanosSaude/Details'='Detalhes do plano de saúde'
  'PlanosSaude/Edit'='Editar plano de saúde'
  'PlanosSaude/Pacientes'='Pacientes do plano de saúde'
  'Triagem/ClassificacaoRisco'='Classificação de risco'
  'Triagem/HistoricoPaciente'='Histórico na triagem'
}
$corrigidos=0
foreach ($p in $pares) {
  $ctrl=$p[0]; $acao=$p[1]; $titulo=$p[2]
  if ($a.ContainsKey($ctrl+'/'+$acao)) { $titulo=$a[$ctrl+'/'+$acao] }
  $arquivo=Join-Path $viewsRoot ($ctrl+'\'+$acao+'.cshtml')
  if (-not (Test-Path $arquivo)) { throw "view nao encontrada: $ctrl/$acao" }
  $txt=[IO.File]::ReadAllText($arquivo,[Text.Encoding]::UTF8)
  if ($txt.Contains('ViewData["Title"]')) { continue }
  $i=$txt.IndexOf('@{')
  $ins='    ViewData["Title"] = "' + $titulo + '";'
  if ($i -lt 0) {
    # Sem bloco @{ (arquivos que abrem com @model direto): cria o bloco apos a primeira linha.
    $fimLinha=$txt.IndexOf("`n")
    if ($fimLinha -lt 0) { $fimLinha=$txt.Length }
    $novo=$txt.Substring(0,$fimLinha)+ "`n@{" + "`n"+ $ins + "`n}" + $txt.Substring($fimLinha)
    [IO.File]::WriteAllText($arquivo,$novo,(New-Object System.Text.UTF8Encoding $false))
    "INSERIDO_BLOCO_NOVO $ctrl/$acao = $titulo"
    $corrigidos++
    continue
  }
  $fimLinha=$txt.IndexOf("`n",$i)
  if ($fimLinha -lt 0) { $fimLinha=$txt.Length }
  $novo=$txt.Substring(0,$fimLinha)+ "`n"+ $ins + $txt.Substring($fimLinha)
  [IO.File]::WriteAllText($arquivo,$novo,(New-Object System.Text.UTF8Encoding $false))
  "INSERIDO $ctrl/$acao = $titulo"
  $corrigidos++
}
"TITULOS_INSERIDOS=$corrigidos (de $($pares.Count))"
# Varredura final: paginas restantes sem Title (esperado: 2 impressoes, V112, Pendencias/Index)
$restam=@()
foreach ($v in (Get-ChildItem $viewsRoot -Recurse -Filter *.cshtml | Where-Object { $_.Name -notlike '_*' } | Where-Object { $_.FullName -notlike '*Shared*' })) {
  if (-not ([IO.File]::ReadAllText($v.FullName,[Text.Encoding]::UTF8)).Contains('ViewData["Title"]')) {
    $restam += $v.FullName.Replace((Join-Path $viewsRoot '')+'\','')
  }
}
"SEM_TITLE_RESTANTES=$($restam.Count)"
$restam | ForEach-Object { $_ }
