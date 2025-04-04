using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    private float defaultTimeScale = 1f;
    private float doubleTimeScale = 2f;
    private float deploymentTimeScale = 0.2f;

    // IsSpeedUp에 따라 1f나 2f로만 저장함 - 사실 크게 필요 없는 것 같음
    private float currentTimeScale = 1f; 


    // isSpeedUp 상태에 따른 동작
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

    // 배치 중일 때의 타임 스케일. currentTimeScale에 저장하지 않음
    public void SetPlacementTimeScale()
    {
        Time.timeScale = deploymentTimeScale;
    }

    public void SetPauseTime()
    {
        Time.timeScale = 0f;
    }

    // ToggleButton으로 인한 IsSpeedUp의 세터 이벤트로 동작
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

        // 씬 전환시 시간의 흐름 설정
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
