using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class CameraDataUpdater : MonoBehaviour
{
    private EntityManager _entityManager;
    private Entity _cameraDataEntity;

    void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _cameraDataEntity = _entityManager.CreateEntity(typeof(CameraData));
    }

    void Update()
    {
        var cam = Camera.main;
        if (!cam) return;

        float4x4 viewMatrix = cam.worldToCameraMatrix;
        float4x4 projectionMatrix = cam.projectionMatrix;

        float4x4 viewProj = math.mul((float4x4)projectionMatrix, (float4x4)viewMatrix);

        float2 screenSize = new float2(Screen.width, Screen.height);

        _entityManager.SetComponentData(_cameraDataEntity, new CameraData
        {
            ViewProjectionMatrix = viewProj,
            ScreenSize = screenSize
        });
    }
}
