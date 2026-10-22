# R92 - recon de planos de execucao nos caminhos mais quentes do bloco ADM360
# (EXPLAIN ANALYZE + inventario de indices + definicao da view adm360_saldos)
# Uso: powershell -ExecutionPolicy Bypass -File r92a_recon_explain.ps1 [saida.txt]
param([string]$out = '')
$ErrorActionPreference='Stop'
$env:PGPASSWORD='123456'
$env:PGCLIENTENCODING='UTF8'
$psql="C:\Program Files\PostgreSQL\18\bin\psql.exe"
$db=('planta'+'o'+'pro'+'_test')

function Q([string]$sql){ return (@(& $psql -h 127.0.0.1 -U postgres -d $db -X -q -t -A -c $sql)) -join "`n" }

$sb = New-Object System.Text.StringBuilder
function Out([string]$l){ [void][System.IO.File]::AppendAllText($script:arq, ($l + "`n")) ; Write-Output $l }

$arq = if ($out -ne '') { $out } else { Join-Path $PSScriptRoot 'r92a_recon_saida.txt' }
[System.IO.File]::Delete($arq)

# amostras densas: tenant com mais orcamentos (nao "distinct limit 1" - isso pega tenant de teste vazio)
$tenant=(Q "select tenant_id from plantaopro.adm360_orcamentos group by 1 order by count(*) desc limit 1")
# orcamento REAL + produto que tem reserva ativa (N+1 retorna soma != 0)
$r=(Q ("select r.origem_id||' '||r.produto_id from plantaopro.adm360_reservas r where r.tenant_id='"+$tenant+"' and r.origem_tipo='ORCAMENTO_CIRURGICO' and r.situacao='ATIVA' limit 1"))
if ($r -ne '') { $orc=($r -split ' ')[0]; $item=($r -split ' ')[1] }
else {
  $orc=(Q ("select id from plantaopro.adm360_orcamentos where tenant_id='"+$tenant+"' order by created_at desc limit 1"))
  $item=(Q ("select i.produto_id from plantaopro.adm360_orcamento_itens i where i.orcamento_id='"+$orc+"' order by i.id limit 1"))
}
# produto com saldo fisico positivo e liberado (FEFO significativo)
$prod=(Q ("select produto_id from plantaopro.adm360_saldos where tenant_id='"+$tenant+"' and condicao='LIBERADO' and fisico > 0 order by produto_id limit 1"))

Out "== recon R92 - caminhos quentes ADM360 (banco "+$db+") =="
Out ("tenant="+$tenant+" orcamento="+$orc+" item_prod="+$item+" fe_prod="+$prod)

Out ""
Out "== volumetria =="
foreach($t in @('adm360_orcamentos','adm360_orcamento_itens','adm360_saldos','adm360_movimentos','adm360_reservas','adm360_lotes','adm360_produtos','adm360_operacoes')){
  $n=(Q ("select count(*) from plantaopro."+$t))
  Out ("  linhas "+$t+"="+$n)
}

Out ""
Out "== volumetria do tenant amostrado =="
foreach($t in @('adm360_orcamentos','adm360_saldos','adm360_movimentos','adm360_reservas')){
  $n=(Q ("select count(*)::bigint from plantaopro.$t where tenant_id='"+$tenant+"'"))
  Out ("  linhas "+$t+"="+$n)
}
foreach($l in (& $psql -h 127.0.0.1 -U postgres -d $db -X -q -t -A -c "select tablename||' :: '||indexname||E'     '||indexdef from pg_indexes where schemaname='plantaopro' and tablename like 'adm360_%' order by tablename, indexname;")){ Out ("  "+$l) }

Out ""
Out "== definicao da view adm360_saldos =="
foreach($l in (& $psql -h 127.0.0.1 -U postgres -d $db -X -q -t -A -c "select definition from pg_views where schemaname='plantaopro' and viewname='adm360_saldos';")){ Out ("  "+$l) }

function Explain([string]$rotulo,[string]$sql,[int]$reps=1){
  Out ""
  Out "== EXPLAIN "+$rotulo+" =="
  $exp="explain (analyze,buffers) "+$sql+";"
  for($rep=1;$rep -le $reps;$rep++){
    if($reps -gt 1){ Out ("-- rep "+$rep+" --") }
    foreach($l in (& $psql -h 127.0.0.1 -U postgres -d $db -X -q -t -A -c $exp)){ Out ("  "+$l) }
  }
}

$baseOrc=@"
FROM plantaopro.adm360_orcamentos o
LEFT JOIN plantaopro.adm360_parceiros h ON h.id = o.hospital_id AND h.tenant_id = o.tenant_id
LEFT JOIN plantaopro.hospitais hosp ON hosp.id = o.hospital_id AND hosp.tenant_id = o.tenant_id
LEFT JOIN plantaopro.adm360_parceiros f ON f.id = o.responsavel_financeiro_id AND f.tenant_id = o.tenant_id
LEFT JOIN plantaopro.hospitais hospf ON hospf.id = o.responsavel_financeiro_id AND hospf.tenant_id = o.tenant_id
LEFT JOIN plantaopro.adm360_parceiros m ON m.id = o.medico_id AND m.tenant_id = o.tenant_id
LEFT JOIN plantaopro.medicos med ON med.id = o.medico_id AND med.tenant_id = o.tenant_id
WHERE o.tenant_id = '$tenant'
"@
# (busca/situacao/datas passadas como NULL pelo Dapper => sem filtros no caso mais quente)

Explain "ORC_LINHAS (listar paginado p1, sem filtros)" ("select o.id, o.numero, o.revisao, o.total_geral, o.situacao, o.created_at "+$baseOrc+" order by o.created_at desc limit 50 offset 0")
Explain "ORC_COUNT (count paginado)" ("select count(*)::bigint "+$baseOrc)

$baseSaldo=@"
FROM plantaopro.adm360_saldos
WHERE tenant_id = '$tenant'
"@
Explain "SALDO_LINHAS (posicao paginada p1, sem filtros)" ("select produto_id, produto, lote_id, lote, validade, local_id, local, condicao, fisico, reservado, disponivel "+$baseSaldo+" order by produto, validade nulls last limit 50 offset 0")
Explain "SALDO_COUNT" ("select count(*)::bigint "+$baseSaldo)
Explain "SALDO_SOMAS (agregados dos cards)" ("select coalesce(sum(fisico),0), coalesce(sum(reservado),0), coalesce(sum(disponivel),0) "+$baseSaldo)
Explain "RESERVAS_ORIGEM (N+1 por item do orcamento)" ("select coalesce(sum(quantidade),0) from plantaopro.adm360_reservas where tenant_id = '$tenant' and origem_tipo = 'ORCAMENTO_CIRURGICO' and origem_id = '$orc' and produto_id = '$item' and situacao = 'ATIVA'") 3
Explain "SALDOS_FEFO (sugestao de lotes por produto)" ("select lote_id, validade, local_id, fisico, reservado, disponivel from plantaopro.adm360_saldos where tenant_id = '$tenant' and produto_id = '$prod' and condicao = 'LIBERADO' and fisico > 0 order by validade asc nulls last")

Out ""
Out "== fim recon =="
