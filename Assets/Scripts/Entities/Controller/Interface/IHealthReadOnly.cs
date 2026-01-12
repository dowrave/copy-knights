using System;

// UnitEntity 외부에서 접근할 수 있는 프로퍼티 / 메서드 정의
public interface IHealthReadOnly
{
    float CurrentHealth { get; }

    // StatController에 있는 값이지만 내부의 구조를 모른 채 값을 사용할 수 있는 게 좋음
    // 여기서 정의하지 않는다면 사용자는 unit.Health 와 unit.Stats 두 곳을 모두 참조해야 한다.
    // 이는 결합도를 높이기 때문에 여기서 구현하는 건 좋은 방법이라는 듯.
    float MaxHealth { get; } 
    
    float CurrentShield { get; }

    // 이벤트도 인터페이스에 들어가야 함
    event Action<float, float ,float> OnHealthChanged;
    event Action OnDeath;
}