using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class NotificationToastManager : MonoBehaviour
{
    public static NotificationToastManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject notificationToastPrefab = default!;
    [SerializeField] private Transform notificationContainer = default!;
    [SerializeField] private int maxVisibleNotifications = 4;  // 최대 표시 알림 수 
    [SerializeField] private float verticalSpacing = 10f;  // 알림 간 수직 간격
    [SerializeField] private float moveDuration = 0.3f;  // 알림 위치 이동 애니메이션 시간

    private List<NotificationToast> activeToasts = new List<NotificationToast>();
    private float toastHeight = -1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationToastPrefab == null)
        {
            Logger.LogError("NotificationToast 프리팹이 할당되지 않음");
            return;
        }

        if (activeToasts.Count >= maxVisibleNotifications)
        {
            // 가장 오래된 토스트는 리스트의 맨 앞 요소t
            NotificationToast oldestToast = activeToasts[0];
            activeToasts.RemoveAt(0);

            // Dismiss를 호출하면 onToastClosed 콜백이 실행, 리스트에서 자동으로 제거된다. 
            oldestToast.Dismiss();

            UpdateToastPositions();
        }

        if (toastHeight < 0)
        {
            toastHeight = notificationToastPrefab.GetComponent<RectTransform>().rect.height;
        }

        // 새로운 토스트가 등장할 Y좌표를 미리 계산한다. activeToasts.Count가 새 토스트의 인덱스이므로 이를 사용한다.
        float newToastTargetY = -activeToasts.Count * (toastHeight + verticalSpacing);

        GameObject toastObject = Instantiate(notificationToastPrefab, notificationContainer);
        NotificationToast newToast = toastObject.GetComponent<NotificationToast>();

        // 새로운 토스트 추가
        activeToasts.Add(newToast);

        // 새 토스트 초기화(등장 애니메이션 시작)
        newToast.Initialize(message, newToastTargetY, () => OnToastClosed(newToast));
    }

    // 토스트가 나타나거나 사라질 때 모든 토스트의 Y위치를 계산하고 이동시킨다.
    private void UpdateToastPositions()
    {
        // 아래에서부터의 위치 계산
        for (int i = 0; i < activeToasts.Count; i++)
        {
            float targetY = -i * (toastHeight + verticalSpacing);
            activeToasts[i].MoveToY(targetY, moveDuration);
        }
    }

    private void OnToastClosed(NotificationToast toast)
    {
       if (activeToasts.Contains(toast))
        {
            activeToasts.Remove(toast);
            UpdateToastPositions();
        }
    }

}
