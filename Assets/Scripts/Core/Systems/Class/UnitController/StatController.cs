using UnityEngine;
using System.Collections.Generic;
using System;

// 능력치(즉 소모되지 않는 값이나 규칙)를 다루는 컨트롤러
// - currentHP는 "상태"라서 여기서 관리하지 않지만, MaxHP는 "능력치"라서 여기서 관리한다.
// - 상태와 능력치의 차이 : 상태는 소모되고 누적된다. 능력치는 소모되지 않는다.
public class StatController: IReadableStatController
{
    // 기본 값(데이터) <스탯 종류, 실제 값>
    private Dictionary<StatType, float> _baseStats = new Dictionary<StatType, float>();

    // 스탯별 수정자
    private Dictionary<StatType, float> _modifiers = new Dictionary<StatType, float>();

    // 덮어쓰기용(MaxBlockCount, BlockSize) 등
    private Dictionary<StatType, float> _overrides = new Dictionary<StatType, float>();
    

    // 스탯 변경 알리는 이벤트
    public event Action<StatType> OnStatChanged = delegate { };

    public StatController()
    {
        
    }

    // 초기화 - OperatorData
    public void Initialize(OperatorData data)
    {
        _baseStats.Clear();

        var stats = data.Stats;

        // 스탯들 초기화
        _baseStats[StatType.MaxHP] = stats.Health;
        _baseStats[StatType.Defense] = stats.Defense;
        _baseStats[StatType.MagicResistance] = stats.MagicResistance;
        _baseStats[StatType.DeploymentCost] = stats.DeploymentCost;
        _baseStats[StatType.RedeployTime] = stats.RedeployTime;
        _baseStats[StatType.AttackPower] = stats.AttackPower;
        _baseStats[StatType.AttackSpeed] = stats.AttackSpeed;
        _baseStats[StatType.MaxBlockCount] = stats.MaxBlockableEnemies;
        _baseStats[StatType.SPRecoveryRate] = stats.SPRecoveryRate;
    }

    // 초기화 - EnemyData
    public void Initialize(EnemyData data)
    {
        _baseStats.Clear();

        var stats = data.Stats;

        // 스탯들 초기화
        _baseStats[StatType.MaxHP] = stats.Health;
        _baseStats[StatType.Defense] = stats.Defense;
        _baseStats[StatType.MagicResistance] = stats.MagicResistance;
        _baseStats[StatType.MovementSpeed] = stats.MovementSpeed;
        _baseStats[StatType.AttackRange] = stats.AttackRange;
        _baseStats[StatType.AttackPower] = stats.AttackPower;
        _baseStats[StatType.AttackSpeed] = stats.AttackSpeed;
        _baseStats[StatType.BlockSize] = stats.BlockSize;
    }

    // 초기화 - DeployableUnitData
    public void Initialize(DeployableUnitData data)
    {
        _baseStats.Clear();

        var stats = data.Stats;

        // 스탯들 초기화
        _baseStats[StatType.MaxHP] = stats.Health;
        _baseStats[StatType.Defense] = stats.Defense;
        _baseStats[StatType.MagicResistance] = stats.MagicResistance;
        _baseStats[StatType.DeploymentCost] = stats.DeploymentCost;
        _baseStats[StatType.RedeployTime] = stats.RedeployTime;
    }


    // 스탯 게터  
    // 요청이 들어올 때마다 modifier에 곱해서 계산된다.
    public float GetStat(StatType type)
    {
        // 오버라이드에 있는 값이라면 최우선으로 나감(덮어쓰기 값)
        if (_overrides.TryGetValue(type, out float overrideValue))
        {
            return overrideValue;
        }

        float baseValue = _baseStats.TryGetValue(type, out float val) ? val : 0f;
        float modifierValue = _modifiers.TryGetValue(type, out float mod) ? mod : 0f;

        float calculatedValue = baseValue * (1 + modifierValue);

        return calculatedValue;
    }

    // 합연산으로 저장된다
    // 예시) value = 1.5라면 +50%을 의미함 - modifiers는 여기서 1을 뺀 0.5를 저장한다
    public void AddModifier(StatType type, float value)
    {
        float actualValue = value - 1.0f;

        // 키가 없다면 0으로 초기화
        if (!_modifiers.ContainsKey(type))
        {
            _modifiers[type] = 0f;
        }

        // 값 더하기
        _modifiers[type] += actualValue;

        // 변경 알림
        OnStatChanged?.Invoke(type);

    }

    public void RemoveModifier(StatType type, float value)
    {
        float actualValue = value - 1.0f;

        if (_modifiers.ContainsKey(type))
        {
            _modifiers[type] -= value;

            // 값이 0에 가까워지면 딕셔너리에서 제거해도 됨
            if (Mathf.Abs(_modifiers[type]) < 0.00001f) _modifiers.Remove(type);

            OnStatChanged?.Invoke(type);
        }
    }

    public void SetOverride(StatType type, float value)
    {
        _overrides[type] = value;
    }

    public void RemoveOverride(StatType type)
    {
        if (_overrides.ContainsKey(type))
        {
            _overrides.Remove(type);
        }
    }
}

