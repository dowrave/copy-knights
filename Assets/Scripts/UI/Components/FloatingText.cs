using UnityEngine;
using TMPro;
using System;

public class FloatingText : MonoBehaviour, IPooledObject
{
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private AnimationCurve alphaCurve = default!;
    private TMP_Text? valueText; // WorldSpace에 바로 쓴다면 이렇게 씀

    [SerializeField] private Color attackColor;
    [SerializeField] private Color healColor;

    private float timer;

    // 이 컴포넌트의 회전은 ObjectPoolManager.ShowFloatingText에 들어가고 그 이후로는 수정되지 않음

    private void Awake()
    {
        if (valueText == null)
        {
            valueText = GetComponent<TextMeshPro>(); // WorldSpace에 바로 쓴다면 이렇게 씀
        }


    }

    public void OnObjectSpawn(string tag)
    {
        timer = 0f;
    }

    public void SetValue(float damage, bool isHealing)
    {
        if (valueText != null)
        {
            // 색 설정
            if (isHealing)
            {
                valueText.text = '+' + Mathf.Round(damage).ToString("F0");
                valueText.color = healColor;
            }
            else
            {
                valueText.text = Mathf.Round(damage).ToString("F0");
                valueText.color = attackColor;
            }
        }
        else
        {
            Logger.Log("대미지 텍스트가 설정되지 않음");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // 메인 카메라의 윗쪽 방향으로 이동시키기
        transform.position += Camera.main.transform.up * moveSpeed * Time.deltaTime;
       
        float alpha = alphaCurve.Evaluate(timer / lifetime);

        if (timer >= lifetime)
        {
            ObjectPoolManager.Instance!.ReturnToPool(ObjectPoolManager.Instance.FLOATING_TEXT_TAG, gameObject);
        }
    }
}
