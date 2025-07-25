
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
namespace Skills.Base
{
    public abstract class ActiveSkill : BaseSkill
    {
        [Header("Skill Duration")]
        public float duration = 0f;

        [Header("Skill Duration Effects")]
        [SerializeField] protected GameObject skillVFXPrefab = default!;

        [Header("Optional) Skill Range")]
        [SerializeField] protected bool activeFromOperatorPosition = true; // UI에 사거리 표시할 때 중심이 되는 부분의 색을 변경하기 위한 기능적인 필드
        [SerializeField] protected List<Vector2Int> skillRangeOffset = new List<Vector2Int>();

        [Tooltip("UI용 수평방향 오프셋. +값은 -x방향으로 이동함.")]
        [SerializeField] protected float rectOffset; // UI용 오프셋

        public IReadOnlyList<Vector2Int> SkillRangeOffset => skillRangeOffset;
        public bool ActiveFromOperatorPosition => activeFromOperatorPosition;
        public float RectOffset => rectOffset;

        protected GameObject? VfxInstance;
        protected VisualEffect? VfxComponent;
        protected ParticleSystem? VfxPs;

        protected HashSet<Vector2Int> actualSkillRange = new HashSet<Vector2Int>();

        public override void Activate(Operator op)
        {
            CommonActivation(op);

            // 지속시간에 따른 구현
            // 이외의 상황은 메서드 오버라이드 ㄱㄱ (ammoBased는 이미 그렇게 구현됨)
            if (duration > 0)
            {
                op.StartSkillCoroutine(Co_HandleSkillDuration(op));
            }
            else
            {
                PlayInstantSkill(op);
            }
        }

        // 시전자 지정, VFX 설정, 공격 초기화 등등 공통된 로직을 구현한다.
        protected virtual void CommonActivation(Operator op)
        {
            caster = op;
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            // 기본 이펙트와 추가 이펙트 재생
            PlaySkillVFX(op);
            PlayAdditionalVFX(op);

            // 스킬 사용 직후에는 공격 속도 / 모션 초기화
            op.SetAttackDuration(0f);
            op.SetAttackCooldown(0f);

            op.SetSkillOnState(true);
        }

        // 추가 이펙트
        protected virtual void PlayAdditionalVFX(Operator op) { }

        // 지속 시간이 없는 (=즉발형) 스킬 실행
        protected void PlayInstantSkill(Operator op)
        {
            PlaySkillEffect(op);
            OnSkillEnd(op);
        }

        // 실질적인 스킬 효과 실행.
        // 무조건 OnSkillStart와 OnSkillEnd 사이에 들어감
        protected virtual void PlaySkillEffect(Operator op) { }

        protected virtual void OnSkillEnd(Operator op)
        {
            if (VfxInstance != null)
            {
                SafeDestroySkillVFX(VfxInstance);
            }

            op.CurrentSP = 0;
            op.SetSkillOnState(false);

            // 스킬이 꺼질 때도 공격 쿨다운 초기화 - 바로 때릴 수 있게끔
            op.SetAttackDuration(0f);
            op.SetAttackCooldown(0f);
            
            // 지속시간이 있는 스킬은 오퍼레이터의 코루틴 초기화
            if (duration > 0)
            {
                op.EndSkillCoroutine();
            }
        }

        // 기본 이펙트 생성 및 재생
        protected virtual void PlaySkillVFX(Operator op)
        {
            if (skillVFXPrefab == null) return;

            // 오퍼레이터 위치에 약간 띄워서 이펙트 생성
            Vector3 effectPosition = op.transform.position + Vector3.up * 0.05f;
            VfxInstance = Instantiate(
                skillVFXPrefab,
                effectPosition,
                Quaternion.identity,
                op.transform    // 오퍼레이터의 자식으로 생성
            );

            if (VfxInstance != null)
            {
                // VFX 또는 파티클 시스템 컴포넌트 검색 및 재생
                VfxComponent = VfxInstance.GetComponent<VisualEffect>();
                if (VfxComponent != null)
                {
                    VfxComponent.Play();
                }

                VfxPs = VfxInstance.GetComponent<ParticleSystem>();
                if (VfxPs != null)
                {
                    VfxPs.Play();
                }
            }
        }

        // 스킬 지속시간 처리
        public virtual IEnumerator Co_HandleSkillDuration(Operator op)
        {
            PlaySkillEffect(op);

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                if (op == null) yield break; // 오퍼레이터 파괴 시에 동작을 멈춤
                
                elapsedTime += Time.deltaTime;

                // 서서히 감소하는 SP 동작을 여기서 구현
                // UI 업데이트는 op.CurrentSP의 세터에 의해 자동으로 이어짐.
                op.CurrentSP = op.MaxSP * (1 - (elapsedTime / duration));

                // op.UpdateSkillDurationDisplay(1 - (elapsedTime / duration));

                yield return null;
            }

            OnSkillEnd(op);
            // op.EndSkillDurationDisplay();
        }

        protected virtual void SafeDestroySkillVFX(GameObject vfxObject)
        {
            if (vfxObject == null) return;

            // 파티클 시스템의 경우 남은 파티클이 모두 사라질 때까지 대기
            ParticleSystem ps = vfxObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(
                    vfxObject,
                    ps.main.duration + ps.main.startLifetime.constantMax
                );
                return;
            }

            // VFX의 경우 즉시 중지 후 제거
            VisualEffect vfx = vfxObject.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                vfx.Stop();
                Destroy(vfxObject);
                return;
            }

            // 그 외의 경우 즉시 제거
            Destroy(vfxObject);
        }

        protected void CalculateActualSkillRange(Vector2Int center)
        {
            foreach (Vector2Int offset in skillRangeOffset)
            {
                Vector2Int rotatedOffset = DirectionSystem.RotateGridOffset(offset, caster.FacingDirection);
                actualSkillRange.Add(center + rotatedOffset);
            }
        }
    }
}
