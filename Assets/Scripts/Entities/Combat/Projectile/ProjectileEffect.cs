using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Projectile�� �ڽ� ������Ʈ Model�� �� ��ũ��Ʈ
/// </summary>
public class ProjectileEffect : MonoBehaviour
{
    private TrailRenderer trailRenderer;
    private ParticleSystem particleSystem;
    private MeshRenderer meshRenderer;
    private float modelScale;

    [SerializeField] private float trailWidthScale = 0.8f; // �޽� ũ�� ��� Ʈ���� �ʺ� ����
    [SerializeField] private float particleSizeScale = 0.3f; // �޽� ũ�� ��� ��ƼŬ ũ�� ����
    [SerializeField] private float colorIntensity = 0.7f; // Ʈ���� ���� ����

    private void Awake()
    {
        // Model ������Ʈ�� �ִ� ������Ʈ�� ����
        meshRenderer = GetComponent<MeshRenderer>();        
        trailRenderer = GetComponent<TrailRenderer>();
        particleSystem = GetComponent<ParticleSystem>();

        InitializeEffects();
    }

    private void InitializeEffects()
    {
        if (meshRenderer == null || trailRenderer == null) return;

        // �޽� ũ��
        modelScale = transform.localScale.x; // Sphere�� ���, x=y=z

        // Ʈ���� ������ ����
        SetupTrailRenderer();

        if (particleSystem != null)
        {
            SetupParticleSystem();
        }
    }

    private void SetupTrailRenderer()
    {
        // �޽� ��Ƽ���� ��
        Color meshColor = meshRenderer.material.color;

        // Ʈ���Ͽ� ���� �� ����
        Color trailColor = new Color(
            Mathf.Lerp(meshColor.r, 1f, colorIntensity),
            Mathf.Lerp(meshColor.g, 1f, colorIntensity),
            Mathf.Lerp(meshColor.b, 1f, colorIntensity),
            meshColor.a
        );

        // Ʈ���� �ʺ� ����
        float trailWidth = modelScale * trailWidthScale;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = trailWidth * 0.5f; // ������ ������ ���þ���

        // Ʈ���� ���� �׶��̼�
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(trailColor, 0.0f),
                new GradientColorKey(trailColor, 1.0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.7f, 0.0f), // ���� ���İ�
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

        // ��ƼŬ ũ�⵵ �޽� ũ�⿡ �����
        main.startSize = modelScale * particleSizeScale;

        // ��ƼŬ ���� �޽� ��� ������ ����
        main.startColor = meshRenderer.material.color;
    }

    // �޽� ���� ����� �� ȣ��
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

    // ��Ÿ�ӿ��� Ʈ���� ���� ���� ����
    public void SetColorIntensity(float intensity)
    {
        colorIntensity = Mathf.Clamp01(intensity);
        UpdateEffectColors();
    }

    // ��Ÿ�ӿ��� Ʈ���� �ʺ� ���� ����
    public void SetTrailWidthScale(float scale)
    {
        trailWidthScale = scale;
        UpdateEffectSizes();
    }
}
