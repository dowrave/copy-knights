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

        // 하이라이트되어야 할 요소(튜토리얼 캔버스로 옮길 요소)
        // public string highlightUIName = string.Empty; 
        public List<string> highlightUINames = new List<string>();

        [Tooltip("애니메이션을 기다리는 시간")]
        public float waitTime = 0f; 

        [Header("사용자의 입력을 기다려야 하는 경우에 사용")]
        public bool requireUserAction;
        public string actionRequiredUIName = string.Empty;
    }
}
