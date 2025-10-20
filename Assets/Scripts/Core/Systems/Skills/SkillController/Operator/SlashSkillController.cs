using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Skills.Base;

public class SlashSkillController : MonoBehaviour
{
    private Operator attacker = default!; // ���� ��쿡 ���� ���� ó���� ��� �ϴ� �̷��� ����
    private float effectDuration;
    private float firstDamageMultiplier;
    private float secondDamageMultiplier;
    private GameObject hitEffectPrefab = default!;
    private string hitEffectTag = string.Empty;
    private string skillPoolTag = string.Empty; // Ǯ�� �ǵ��� �� ���
    private OperatorSkill OperatorSkill;

    private float firstDelay = 0.3f;
    private float secondDelay = 0.1f;

    // ��ƼŬ �ý��� ����
    [SerializeField] private ParticleSystem mainEffect = default!;

    // ����� ����� �� ����
    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

    // ���� ���� Ÿ�� ��ǥ
    private HashSet<Vector2Int> skillRange = new HashSet<Vector2Int>();

    public void Initialize(Operator op, float duration, HashSet<Vector2Int> attackableGridPositions, float firstDmg, float secondDmg, GameObject hitEffectPrefab, string hitEffectTag, string skillPoolTag, OperatorSkill OperatorSkill)
    {
        attacker = op;
        effectDuration = duration;
        this.skillRange = attackableGridPositions;
        firstDamageMultiplier = firstDmg;
        secondDamageMultiplier = secondDmg;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;
        this.skillPoolTag = skillPoolTag;
        this.OperatorSkill = OperatorSkill;

        mainEffect.Play(true);

        StartCoroutine(SkillSequenceCoroutine());
    }

    private IEnumerator SkillSequenceCoroutine()
    {
        // 1��° Ÿ��
        ApplyDamageInRange(firstDamageMultiplier);
        yield return new WaitForSeconds(firstDelay);

        // 2��° Ÿ�� : 3���� ���� ��. ������ 0.15��.
        ApplyDamageInRange(secondDamageMultiplier);
        yield return new WaitForSeconds(secondDelay);
        ApplyDamageInRange(secondDamageMultiplier);
        yield return new WaitForSeconds(secondDelay);
        ApplyDamageInRange(secondDamageMultiplier);

        // ��ų ���� �Ŀ��� ���� �ٷ� �����ϵ��� ���� �Ұ� ���� ����
        attacker.RemoveBuffFromSourceSkill(OperatorSkill);

        // --- ��ų ���� �� ���� ---
        // ���� vfxDuration ��ŭ ��ٷȴٰ� ��Ȱ��ȭ (������Ʈ Ǯ��)
        // ������ 0.3 + 0.2 = 0.5�ʸ� ��������Ƿ� ���� �ð��� ��ٸ�
        float remainingTime = effectDuration - 0.6f;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        mainEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ObjectPoolManager.Instance.ReturnToPool(skillPoolTag, gameObject);
    }


    // ������ ���� ���� ��� ������ �������� �����ϴ� ���� �Լ�
    private void ApplyDamageInRange(float damageMultiplier)
    {
        // 1. ���� ���� ��� ���� ã���ϴ� (�ߺ� ���Ÿ� ���� HashSet ���).
        HashSet<Enemy> enemiesInRange = new HashSet<Enemy>();
        foreach (Vector2Int gridPos in skillRange)
        {
            Tile tile = MapManager.Instance.GetTile(gridPos.x, gridPos.y);
            if (tile != null)
            {
                // Ÿ�� ���� ��� ���� ��ȸ�ϸ� enemiesInRange�� �߰�
                foreach (Enemy enemy in tile.EnemiesOnTile)
                {
                    enemiesInRange.Add(enemy);
                }
            }
        }

        // 2. ã�� ���鿡�� �������� �����մϴ�. - ���߿� ���� ������ ���� ����
        foreach (Enemy enemy in enemiesInRange)
        {            
            if (enemy != null)
            {
                // ������ ó�� ����...
                AttackSource attackSource = new AttackSource(
                    attacker: attacker,
                    position: attacker.transform.position,
                    damage: attacker.AttackPower * damageMultiplier,
                    type: attacker.AttackType,
                    isProjectile: false,
                    hitEffectTag: hitEffectTag,
                    showDamagePopup: false
                );

                enemy.TakeDamage(attackSource);

                // enemy.TakeDamage(...);

                // �ǰ� ����Ʈ ���� ��...

                // ó���� ������ ���
                // alreadyHitEnemies.Add(enemy);
            }
        }
    }

    private void OnDestroy()
    {
        damagedEnemies.Clear();
    }
}
