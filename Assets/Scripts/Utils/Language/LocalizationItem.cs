[System.Serializable]
public class LocalizationItem
{
    public string key; // 텍스트 식별 키 (엔티티 이름 등등)
    public string[] translations; // 언어 순서에 따른 텍스트 배열 - GameManagement.Langauge의 순서를 따라야 함!
}