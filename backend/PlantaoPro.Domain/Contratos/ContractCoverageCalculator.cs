namespace PlantaoPro.Domain.Contratos;

public sealed record ContractCoverage(int Contracted,int Requested,int Approved,int Completed,int Uncovered,bool SlaMet);
public static class ContractCoverageCalculator
{
    public static ContractCoverage Calculate(int contracted,int requested,int approved,int completed,TimeSpan responseTime,TimeSpan sla)
    {
        if(contracted<0||requested<0||approved<0||completed<0) throw new ArgumentOutOfRangeException(nameof(contracted));
        return new(contracted,requested,approved,completed,Math.Max(0,Math.Max(contracted,approved)-completed),responseTime<=sla);
    }
}
