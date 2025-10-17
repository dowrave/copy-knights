using UnityEngine;
using System.Collections;
using Skills.Base;

// 보스 스킬에서 떨어지는 태양 VFX의 움직임을 제어하는 컴포넌트.
public class FallingSunVFXController : MonoBehaviour
{
    private EnemyBoss _caster;
    private BossExplosionSkill skillData;

    [Header("Sun Particle Settings")]
    [SerializeField] private ParticleSystem mainParticle;
    [SerializeField] private GameObject sunObject;
    [SerializeField] private float sunSpeed = 1f; // 일단 이렇게 구현. ScriptableObject를 받아서 구현해야 할 수도 있음.
    [SerializeField] private float startYPos = 3f;

    private float duration = 3f; // 파티클이 내려오는 시간.

    // 테스트용. 실제로 쓸 때는 끄자.
    // private void Start()
    // {
    //     Initialize(duration);
    // }

    // 파티클 시스템 실행 및 효과 재생
    public void Initialize(EnemyBoss caster, BossExplosionSkill skillData, float duration)
    {
        StopAllCoroutines();

        _caster = caster;
        this.skillData = skillData;
        this.duration = duration;

        // 위치 초기화
        sunObject.transform.localPosition = new Vector3(0f, startYPos, 0f);

        // 파티클 시스템 재생
        mainParticle.Play(true);

        // Sun 파티클 위치 변화 코루틴 시작
        StartCoroutine(FallSunParticle());
    }

    public IEnumerator FallSunParticle()
    {
        float elapsedTime = 0f;

        while (elapsedTime <= duration)
        {
            sunObject.transform.localPosition = new Vector3(
                sunObject.transform.localPosition.x,
                sunObject.transform.localPosition.y - sunSpeed * Time.deltaTime,
                sunObject.transform.localPosition.z
            );

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        ObjectPoolManager.Instance.ReturnToPool(skillData.GetFallingSunVFXTag(_caster.BossData), gameObject);
    }
}
