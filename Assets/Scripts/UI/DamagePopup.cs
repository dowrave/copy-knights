using UnityEngine;
using TMPro;
using System;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private AnimationCurve alphaCurve;
    //private TextMeshProUGUI damageText; // Canvas를 부모에 갖고 있다면 이렇게 씀
    private TMP_Text damageText; // WorldSpace에 바로 쓴다면 이렇게 씀

    private float timer;

    private void Awake()
    {
        if (damageText == null)
        {
            //damageText = GetComponent<TextMeshProUGUI>(); // Canvas를 부모에 갖고 있다면 이렇게 씀
            damageText = GetComponent<TextMeshPro>(); // WorldSpace에 바로 쓴다면 이렇게 씀
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
            ObjectPoolManager.Instance.ReturnToPool("DamagePopup", gameObject);
        }
    }

    private void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }
}
