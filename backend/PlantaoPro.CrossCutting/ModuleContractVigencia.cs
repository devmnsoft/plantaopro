namespace PlantaoPro.CrossCutting.Security;

/// <summary>
/// B6 (rodada 4): predicado canônico de vigência efetiva de um contrato de
/// módulo (tabela plantaopro.tenant_modulos, alias tm).
///
/// Semântica decidida na governança B6 — "contrato ausente/expirado nunca é
/// ilimitado" e "AGENDADO vence pelo relógio, sem job":
///   - sem contrato para o tenant  -> o módulo não vale (exists() vazio);
///   - desativado_em preenchido    -> não vale, mesmo que status tenha sido
///     manualmente devolvido a ATIVO (a marca de desativação tem precedência);
///   - status ATIVO + habilitado e início ainda futuro (ativado_em &gt; now)
///                                    -> ainda não vale;
///   - status AGENDADO com início vencido (ativado_em/reg_date &lt;= now)
///                                    -> vale desde já (ativação preguiçosa);
///   - SUSPENSO/BLOQUEADO/outras    -> não vale.
///
/// A avaliação é sempre lazy, no read, nos dois pontos de aplicação
/// (claims de login em Data.LoadModulesAsync e política efetiva em
/// EffectivePermissionService.TestarAsync). Não há job de expiração: se o
/// estado físico do contrato mudou, os claims antigos só sobrevivem até a
/// revogação de sessão (S5) ou o próximo login.
/// </summary>
public static class ModuleContractVigencia
{
    public const string EffectivePredicate =
        @"tm.reg_status='A' and tm.desativado_em is null
and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
  or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))";
}
