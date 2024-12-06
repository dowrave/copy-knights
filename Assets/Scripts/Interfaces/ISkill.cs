// 스킬 사용이 가능한 엔티티를 위한 인터페이스
public interface ISkill
{
    void UseSkill();
    bool CanUseSkill();
}

// SP 구현은 오퍼레이터만 해당되므로 여기선 구현하지 않음
// 적의 경우는 쿨타임만 돌면 되므로 마찬가지로 여기서 구현하지 않음