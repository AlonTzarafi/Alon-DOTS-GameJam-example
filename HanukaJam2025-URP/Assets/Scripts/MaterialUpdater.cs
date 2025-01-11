using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UI;
using System;

public class MaterialUpdater : MonoBehaviour
{
    [SerializeField] private MeshRenderer DeathstarRenderer;
    [SerializeField] private MeshRenderer ShieldRenderer;
    [SerializeField] private Image ShieldFillImage;
    [SerializeField] private Image HealthFillImage;
    [SerializeField] private GameObject DeathStarDiedObject;
    [SerializeField] private Image _screenFlashImage;

    [SerializeField] private float ShieldMaxExtra = 1.0f;
    [SerializeField] private float ShieldRelaxSpeed = 1f;
    [SerializeField] private float ShieldScaleTPow = 1f;
    [SerializeField] private Color ShieldColorHit;
    [SerializeField] private Color ShieldColorRelaxed;
    [SerializeField] private float ShieldColorChangeSpeed = 1f;
    [SerializeField] private float ShieldMaxScale = 1.5f;
    [SerializeField] private float ShieldMinScale = 1f;

    private EntityManager _entityManager;
    private Color _originalColor;

    [SerializeField] private float _shieldTension = 0f;
    [SerializeField] private float _fullTentionTimeRemaining = 0f;

    // Effects
    private Color _screenFlashColor;
    private float _screenFlashDurationTotal;
    private float _screenFlashDurationLeft;

    void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _originalColor = DeathstarRenderer.material.color;
    }

    void Update()
    {
        UpdateScreenEffects();
        
        // get singleton DeathStar ECS component
        var deathStarEntity = _entityManager.CreateEntityQuery(typeof(DeathStar)).GetSingletonEntity();
        if (deathStarEntity == Entity.Null)
            return;


        var deathStar = _entityManager.GetComponentData<DeathStar>(deathStarEntity);
        var shield = deathStar.Shield;
        var health = deathStar.Health;
        HealthFillImage.fillAmount = health;
        ShieldFillImage.fillAmount = shield;
        
        DeathStarDiedObject.SetActive(deathStar.Died);
        
        var died = deathStar.Died;
        var damage = deathStar.GetDamage(); // 0 - 1

        // color goes fromoriginalColor to BLACK
        var fullDamagedColor = new Color(0, 0, 0, 1);
        var currentColor = Color.Lerp(_originalColor, fullDamagedColor, damage);
        DeathstarRenderer.material.color = currentColor;

        // _BumpScale goes from 0 to 5
        var damageForNormalScale = math.pow(damage, 2f) * 0.2f;
        DeathstarRenderer.material.SetFloat("_BumpScale", damageForNormalScale * 4);

        var shieldHits = deathStar.ShieldHits;
        // Lerp to 0
        _shieldTension = math.lerp(_shieldTension, 0, Time.deltaTime * ShieldRelaxSpeed);
        // Increase tension by shield hits
        _shieldTension += shieldHits * 0.04f;
        _shieldTension = math.clamp(_shieldTension, 0f, ShieldMaxExtra);

        _fullTentionTimeRemaining = math.max(0f, _fullTentionTimeRemaining);
        _fullTentionTimeRemaining += shieldHits * 0.026f;
        _fullTentionTimeRemaining = math.min(0.1f, _fullTentionTimeRemaining);
        if (_fullTentionTimeRemaining > 0) {
            _fullTentionTimeRemaining -= Time.deltaTime;
            _shieldTension = math.max(_shieldTension, 1);
        }

        if (health <= 0) {
            _shieldTension = 0;
        }

        // Scale
        var shieldScaleT = math.pow(_shieldTension, ShieldMaxExtra);
        shieldScaleT = math.clamp(shieldScaleT, 0f, 1.1f);
        var shieldScaleTarget = math.lerp(ShieldMinScale, ShieldMaxScale, shieldScaleT);
        var shieldCurrentScale = ShieldRenderer.transform.localScale.x;
        var shieldScale = math.lerp(shieldCurrentScale, shieldScaleTarget, Time.deltaTime * 40);
        if (shieldHits > 0 && shieldScale > ShieldMaxScale) {
            shieldScale = ShieldMaxScale;
        }
        shieldScale += shieldHits * 0.2f;
        ShieldRenderer.transform.localScale = new Vector3(shieldScale, shieldScale, shieldScale);

        // Color
        var currentShieldColor = ShieldRenderer.material.GetColor("_DepthColor");
        var shieldTentionForColor = math.remap(0.95f, 1f, 0f, 1f, _shieldTension);
        shieldTentionForColor = math.clamp(shieldTentionForColor, 0f, 1f);
        currentShieldColor = Color.Lerp(currentShieldColor, ShieldColorHit, shieldTentionForColor * Time.deltaTime * ShieldColorChangeSpeed);
        currentShieldColor = Color.Lerp(currentShieldColor, ShieldColorRelaxed, (1 - shieldTentionForColor) * Time.deltaTime * ShieldColorChangeSpeed);
        ShieldRenderer.material.SetColor("_DepthColor", currentShieldColor);

        var shieldOpaqueDepth = math.lerp(130000, 130, shieldTentionForColor);
        ShieldRenderer.material.SetFloat("_OpaqueDepth", shieldOpaqueDepth);
    }

    private void UpdateScreenEffects()
    {
        var effectCallScreenFlashQuery = _entityManager.CreateEntityQuery(typeof(EffectCallScreenFlash));
        if (effectCallScreenFlashQuery.CalculateEntityCount() > 0) {
            // Just get the first one
            var entitiesInQuery = effectCallScreenFlashQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var effectCallScreenFlashEntity = entitiesInQuery[0];
            var effectCallScreenFlash = _entityManager.GetComponentData<EffectCallScreenFlash>(effectCallScreenFlashEntity);
            // Perform the screen flash
            // Add new class members later
            var color = effectCallScreenFlash.Color;
            var duration = effectCallScreenFlash.Duration;

            // Do the screen flash
            _screenFlashColor = new Color(color.x, color.y, color.z, color.w);
            _screenFlashDurationTotal = duration;
            _screenFlashDurationLeft = duration;
        }

        // Update the screen flash
        if (_screenFlashDurationLeft > 0) {
            _screenFlashDurationLeft -= Time.deltaTime;
            // lerp _screenFlashColor alpha to 0
            var t = 1 - (_screenFlashDurationLeft / _screenFlashDurationTotal);
            t = math.pow(t, 0.5f);
            _screenFlashColor.a = math.lerp(_screenFlashColor.a, 0, t);
            _screenFlashImage.color = _screenFlashColor;
        } else {
            _screenFlashImage.color = new Color(0, 0, 0, 0);
        }
    }
}
