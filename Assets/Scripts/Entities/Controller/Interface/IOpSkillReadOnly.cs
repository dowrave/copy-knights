public interface IOpSkillReadOnly 
{
    public float CurrentSP { get; }
    public float MaxSP { get; }
    public bool IsSkillOn { get; }

    public event System.Action<float, float> OnSPChanged;
    public event System.Action OnSkillStateChanged;
}