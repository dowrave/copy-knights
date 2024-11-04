

[System.Serializable]
public class OwnedOperator
{
    // ����ȭ�ؼ� �����ϴ� �κ� - PlayerPrefs�� �����
    public string operatorId; // OperatorData�� entityName�� ��Ī

    [System.NonSerialized]
    private OperatorData _baseData; // ����ȭ���� ����. ���� ���� �� null�� �ʱ�ȭ
    public OperatorData BaseData // �� ������Ƽ�� ���� OperatorData�� �����ؼ� ������
    {
        get
        {
            // ���� ���� ��
            if (_baseData == null) 
            {   
                // Lazy Loading���� �̷� ������ �����ӿ��� �ʵ带 �Ҵ��ϴ� �� �� Ȯ���� �����
                //_baseData = PlayerDataManager.Instance.GetOwnedOperators(operatorId);  // PlayerDataManager���� operatorID�� �ش��ϴ� OperatorData�� ������
            }

            return _baseData;
        }
    }
}