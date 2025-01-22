using System.Collections.Generic;
using UnityEngine;

// ���� ȿ�� ��ų ���� ����
public abstract class FieldEffectController : MonoBehaviour
{
    protected Operator caster; // ������
    protected Vector2Int centerPosition; // �߽� ��ġ
    protected HashSet<Vector2Int> affectedTiles; // ���� ������ �޴� Ÿ�ϵ�
    protected float fieldDuration; // ���� �ð�
    protected float amountPerTick; // �ʵ� ���� Ÿ�ٵ鿡 ����Ǵ� ��ġ (����, �� ��)
    protected float interval; // ƽ�� ����Ǵ� ����(��)

    protected float elapsedTime = 0f; // ��� �ð�

    public virtual void Initialize(
        Operator caster, 
        Vector2Int centerPosition, 
        HashSet<Vector2Int> affectedTiles, 
        float fieldDuration,
        float amountPerTick,
        float interval
        )
    {
        this.caster = caster;
        this.centerPosition = centerPosition;
        this.affectedTiles = affectedTiles;
        this.fieldDuration = fieldDuration;
        this.amountPerTick = amountPerTick;
        this.interval = interval;
    }

    protected virtual void Update()
    {
        if (elapsedTime >= fieldDuration)
        {
            StopAndDestroyEffects();
        }

        elapsedTime += Time.deltaTime;
    }

    //protected abstract void UpdateTargets();

    //protected abstract void ApplyEffectToTarget();

    protected virtual void StopAndDestroyEffects()
    {
        Destroy(gameObject);
    }

    public virtual void ForceRemove()
    {
        StopAndDestroyEffects();
    }
}
