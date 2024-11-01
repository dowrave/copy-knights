using UnityEngine;
using TMPro;
using System;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private AnimationCurve alphaCurve;
    //private TextMeshProUGUI valueText; // Canvas를 부모에 갖고 있다면 이렇게 씀
    private TMP_Text valueText; // WorldSpace에 바로 쓴다면 이렇게 씀

    [SerializeField] private Color attackColor;
    [SerializeField] private Color healColor;

    private float timer;

    private void Awake()
    {
        if (valueText == null)
        {
            //valueText = GetComponent<TextMeshProUGUI>(); // Canvas를 부모에 갖고 있다면 이렇게 씀
            valueText = GetComponent<TextMeshPro>(); // WorldSpace에 바로 쓴다면 이렇게 씀
        }
    }

    public void OnObjectSpawn()
    {
        timer = 0f; 
    }

    private void OnEnable()
    {
        OnObjectSpawn();
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
            Debug.Log("대미지 텍스트가 설정되지 않음");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        float alpha = alphaCurve.Evaluate(timer / lifetime);

        if (timer >= lifetime)
        {
            ObjectPoolManager.Instance.ReturnToPool(ObjectPoolManager.Instance.FLOATING_TEXT_TAG, gameObject);
        }
    }

    private void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}
