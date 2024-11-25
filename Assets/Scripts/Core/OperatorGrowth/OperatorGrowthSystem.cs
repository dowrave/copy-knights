// ���� �ý����� �⺻ ��Ģ ����
public static class OperatorGrowthSystem
{
    public enum ElitePhase
    {
        Elite0 = 0,  // �ʱ� ����
        Elite1 = 1   // 1�� ����ȭ
    }

    public static readonly int Elite0MaxLevel = 50;
    public static readonly int Elite1MaxLevel = 60;

    public static int GetMaxLevel(ElitePhase phase)
    {
        // ���� swtich (phase) {case Elite0: return Elite0MaxLevel ...} ���� ����������
        // �̰Ŵ� C#�� ���� ��Ī�� ����� switch ǥ������
        return phase switch
        {
            ElitePhase.Elite0 => Elite0MaxLevel,
            ElitePhase.Elite1 => Elite1MaxLevel,
            _ => 1
        };
    }
}
