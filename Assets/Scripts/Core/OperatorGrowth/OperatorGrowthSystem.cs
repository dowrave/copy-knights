// 성장 시스템의 기본 규칙 정의
public static class OperatorGrowthSystem
{
    public enum ElitePhase
    {
        Elite0 = 0,  // 초기 상태
        Elite1 = 1   // 1차 정예화
    }

    public static readonly int Elite0MaxLevel = 50;
    public static readonly int Elite1MaxLevel = 60;

    public static int GetMaxLevel(ElitePhase phase)
    {
        // 원래 swtich (phase) {case Elite0: return Elite0MaxLevel ...} 같은 서술이지만
        // 이거는 C#의 패턴 매칭을 사용한 switch 표현식임
        return phase switch
        {
            ElitePhase.Elite0 => Elite0MaxLevel,
            ElitePhase.Elite1 => Elite1MaxLevel,
            _ => 1
        };
    }
}
