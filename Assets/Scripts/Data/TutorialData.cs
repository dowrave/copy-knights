using System;
using System.Collections.Generic;
using UnityEngine;

// 튜토리얼 각각에 대한 데이터. 사용자가 저장하는 데이터가 아님!
[CreateAssetMenu(fileName = "TutorialData", menuName = "Tutorial/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    public string tutorialName;
    public List<TutorialStep> steps = new List<TutorialStep>();

    // 1개의 Step은 "Dialogue 출력 -> 사용자의 동작 완료"를 기본 골자로 하겠음
    [Serializable]
    public class TutorialStep
    {
        public string stepName;
        public List<string> dialogues = new List<string>();
        public Vector2 dialogueBoxPosition = new Vector2(0, 0);
        public bool requireUserAction;

        [Tooltip("requireUserAction이 true일 때만 사용. 활성화 시 해당 버튼의 입력을 기다립니다.")]
        public string expectedButtonName;

    }
}
