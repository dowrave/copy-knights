// 행동에 제약이 걸리는 조건들을 정리합니다.
[System.Flags] // enum이 비트 필드 = 여러 상태의 조합으로 사용될수 있음을 나타냄
public enum ActionRestriction
{
    None = 0,
    Stunned = 1 << 0, // 00000001 (1)

    // 아래는 참고로 쓰라는 예제들
    // Frozen = 1 << 1,   // 빙결 상태 (2)
    // Disarmed = 1 << 2, // 무장 해제 (공격만 불가) (4)
    // Rooted = 1 << 3,   // 속박 (이동만 불가) (8)
    // Casting = 1 << 4,  // 스킬 시전 중 (16)

    CannotAttack = Stunned,
    CannotMove = Stunned, 
    CannotUseSkill = Stunned, 
    CannotAction = Stunned, 
}