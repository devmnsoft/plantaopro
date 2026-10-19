# R61 D-R61-3: todo input de formulario ganha nome acessivel (aria-label pt-BR).
# Regra do orfao (mesma do recon e do contrato R61): input de formulario sem
# aria-label/aria-labelledby, sem asp-for, sem type hidden/submit/button/checkbox/radio,
# sem id referenciado por label[for] no arquivo e sem label envolvente na janela de 220 chars.
# Idempotente: roda ateh zero orfaos; cada execucao insere o proximo rotulo da lista do arquivo.
$ErrorActionPreference='Stop'
$viewsRoot = Join-Path (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))) 'backend\PlantaoPro.Web\Views'

$rotulos = @{
  'Administrativo360\Cadastros.cshtml'        = @('Nome','Documento','SKU','Nome','Codigo de barras','Codigo','Nome')
  'Administrativo360\Cirurgias.cshtml'        = @('Buscar cirurgias')
  'Administrativo360\Coleta.cshtml'           = @('Codigo','Lote')
  'Administrativo360\Estoque.cshtml'          = @('Buscar no estoque')
  'Administrativo360\Inspecoes.cshtml'        = @('Quantidade aprovada','Quantidade reprovada')
  'Administrativo360\Movimentacoes.cshtml'    = @('Quantidade','Motivo')
  'Administrativo360\Ocorrencias.cshtml'      = @('Destino','Justificativa')
  'Administrativo360\OrcamentoEditar.cshtml'  = @('Motivo da revisao')
  'Administrativo360\OrcamentoNovo.cshtml'    = @('Procedimento','Codigo do vendedor')
  'Administrativo360\Orcamentos.cshtml'       = @('Buscar orcamentos')
  'Administrativo360\Parceiros.cshtml'        = @('Buscar parceiros')
  'Administrativo360\PedidosCompra.cshtml'    = @('Fornecedor')
  'Administrativo360\Produtos.cshtml'         = @('Buscar produtos')
  'Administrativo360\Recebimentos.cshtml'     = @('Documento','Lote')
  'Administrativo360\Relatorios.cshtml'       = @('Buscar relatorios')
  'Administrativo360\ValeDetalhes.cshtml'     = @('Motivo')
  'Administrativo360\Vales.cshtml'            = @('Buscar vales')
  'Administrativo360\Cotacoes\ContasPortal.cshtml' = @('Nome da conta','Segredo de referencia')
  'Administrativo360\Cotacoes\Detalhes.cshtml'     = @('Justificativa da substituicao','Motivo do nao atendimento')
  'Administrativo360\Cotacoes\Estabelecimentos.cshtml' = @('CNPJ')
  'Administrativo360\Cotacoes\Relacionamento.cshtml'   = @('Codigo interno')
  'Administrativo360\DocumentosXml\Importar.cshtml'    = @('Nome do arquivo')
  'Administrativo360\Fiscal\Configurar.cshtml' = @('UF','Municipio','Referencia do certificado','CFOP de venda','CFOP de remessa','CFOP de retorno','Nome do responsavel','Observacao')
  'Administrativo360\Fiscal\Nova.cshtml'       = @('Codigo do documento de origem','Nome do destinatario','Documento do destinatario','Descricao do item')
  'Ajuda\Busca.cshtml'                          = @('Termo de busca','Perfil')
  'Comunicacao\Index.cshtml'                    = @('Buscar por tarefa','Tipo','Status')
  'Inteligencia\Dashboard.cshtml'               = @('Hospital','Especialidade')
  'JornadaClientes\Details.cshtml'              = @('Responsavel')
  'MinhaAssinatura\Downgrade.cshtml'            = @('Motivo (opcional)')
  'MinhaAssinatura\Upgrade.cshtml'              = @('Motivo (opcional)')
  'OperacaoAssistida\Checklist.cshtml'          = @('Justificativa','Observacao')
  'OperacaoAssistida\Ocorrencias.cshtml'        = @('Status','Prioridade','Responsavel','Descricao','Solucao aplicada')
  'OperacaoAssistida\Treinamentos.cshtml'       = @('Tema','Perfil','Responsavel','Participantes','Observacoes')
  'Usuario\Admin.cshtml'                        = @('Pesquisar por nome ou e-mail')
  'V112\OperationalPage.cshtml'                 = @('Filtro local')
}
# Acentos para os rotulos que o canal nao aceita em ASCII simples:
$rotulos['Administrativo360\Cadastros.cshtml'][4] = 'Código de barras'
$rotulos['Administrativo360\Cirurgias.cshtml'] | Out-Null
$rotulos['Administrativo360\Estoque.cshtml'] | Out-Null
$rotulos['Administrativo360\Movimentacoes.cshtml'][1] = 'Motivo'
$rotulos['Administrativo360\OrcamentoEditar.cshtml'][0] = 'Motivo da revisão'
$rotulos['Administrativo360\OrcamentoNovo.cshtml'][1] = 'Código do vendedor'
$rotulos['Administrativo360\Orcamentos.cshtml'][0] = 'Buscar orçamentos'
$rotulos['Administrativo360\Relatorios.cshtml'][0] = 'Buscar relatórios'
$rotulos['Administrativo360\Cotacoes\ContasPortal.cshtml'][1] = 'Segredo de referência'
$rotulos['Administrativo360\Cotacoes\Detalhes.cshtml'][0] = 'Justificativa da substituição'
$rotulos['Administrativo360\Cotacoes\Detalhes.cshtml'][1] = 'Motivo do não atendimento'
$rotulos['Administrativo360\Fiscal\Configurar.cshtml'][1] = 'Município'
$rotulos['Administrativo360\Fiscal\Configurar.cshtml'][2] = 'Referência do certificado'
$rotulos['Administrativo360\Fiscal\Configurar.cshtml'][6] = 'Nome do responsável'
$rotulos['Administrativo360\Fiscal\Configurar.cshtml'][7] = 'Observação'
$rotulos['Administrativo360\Fiscal\Nova.cshtml'][0] = 'Código do documento de origem'
$rotulos['Administrativo360\Fiscal\Nova.cshtml'][1] = 'Nome do destinatário'
$rotulos['Administrativo360\Fiscal\Nova.cshtml'][2] = 'Documento do destinatário'
$rotulos['Administrativo360\Fiscal\Nova.cshtml'][3] = 'Descrição do item'
$rotulos['JornadaClientes\Details.cshtml'][0] = 'Responsável'
$rotulos['OperacaoAssistida\Checklist.cshtml'][1] = 'Observação'
$rotulos['OperacaoAssistida\Ocorrencias.cshtml'][2] = 'Responsável'
$rotulos['OperacaoAssistida\Ocorrencias.cshtml'][3] = 'Descrição'
$rotulos['OperacaoAssistida\Ocorrencias.cshtml'][4] = 'Solução aplicada'
$rotulos['OperacaoAssistida\Treinamentos.cshtml'][2] = 'Responsável'
$rotulos['OperacaoAssistida\Treinamentos.cshtml'][4] = 'Observações'

function GetOrfaos([string]$txt) {
  $r = [regex]'<input\b[^>]*>'
  $lista = New-Object System.Collections.Generic.List[int]
  foreach ($m in $r.Matches($txt)) {
    $tag = $m.Value
    if ($tag -match 'aria-label|aria-labelledby|type="(hidden|submit|button|checkbox|radio)"') { continue }
    if ($tag -match 'asp-for=') { continue }
    $idm = [regex]::Match($tag, 'id="([^"]+)"')
    if ($idm.Success -and [regex]::IsMatch($txt, 'for="' + [regex]::Escape($idm.Groups[1].Value) + '"')) { continue }
    $start = $m.Index
    $window = $txt.Substring([Math]::Max(0, $start - 220), [Math]::Min(220, $start))
    if ($window -match '<label\b' -and ($window -notmatch '</label\s*>')) { continue }
    $lista.Add($m.Index)
  }
  return $lista
}

$total = 0
foreach ($chave in ($rotulos.Keys | Sort-Object)) {
  $arquivo = Join-Path $viewsRoot $chave
  if (-not (Test-Path $arquivo)) { throw "arquivo nao encontrado: $chave" }
  $txt = [IO.File]::ReadAllText($arquivo)
  $i = 0
  while ($true) {
    $orfaos = @(GetOrfaos $txt)
    if ($orfaos.Count -eq 0) { break }
    if ($i -ge $rotulos[$chave].Count) {
      throw "$chave : $($orfaos.Count) orfao(s) sem rotulo definido (primeiro em posicao $($orfaos[0]))"
    }
    $pos = $orfaos[0]
    $ins = '<input aria-label="' + $rotulos[$chave][$i] + '" '
    $txt = $txt.Substring(0, $pos) + $ins + $txt.Substring($pos + 6)
    $i++
  }
  if ($i -gt 0) {
    $utf8bom = New-Object System.Text.UTF8Encoding $false
    [IO.File]::WriteAllText($arquivo, $txt, $utf8bom)
    "FIXADO $chave = $i"
    $total += $i
  }
}
"TOTAL_INSERIDOS=" + $total
# Re-verificacao final em todas as views (regra completa do contrato):
$restam = 0
foreach ($v in (Get-ChildItem $viewsRoot -Recurse -Filter *.cshtml | Where-Object { $_.Name -notlike '_*' })) {
  $txt = [IO.File]::ReadAllText($v.FullName)
  if (@(GetOrfaos $txt).Count -gt 0) { $restam++; Write-Output ("REMANESCENTE " + $v.Name + " n=" + @(GetOrfaos $txt).Count) }
}
"ORFAOS_REMANESCENTES=" + $restam
