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

    // R6-BlocoA item 2: o mesmo predicado vive materializado na função canônica
    // plantaopro.modulos_efetivos(uuid) (migration v2339), que além do contrato
    // direto expande as capacidades-filhas herdadas do pacote (pacote_codigo).
    // Todo o runtime de acesso (claims de login, política por request, gates de
    // módulo, autosserviço, onboarding) consulta a função; EffectivePredicate
    // permanece para os pontos administrativos que avaliam a linha tm isolada e
    // para o teste de sincronia que garante que os dois textos não divergem.

    /// <summary>Lista dos módulos efetivamente contratados do tenant (@tenantId),
    /// incluindo capacidades incluídas no pacote contratado. Fonte única R6.</summary>
    public const string ModulosEfetivosSql =
        "select codigo from plantaopro.modulos_efetivos(@tenantId) order by 1";

    /// <summary>Contratação efetiva pontual (@tenantId,@moduleCode), com herança
    /// de pacote. Fonte única R6 para política por request e gates [Saude360Module].</summary>
    public const string ModuloEfetivoSql =
        "select plantaopro.modulo_efetivo(@tenantId,@moduleCode)";
}
