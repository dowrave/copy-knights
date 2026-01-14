using UnityEngine;
using System.Collections;
using Skills.Base;
using System;

public class OpSkillController: IOpSkillReadOnly
{
    private Operator _owner;

    private float _currentSP;
    public float CurrentSP 
    {
        get { return _currentSP; }
        set 
        { 
            _currentSP = Mathf.Clamp(value, 0f, MaxSP);
            OnSPChanged?.Invoke(CurrentSP, MaxSP);
        }
    }
    public float MaxSP { get; private set; }

    private string _durationVFXTag;
    private GameObject _currentDurationVFX;

    // 스킬 관련
    public OperatorSkill CurrentSkill { get; private set; } = default!;
    private bool _isSkillOn;
    public bool IsSkillOn
    {
        get => _isSkillOn;
        private set
        {
            if (_isSkillOn != value)
            {
                _isSkillOn = value;
                OnSkillStateChanged?.Invoke();
            }
        }
    }
    private Coroutine _activeSkillCoroutine; // 지속시간이 있는 스킬에 해당하는 코루틴

    public event Action<float, float> OnSPChanged = delegate { };
    public event Action OnSkillStateChanged = delegate { };

    // 스킬이 켜고 꺼질 때 오퍼레이터의 공격 속도/동작 시간 초기화를 위한 이벤트
    public event Action<Operator> OpActionTimeReset = delegate { };

    public OpSkillController(Operator op)
    {
        _owner = op;
    }

    public void Initialize(OperatorSkill skill)
    {
        // 스킬 설정
        CurrentSkill = skill;
        CurrentSP = _owner.OperatorData.InitialSP;
        MaxSP = CurrentSkill?.SPCost ?? 0f;
    }

    #region skill Flow

    public void ActivateSkill()
    {
        if (!CanUseSkill()) return;

        if (CurrentSkill is ActiveSkill activeSkill)
        {
            ActivateActiveSkill(activeSkill);
        }
        else
        {
            ActivateGenericSkill();
        }
    }

    /// <summary>
    /// 스킬의 내용은 OnSkillActivated, OnUpdate, OnSkillEnd에 있음
    /// </summary>
    private void ActivateActiveSkill(ActiveSkill skill)
    {
        PrepareSkillActivation();

        if (skill.Duration > 0f)
        {
            StartDurationSkill(skill);
        }
        else
        {
            ExecuteInstantSkill(skill);
        }
    }

    private void ExecuteInstantSkill(ActiveSkill skill)
    {
        skill.OnSkillActivated(_owner);
        skill.OnUpdate(_owner);
        skill.OnSkillEnd(_owner);
    }

    private void PrepareSkillActivation()
    {
        _isSkillOn = true;

        // 스킬 준비 후 공격 모션 초기화
       OpActionTimeReset?.Invoke(_owner);
    }

    private void StartDurationSkill(ActiveSkill skill)
    {
        // 스킬 켜졌을 때의 VFX 활성화
        PlayDurationVFX(skill);
        
        // 참고) Shield의 경우 추가 VFX가 있는데 이걸 어떻게 처리할지 생각 필요

        // 코루틴 시작
        if (_activeSkillCoroutine != null)
        {
            _owner.StopCoroutine(_activeSkillCoroutine);
        }

        _activeSkillCoroutine = _owner.StartCoroutine(Co_HandleDurationSkill(skill));
    }

    private IEnumerator Co_HandleDurationSkill(ActiveSkill skill)
    {
        skill.OnSkillActivated(_owner);

        float elapsed = 0f;
        float duration = skill.Duration; 

        while (elapsed < duration)
        {
            // 소유자 파괴 시 중단
            if (_owner == null)
            {
                CleanupSkill();
                yield break;
            }

            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // UI용 - SP 시각적 감소
            CurrentSP = MaxSP * (1f - progress);

            // 스킬 틱 호출
            skill.OnUpdate(_owner);

            yield return null;
        }

        // 정상 종료
        CompleteActiveSkill(skill);
    }

    private void CompleteActiveSkill(ActiveSkill skill)
    {
        skill.OnSkillEnd(_owner);
        CleanupSkill();
        OpActionTimeReset?.Invoke(_owner);
    }

    public void CleanupSkill()
    {
        CurrentSP = 0f;
        IsSkillOn = false;
        _activeSkillCoroutine = null;
        CleanupVFX();
    }

    // ActiveSkill이 아닌 경우의 실행
    private void ActivateGenericSkill()
    {
        PrepareSkillActivation();
        CurrentSkill.OnUpdate(_owner);

        CurrentSP = 0f;
        // IsSkillOn = false;
    }

    public void OnUpdate()
    {
        HandleSPRecovery();
        CurrentSkill.OnUpdate(_owner);

        if (_owner.HasRestriction(ActionRestriction.CannotAction)) return;

        // 자동 발동 스킬은 조건 체크 후 실행
        HandleSkillAutoActivate();
    }

    #endregion

    #region VFX Methods
    private void PlayDurationVFX(ActiveSkill skill)
    {
        if (skill.DurationVFXPrefab == null) return;
        if (skill.DurationVFXTag == null || skill.DurationVFXTag == string.Empty) return;

        _durationVFXTag = skill.DurationVFXTag;
        Vector3 pos = _owner.transform.position; 

        _currentDurationVFX = ObjectPoolManager.Instance.SpawnFromPool(_durationVFXTag, pos, Quaternion.identity);

        if (_currentDurationVFX != null)
        {
            var controller = _currentDurationVFX.GetComponent<SelfReturnVFXController>();
            controller?.Initialize(skill.Duration, _owner);
        }
    }

    private void PlayAdditionalVFX(ActiveSkill skill)
    {
        
    }
    
    private void CleanupVFX()
    {
        if (_currentDurationVFX != null)
        {
            var controller = _currentDurationVFX.GetComponent<SelfReturnVFXController>();
            if (controller != null)
            {
                controller.ForceReturn();
            }
            else
            {
                ObjectPoolManager.Instance.ReturnToPool(_durationVFXTag, _currentDurationVFX);
            }

            _currentDurationVFX = null;
        }
    }
    #endregion



    private void HandleSkillAutoActivate()
    {
        if (CurrentSkill != null && CurrentSkill.autoActivate && CanUseSkill())
        {
            ActivateSkill();
        }
    }

    private void HandleSPRecovery()
    {
        if (_owner.IsDeployed == false || CurrentSkill == null) { return; }

        // 자동회복일 때만 처리
        if (CurrentSkill.autoRecover)
        {
            // float oldSP = CurrentSP;

            // 최대 SP 초과 방지 (이벤트는 자체 발생)
            CurrentSP = Mathf.Min(CurrentSP + _owner.SPRecoveryRate * Time.deltaTime, MaxSP);
        }
        
        // 수동회복 스킬은 공격 시에 회복되므로 여기서 처리하지 않음
    }

    public bool CanUseSkill()
    {
        return _owner.IsDeployed && 
            CurrentSP >= MaxSP && 
            !IsSkillOn && 
            CurrentSkill != null;
    }

    public void SetCurrentSP(float newValue) 
    {
        CurrentSP = newValue;
    }

    // 지속 시간이 있는 스킬을 켜거나 끌 때 호출됨
    public void SetState(bool skillOnState)
    {
        IsSkillOn = skillOnState;
    }

    public void OnDisabled()
    {
        CleanupSkill();
    }
}