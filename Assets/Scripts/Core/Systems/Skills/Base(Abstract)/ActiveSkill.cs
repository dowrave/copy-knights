
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Skills.Base
{
    public abstract class ActiveSkill : OperatorSkill
    {
        [Header("Skill Duration")]
        public float duration = 0f;

        [Header("Skill Duration Effects")]
        [SerializeField] protected GameObject durationVFXPrefab = default!;

        [Header("Optional) Skill Range")]
        [SerializeField] protected bool activeFromOperatorPosition = true; // UI에 사거리 표시할 때 중심이 되는 부분의 색을 변경하기 위한 기능적인 필드
        [SerializeField] protected List<Vector2Int> skillRangeOffset = new List<Vector2Int>(); // 공격 범위

        [Header("For UI")]
        [Tooltip("UI용 수평방향 오프셋. +값은 -x방향으로 이동함.")]
        [SerializeField] protected float rectOffset; // UI용 오프셋

        protected string _durationVFXTag;
        public string DurationVFXTag
        {
            get
            {
                // durationVFXPrefab이 있을 때에만 생성됨
                if (durationVFXPrefab != null && string.IsNullOrEmpty(_durationVFXTag))
                {
                    _durationVFXTag = $"{skillName}_DurationVFX";
                }
                return _durationVFXTag;
            }
        }

        public float Duration => duration;
        public IReadOnlyList<Vector2Int> SkillRangeOffset => skillRangeOffset;
        public bool ActiveFromOperatorPosition => activeFromOperatorPosition;
        public GameObject DurationVFXPrefab => durationVFXPrefab; 
        public float RectOffset => rectOffset;

        public override void PreloadObjectPools(OperatorData opData)
        {
            base.PreloadObjectPools(opData);

            if (durationVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(DurationVFXTag, durationVFXPrefab, 1);
            }
        }
    }
}


