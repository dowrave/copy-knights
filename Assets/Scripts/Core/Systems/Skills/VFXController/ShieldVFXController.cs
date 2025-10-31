using System.Collections;
using UnityEngine;

// 쉴드는 쉴드가 깨지는 이벤트가 발생하면 사라져야 하므로 그 부분만 추가
public class ShieldVFXController : SelfReturnVFXController
{
    private UnitEntity owner;

    new public void Initialize(float duration, UnitEntity owner)
    {
        Logger.LogWarning("[ShieldVFXController] Initialize 실행됨");
        this.owner = owner;

        // 일단 쉴드 시스템이 Operator에만 구현되어 있기 때문에 이렇게 만듦
        if (owner is Operator op)
        {
            op.shieldSystem.OnShieldChanged += HandleShieldChanged;
        }

        base.Initialize(duration, owner);
    }

    protected override void ReturnToPool()
    {
        // 일단 쉴드 시스템이 Operator에만 구현되어 있기 때문에 이렇게 만듦
        if (owner is Operator op)
        {
            op.shieldSystem.OnShieldChanged -= HandleShieldChanged;
        }

        base.ReturnToPool();
    }

    protected void HandleShieldChanged(float currentShield, bool isShieldDepleted)
    {
        if (isShieldDepleted)
        {
            ReturnToPool();
        }
    }


}