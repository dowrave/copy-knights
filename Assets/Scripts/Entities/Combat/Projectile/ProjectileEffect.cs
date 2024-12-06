using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Projectile의 자식 오브젝트 Model에 들어갈 스크립트
/// </summary>
public class ProjectileEffect : MonoBehaviour
{
    private TrailRenderer trailRenderer;
    private ParticleSystem particleSystem;
    private MeshRenderer meshRenderer;
    private float modelScale;

    [SerializeField] private float trailWidthScale = 0.8f; // 메쉬 크기 대비 트레일 너비 비율
    [SerializeField] private float particleSizeScale = 0.3f; // 메쉬 크기 대비 파티클 크기 비율
    [SerializeField] private float colorIntensity = 0.7f; // 트레일 색상 강도

    private void Awake()
    {
        // Model 오브젝트에 있는 컴포넌트들 참조
        meshRenderer = GetComponent<MeshRenderer>();        
        trailRenderer = GetComponent<TrailRenderer>();
        particleSystem = GetComponent<ParticleSystem>();

        InitializeEffects();
    }

    private void InitializeEffects()
    {
        if (meshRenderer == null || trailRenderer == null) return;

        // 메쉬 크기
        modelScale = transform.localScale.x; // Sphere일 경우, x=y=z

        // 트레일 렌더러 설정
        SetupTrailRenderer();

        if (particleSystem != null)
        {
            SetupParticleSystem();
        }
    }

    private void SetupTrailRenderer()
    {
        // 메쉬 머티리얼 색
        Color meshColor = meshRenderer.material.color;

        // 트레일용 밝은 색 생성
        Color trailColor = new Color(
            Mathf.Lerp(meshColor.r, 1f, colorIntensity),
            Mathf.Lerp(meshColor.g, 1f, colorIntensity),
            Mathf.Lerp(meshColor.b, 1f, colorIntensity),
            meshColor.a
        );

        // 트레일 너비 설정
        float trailWidth = modelScale * trailWidthScale;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = trailWidth * 0.5f; // 끝으로 갈수록 가늘어짐

        // 트레일 색상 그라데이션
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(trailColor, 0.0f),
                new GradientColorKey(trailColor, 1.0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.7f, 0.0f), // 시작 알파값
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trailRenderer.colorGradient = gradient;

        trailRenderer.time = 0.2f;
        trailRenderer.minVertexDistance = 0.1f;
    }

    private void SetupParticleSystem()
    {
        var main = particleSystem.main;

        // 파티클 크기도 메쉬 크기에 맞춘다
        main.startSize = modelScale * particleSizeScale;

        // 파티클 색도 메쉬 기반 색으로 수정
        main.startColor = meshRenderer.material.color;
    }

    // 메쉬 색이 변경될 때 호출
    public void UpdateEffectColors()
    {
        if (meshRenderer != null && trailRenderer != null)
        {
            SetupTrailRenderer();
        }
    }

    public void UpdateEffectSizes()
    {
        modelScale = transform.localScale.x;
        SetupTrailRenderer();
        if (particleSystem != null)
        {
            SetupParticleSystem();
        }
    }

    // 런타임에서 트레일 색상 강도 조절
    public void SetColorIntensity(float intensity)
    {
        colorIntensity = Mathf.Clamp01(intensity);
        UpdateEffectColors();
    }

    // 런타임에서 트레일 너비 비율 조절
    public void SetTrailWidthScale(float scale)
    {
        trailWidthScale = scale;
        UpdateEffectSizes();
    }
}
