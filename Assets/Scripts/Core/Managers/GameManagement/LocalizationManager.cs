using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager: MonoBehaviour
{
    // 싱글톤은 GameManagement에서 관리되므로 생략됨

    [SerializeField] private LocalizationTable localizationTable;

    private Dictionary<string, string> _currentLanguageTexts;

    private void Start()
    {
        // 언어 변경 이벤트 구독 
        GameManagement.Instance.OnLanguageChanged += LoadLanguage;
        LoadLanguage();
    }

    // 현재 언어에 해당하는 텍스트를 가져옴
    private void LoadLanguage()
    {
        _currentLanguageTexts = new Dictionary<string, string>();
        int languageIndex = (int)GameManagement.Instance.CurrentLanguage;

        foreach (var item in localizationTable.items)
        {
            // 현재 언어 인덱스에 맞는 번역문을 가져옴
            if (item.translations.Length > languageIndex)
            {
                _currentLanguageTexts.Add(item.key, item.translations[languageIndex]);
            }
            else
            {
                Logger.LogWarning($"{item.key}에 관한 번역이 없습니다 - 언어 : {GameManagement.Instance.CurrentLanguage}");
            }
        }
    }

    // 외부에서 텍스트를 가져갈 때 사용하는 함수
    public string GetText(string key)
    {
        if (_currentLanguageTexts.TryGetValue(key, out string value))
        {
            return value;
        }

        Logger.LogError($"Localization {key}값을 찾지 못함");
        return key;
    }
}