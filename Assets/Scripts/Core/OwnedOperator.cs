

[System.Serializable]
public class OwnedOperator
{
    // 직렬화해서 저장하는 부분 - PlayerPrefs에 저장됨
    public string operatorId; // OperatorData의 entityName과 매칭

    [System.NonSerialized]
    private OperatorData _baseData; // 직렬화하지 않음. 게임 시작 시 null로 초기화
    public OperatorData BaseData // 이 프로퍼티를 통해 OperatorData에 접근해서 가져옴
    {
        get
        {
            // 최초 접근 시
            if (_baseData == null) 
            {   
                // Lazy Loading에서 이런 식으로 게터임에도 필드를 할당하는 건 잘 확립된 방식임
                //_baseData = PlayerDataManager.Instance.GetOwnedOperators(operatorId);  // PlayerDataManager에서 operatorID에 해당하는 OperatorData를 가져옴
            }

            return _baseData;
        }
    }
}