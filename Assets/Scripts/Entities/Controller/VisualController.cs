using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

// 코루틴만 필요하다면 MonoBehaviour을 쓸 필요는 없다.  : _owner.StartCoroutine(_coroutine); 은 C# 클래스만으로 충분함
// 여기선 세부 설정값들도 쓰고 있기 때문에 MonoBehaviour을 유지하고 참조하는 것으로 진행.
public class VisualController: MonoBehaviour
{
    private UnitEntity _owner;

    // 이 객체가 갖고 있는 메쉬 렌더러들
    [SerializeField] private List<Renderer> renderers;

    [Header("Settings")]
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private Color flashColor = new Color(0.3f, 0.3f, 0.3f, 1);
    [SerializeField] private float fadeDuration = 0.3f;

    private MaterialPropertyBlock _propBlock; // 1개만 정의해도 모든 렌더러에 사용 가능
    private Dictionary<Renderer, Color> _originalEmissionColors = new Dictionary<Renderer, Color>();
    private List<Material> materialInstances = new List<Material>();

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor"); 
    private static readonly int FadeAmountID = Shader.PropertyToID("_FadeAmount"); 
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor"); 

    private Coroutine _flashCoroutine; // 피격 시 머티리얼 색 변하는 코루틴
    
    public System.Action OnDeathAnimationComplete = delegate { };

    private void Awake()
    {
        // 메쉬 색상 설정
        _propBlock = new MaterialPropertyBlock();

        // 갖고 있는 렌더러들 설정
        if (renderers.Count == 0)
        {
            Logger.LogError("UnitEntity에 할당된 Renderer가 없음");
            return;
        }
        else
        {
            foreach (var renderer in renderers)
            {
                Color originalColor = Color.black;
                // URP - Lit 사용한다고 가정
                if (renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"))
                {
                    Logger.Log("셰이더의 Emission 키워드 사용 가능");
                    renderer.GetPropertyBlock(_propBlock);
                    if (_propBlock.GetColor(EmissionColorID) != Color.clear)
                    {
                        originalColor = _propBlock.GetColor(EmissionColorID);
                    }
                    else
                    {
                        originalColor = renderer.sharedMaterial.GetColor(EmissionColorID);
                    }
                }

                _originalEmissionColors.Add(renderer, originalColor);
            }
        }
    }

    public void Initialize(UnitEntity owner)
    {
        _owner = owner;

        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat("_FadeAmount", 1f);
            renderer.SetPropertyBlock(_propBlock);
        }
    }

    // 대미지를 받을 때 실행됨
    public void PlayHitFeedback(AttackSource attackSource, UnitEntity owner)
    {
        // 1. 유닛 자체가 깜빡이는 현상
        if (_flashCoroutine != null)
        {
            _owner.StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        _flashCoroutine = _owner.StartCoroutine(PlayModelFlashVFX());

        // 2. attackSource에 의한 GetHit 이펙트 재생
        PlayGetHitVFX(attackSource, owner);
    }

    // 대미지를 받거나 힐을 받을 때 실행됨
    public void PlayGetHitVFX(AttackSource attackSource, UnitEntity owner)
    {
        string tag = attackSource.HitEffectTag;

        if (tag != string.Empty)
        {
            Vector3 effectPosition = transform.position;
            GameObject? hitEffect = ObjectPoolManager.Instance!.SpawnFromPool(tag, effectPosition, Quaternion.identity);

            if (hitEffect != null)
            {
                CombatVFXController hitVFXController = hitEffect.GetComponent<CombatVFXController>();
                hitVFXController.Initialize(attackSource, owner, attackSource.HitEffectTag);
            }
        }
    }

    public void PlayDeathAnimation()
    {
        if (renderers.Count == 0)
        {
            // 렌더러가 없으면 즉시 실행
            OnDeathAnimationComplete?.Invoke();
            return;
        }

        // 렌더러가 있다면 서서히 사라지는 애니메이션 구현
        float currentAlpha = 1f; 
        float endAlpha = 0f;
        
        DOTween.To(() => currentAlpha, x => currentAlpha = x, endAlpha, fadeDuration) // 
            .OnUpdate(() =>
            {   
                // 모든 렌더러에서 동시에 진행
                foreach (Renderer renderer in renderers)
                {
                    renderer.GetPropertyBlock(_propBlock);
                    _propBlock.SetFloat("_FadeAmount", currentAlpha);
                    renderer.SetPropertyBlock(_propBlock);
                }
            })
            .OnComplete(() =>
            {
                OnDeathAnimationComplete?.Invoke();
            }
        );
    }

    public virtual void AssignColorToRenderers(Color primaryColor, Color secondaryColor)
    {
        if (renderers.Count > 0)
        {
            renderers[0].GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", primaryColor); // URP Lit 기준
            renderers[0].SetPropertyBlock(_propBlock);
        }
            
        if (renderers.Count > 1)
        {
            renderers[1].GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", secondaryColor); // URP Lit 기준
            renderers[1].SetPropertyBlock(_propBlock);
        }
    }

    // 피격 시 모델이 반짝이는 효과
    private IEnumerator PlayModelFlashVFX()
    {
        foreach (Renderer renderer in renderers)
        {
            // 현재 렌더러의 프로퍼티 블록 상태를 가져옴 (다른 프로퍼티 유지를 위해)
            renderer.GetPropertyBlock(_propBlock);
            // Emission 색상만 덮어씀
            _propBlock.SetColor(EmissionColorID, flashColor);
            renderer.SetPropertyBlock(_propBlock);
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (var renderer in renderers)
        {
            // Dictionary에서 해당 렌더러의 원래 색상을 찾아옴
            Color originalColor = _originalEmissionColors[renderer];
            
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColorID, originalColor);
            renderer.SetPropertyBlock(_propBlock);
        }

        _flashCoroutine = null;
    }
}