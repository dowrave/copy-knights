using UnityEngine;
using TMPro;
using System;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private AnimationCurve alphaCurve;
    //private TextMeshProUGUI valueText; // Canvas�� �θ� ���� �ִٸ� �̷��� ��
    private TMP_Text valueText; // WorldSpace�� �ٷ� ���ٸ� �̷��� ��

    [SerializeField] private Color attackColor;
    [SerializeField] private Color healColor;

    private float timer;

    private void Awake()
    {
        if (valueText == null)
        {
            //valueText = GetComponent<TextMeshProUGUI>(); // Canvas�� �θ� ���� �ִٸ� �̷��� ��
            valueText = GetComponent<TextMeshPro>(); // WorldSpace�� �ٷ� ���ٸ� �̷��� ��
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
            

            // �� ����
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
            Debug.Log("����� �ؽ�Ʈ�� �������� ����");
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
