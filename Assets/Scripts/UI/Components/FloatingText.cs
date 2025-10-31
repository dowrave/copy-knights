using UnityEngine;
using TMPro;
using System;

public class FloatingText : MonoBehaviour, IPooledObject
{
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private AnimationCurve alphaCurve = default!;
    private TMP_Text? valueText; // WorldSpace�� �ٷ� ���ٸ� �̷��� ��

    [SerializeField] private Color attackColor;
    [SerializeField] private Color healColor;

    private float timer;

    // �� ������Ʈ�� ȸ���� ObjectPoolManager.ShowFloatingText�� ���� �� ���ķδ� �������� ����

    private void Awake()
    {
        if (valueText == null)
        {
            valueText = GetComponent<TextMeshPro>(); // WorldSpace�� �ٷ� ���ٸ� �̷��� ��
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
            Logger.Log("����� �ؽ�Ʈ�� �������� ����");
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // ���� ī�޶��� ���� �������� �̵���Ű��
        transform.position += Camera.main.transform.up * moveSpeed * Time.deltaTime;
       
        float alpha = alphaCurve.Evaluate(timer / lifetime);

        if (timer >= lifetime)
        {
            ObjectPoolManager.Instance!.ReturnToPool(ObjectPoolManager.Instance.FLOATING_TEXT_TAG, gameObject);
        }
    }
}
