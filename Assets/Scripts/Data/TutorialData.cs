using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialData", menuName = "Tutorial/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    public string tutorialName;
    public List<TutorialStep> steps = new List<TutorialStep>();

    // 1���� Step�� "Dialogue ��� -> ������� ���� �Ϸ�"�� �⺻ ���ڷ� �ϰ���
    [Serializable]
    public class TutorialStep
    {
        public string stepName;
        public List<string> dialogues = new List<string>();
        public Vector2 dialogueBoxPosition = new Vector2(0, 0);
        public bool requireUserAction;

        [Tooltip("requireUserAction�� true�� ���� ���. Ȱ��ȭ �� �ش� ��ư�� �Է��� ��ٸ��ϴ�.")]
        public string expectedButtonName;

    }
}
