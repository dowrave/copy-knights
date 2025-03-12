using System;
using System.Collections.Generic;
using UnityEngine;

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
        public string expectedButtonName;

        //public TutorialAction actionAfterDialogue; // 대화 이후 수행할 액션 유형

        //public enum TutorialAction
        //{
        //    None,
        //    ContinueToNextStep,
        //    ActivateGameObject,
        //    WaitForPlayerAction,
        //    EndTutorial
        //}

        //public string targetObjectName; // 활성할 게임 오브젝트 이름
    }
}
