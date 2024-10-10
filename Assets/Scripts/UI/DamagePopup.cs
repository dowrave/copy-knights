using UnityEngine;
using TMPro;
using System;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private AnimationCurve alphaCurve;
    //private TextMeshProUGUI damageText; // Canvas�� �θ� ���� �ִٸ� �̷��� ��
    private TMP_Text damageText; // WorldSpace�� �ٷ� ���ٸ� �̷��� ��

    private float timer;

    private void Awake()
    {
        if (damageText == null)
        {
            //damageText = GetComponent<TextMeshProUGUI>(); // Canvas�� �θ� ���� �ִٸ� �̷��� ��
            damageText = GetComponent<TextMeshPro>(); // WorldSpace�� �ٷ� ���ٸ� �̷��� ��
        }
    }

    public void OnObjectSpawn()
    {
        timer = 0f; 
    }

    public void SetDamage(float damage)
    {
        if (damageText != null)
        {
            damageText.text = Mathf.Round(damage).ToString();
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
            ObjectPoolManager.Instance.ReturnToPool("DamagePopup", gameObject);
        }
    }

    private void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}
