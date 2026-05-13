using Dapper;
using Npgsql;

namespace PlantaoPro.Api.Data;

public static class DevelopmentSeed
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentSeed");

        if (!env.IsDevelopment())
            return;

        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();

        await cn.ExecuteAsync("create schema if not exists plantaopro;");

        var perfilId = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.perfis where upper(nome)='ADMINISTRADOR' limit 1");
        if (!perfilId.HasValue)
        {
            perfilId = Guid.NewGuid();
            await cn.ExecuteAsync(@"insert into plantaopro.perfis(id,nome,descricao,reg_status,reg_date)
                                    values(@id,'ADMINISTRADOR','Perfil administrador','A',now())", new { id = perfilId });
        }

        var admin = await cn.QueryFirstOrDefaultAsync<(Guid Id, string? SenhaHash)>("select id,senha_hash from plantaopro.usuarios where lower(email)=lower(@Email) limit 1", new { Email = "admin@plantaopro.com" });

        Guid usuarioId;
        if (admin.Id == Guid.Empty)
        {
            usuarioId = Guid.NewGuid();
            var hash = BCrypt.Net.BCrypt.HashPassword("123456");
            await cn.ExecuteAsync(@"insert into plantaopro.usuarios(id,nome,email,senha_hash,reg_status,reg_date)
                                    values(@id,'Administrador','admin@plantaopro.com',@hash,'A',now())", new { id = usuarioId, hash });
        }
        else
        {
            usuarioId = admin.Id;
            if (string.IsNullOrWhiteSpace(admin.SenhaHash))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword("123456");
                await cn.ExecuteAsync("update plantaopro.usuarios set senha_hash=@hash,reg_update=now() where id=@id", new { id = usuarioId, hash });
            }
        }

        await cn.ExecuteAsync(@"insert into plantaopro.usuarios_perfis(id,usuario_id,perfil_id,reg_status,reg_date)
                                values(gen_random_uuid(),@usuarioId,@perfilId,'A',now())
                                on conflict do nothing", new { usuarioId, perfilId });

        logger.LogInformation("Seed de desenvolvimento executado para admin e perfil ADMINISTRADOR.");
    }
}
