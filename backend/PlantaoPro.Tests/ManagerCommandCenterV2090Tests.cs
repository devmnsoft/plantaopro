using PlantaoPro.Domain.Escalas;

namespace PlantaoPro.Tests;
public sealed class ManagerCommandCenterV2090Tests
{
 [Fact] public void RiskIsCriticalForUncoveredShiftStartingSoon()=>Assert.Equal(65,ShiftRiskCalculator.Calculate(DateTimeOffset.UtcNow.AddHours(2),1,false,false,0,DateTimeOffset.UtcNow));
 [Fact] public void RiskIsCappedAtOneHundred()=>Assert.Equal(100,ShiftRiskCalculator.Calculate(DateTimeOffset.UtcNow,2,true,true,9,DateTimeOffset.UtcNow));
 [Fact] public void RankingBlocksScheduleConflict()
 { var candidate=new ScheduleCandidate(Guid.NewGuid(),"Dra. Ana",true,true,true,.95m,12,true,900,false,true,true);var result=SmartScheduleScoring.Score(candidate);Assert.False(result.Eligible);Assert.Contains("Conflito de horário",result.Alerts); }
 [Fact] public void RankingIsDeterministicAndExplainable()
 { var id=Guid.NewGuid();var candidate=new ScheduleCandidate(id,"Dr. Bruno",true,true,false,.8m,24,true,null,false,true,true);var first=SmartScheduleScoring.Score(candidate);var second=SmartScheduleScoring.Score(candidate);Assert.Equal(first.Score,second.Score);Assert.Equal(first.Reasons,second.Reasons);Assert.True(first.Eligible);Assert.InRange(first.Score,80,100);Assert.NotEmpty(first.Reasons); }
 [Fact] public void InactiveOrUnauthorizedProfessionalIsNotEligible()
 { var candidate=new ScheduleCandidate(Guid.NewGuid(),"Dra. Lia",true,true,false,1m,0,false,null,false,false,false);Assert.False(SmartScheduleScoring.Score(candidate).Eligible); }
}
