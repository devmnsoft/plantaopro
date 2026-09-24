using PlantaoPro.Domain.Administrativo360;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class Administrativo360SuprimentosDomainTests
{
    [Fact] public void Disponivel_SubtraiReservasDoLiberado() => Assert.Equal(5m, Estoque.Disponivel(8m,3m,false,false));
    [Theory]
    [InlineData(true,false)] [InlineData(false,true)]
    public void Disponivel_BloqueadoOuVencido_EhZero(bool vencido,bool bloqueado) => Assert.Equal(0m,Estoque.Disponivel(8m,0m,vencido,bloqueado));
    [Fact] public void Disponivel_ImpedeReservaAcimaDoFisico() => Assert.Throws<InvalidOperationException>(()=>Estoque.Disponivel(1m,2m,false,false));
    [Fact] public void Inspecao_ParcialAceitaOitoEDois() => Inspecao.ValidarDecisao(10m,8m,2m,"Embalagem avariada");
    [Fact] public void Inspecao_ImpedeDecisaoAcimaDoPendente() => Assert.Throws<InvalidOperationException>(()=>Inspecao.ValidarDecisao(10m,9m,2m,"Avaria"));
    [Fact] public void Inspecao_ExigeJustificativaParaReprovacao() => Assert.Throws<InvalidOperationException>(()=>Inspecao.ValidarDecisao(10m,8m,2m,""));
}
