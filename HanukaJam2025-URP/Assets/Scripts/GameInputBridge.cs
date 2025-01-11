using System;
using System.Linq;
using AutoLetterbox;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameInputBridge : MonoBehaviour, InputSystem_Actions.IGameActions
{
    [SerializeField] private Transform _targetCameraPositionTransform;
    [SerializeField] private float _targetCameraPositionTransformThreshold;

    [SerializeField] private Transform _playerCursor;

    [SerializeField] private float _abilityCooldownChargeSpeed = 0.25f;
    [SerializeField] private float _abilityCooldownColorChangeSpeed = 5f;

    // InputSystem_Actions is the C# class that Unity generated
    private InputSystem_Actions controls;

    // State
    private bool _allowInput = false;
    private Vector2 _rawAim;
    private Vector2 _aim;
    private bool _click;

    // Ability state
    private float _abilityCooldown = 1f;
    private float _abilityCooldownOnOff = 1f;
    private float _cursorTime;
    private float _cursorTimeScale = 1f;

    // Singleton entity
    private Entity singletonEntity;

    private void Awake()
    {
        // Create the singleton entity here
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        singletonEntity = manager.CreateEntity(typeof(GameInputData));
        manager.AddComponentData(singletonEntity, new GameInputData());

        _rawAim = Vector2.one * -9999f;
    }
    
    public void OnEnable()
    {
        if (controls == null)
        {
            controls = new InputSystem_Actions();
            // Tell the "gameplay" action map that we want to get told about
            // when actions get triggered.
            controls.Game.SetCallbacks(this);
        }
        controls.Game.Enable();

        RefreshEntity();
    }

    public void Update()
    {
        UpdateVisualCursor();

        UpdateCooldown();

        // Write the latest values into our singleton
        RefreshEntity();
    }

    private void UpdateVisualCursor()
    {
        _cursorTime += Time.deltaTime * _cursorTimeScale * 1.8f;
        var relaxSpeed = 640f;
        if (_cursorTimeScale > 1f) {
            _cursorTimeScale = math.max(_cursorTimeScale - Time.deltaTime * relaxSpeed, 1f);
        } else if (_cursorTimeScale < 1f) {
            _cursorTimeScale = math.min(_cursorTimeScale + Time.deltaTime * relaxSpeed, 1f);
        }

        var aimOnScreen = math.abs(_aim.x) < 1 && math.abs(_aim.y) < 1;
        Cursor.visible = !aimOnScreen;
        var screenPosition = _rawAim;
        _playerCursor.position = screenPosition;

        
        // update the _MyTime shader argument
        var cursorImage = _playerCursor.GetComponentInChildren<Image>();
        cursorImage.material.SetFloat("_MyTime", _cursorTime);
    }

    private void UpdateCooldown()
    {
        if (_allowInput) {
            _abilityCooldown -= Time.deltaTime * _abilityCooldownChargeSpeed;
            _abilityCooldown = math.clamp(_abilityCooldown, 0, 1);
        }

        float isCooldownOnOff = _abilityCooldown > 0 ? 10 : 0;
        _abilityCooldownOnOff = math.lerp(_abilityCooldownOnOff, isCooldownOnOff, Time.deltaTime * _abilityCooldownColorChangeSpeed);
        _abilityCooldownOnOff = math.clamp(_abilityCooldownOnOff, 0, 1);
        var cursorImage = _playerCursor.GetComponentInChildren<Image>();
        cursorImage.material.SetFloat("_Cooldown", _abilityCooldown);
        cursorImage.material.SetFloat("_CooldownOnOff", _abilityCooldownOnOff);
    }

    public void OnDisable()
    {
        controls.Game.Disable();
    }

    public void OnGameAim(InputAction.CallbackContext context)
    {
        var screenPixel = context.ReadValue<Vector2>();
        _rawAim = screenPixel;


        var regularScreenRect = new Rect(0, 0, Screen.width, Screen.height);
        var camera = GetCamera();
        var viewportRect = camera?.rect ?? new Rect(0, 0, 0, 0);
        // take into account all 4 values of the viewportRect
        var fixedScreenRect = new Rect(
            regularScreenRect.x + regularScreenRect.width * viewportRect.x,
            regularScreenRect.y + regularScreenRect.height * viewportRect.y,
            regularScreenRect.width * viewportRect.width,
            regularScreenRect.height * viewportRect.height
        );

        // var screenRect = regularScreenRect;
        var screenRect = fixedScreenRect;

        // Get a value between -1 and 1
        // _aim = new Vector2(
        //     (screenPixel.x / screenSize.x) * 2 - 1,
        //     (screenPixel.y / screenSize.y) * 2 - 1
        // );
        // Get a value between -1 and 1 - Take into account all 4 values of the screenRect
        _aim = new Vector2(
            ((screenPixel.x - screenRect.x) / screenRect.width) * 2 - 1,
            ((screenPixel.y - screenRect.y) / screenRect.height) * 2 - 1
        );

        // Debug.Log($"regularScreenRect: {regularScreenRect}, fixedScreenRect: {fixedScreenRect}, viewportRect: {viewportRect}, _aim: {_aim}");
    }

    public void OnGameClick(InputAction.CallbackContext context)
    {
        // if (context.started) {
        //     _click = true;
        // }

        // I guess for Unity "mouse up" is called "canceled"....
        if (context.canceled) {
            _click = true;
        }
    }

    private Camera GetCamera()
    {
        var camera = Camera.main;
        if (camera == null) {
            camera = Camera.allCameras.FirstOrDefault(c => c.isActiveAndEnabled && c.gameObject.activeInHierarchy);
        }
        return camera;
    }
    
    private void RefreshEntity()
    {
        var camera = Camera.main;
        _allowInput = (camera.transform.position - _targetCameraPositionTransform.position).magnitude < _targetCameraPositionTransformThreshold;
        var allowInputAndCooldownReady = _allowInput && _abilityCooldown <= 0;
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        manager.SetComponentData(singletonEntity, new GameInputData
        {
            AllowInput = allowInputAndCooldownReady,
            Aim = _aim,
            Click = _click
        });

        if (_click) {
            _click = false;

            if (allowInputAndCooldownReady) {
                // We are using up the ability this frame
                _abilityCooldown = 1;
                _cursorTimeScale = -60f;
            }
        }
    }
}
