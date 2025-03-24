using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    private float defaultTimeScale = 1f;
    private float doubleTimeScale = 2f;
    private float deploymentTimeScale = 0.2f;

    // IsSpeedUp�� ���� 1f�� 2f�θ� ������ - ��� ũ�� �ʿ� ���� �� ����
    private float currentTimeScale = 1f; 


    // isSpeedUp ���¿� ���� ����
    public void UpdateTimeScale(bool? isSpeedUp = null)
    {
        if (StageManager.Instance != null)
        {
            bool isSpeedUpValue = isSpeedUp ?? StageManager.Instance.IsSpeedUp;
            currentTimeScale = isSpeedUpValue ? doubleTimeScale : defaultTimeScale;
        }
        else
        {
            currentTimeScale = 1f;
        }

        Time.timeScale = currentTimeScale;
    }

    // ��ġ ���� ���� Ÿ�� ������. currentTimeScale�� �������� ����
    public void SetPlacementTimeScale()
    {
        Time.timeScale = deploymentTimeScale;
    }

    public void SetPauseTime()
    {
        Time.timeScale = 0f;
    }

    // ToggleButton���� ���� IsSpeedUp�� ���� �̺�Ʈ�� ����
    public void ReactSpeedToggleButton(bool isSpeedUp)
    {
        UpdateTimeScale(isSpeedUp);
    }


    public float GetCurrentTimeScale()
    {
        return currentTimeScale;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //if (StageManager.Instance != null)
        //{
        //    StageManager.Instance.OnSpeedUpChanged += ReactSpeedToggleButton;
        //}

        // �� ��ȯ�� �ð��� �帧 ����
        UpdateTimeScale();
    }


    private void OnDisable()
    {
        //if (StageManager.Instance != null)
        //{
        //    StageManager.Instance.OnSpeedUpChanged -= ReactSpeedToggleButton;
        //}
    }
}
