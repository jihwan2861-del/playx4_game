using UnityEngine;

/// <summary>
/// 허브 씬(Hub_Scene)에 상호작용 포인트(GaragePoint, HologramPoint, WorkshopPoint)가 없을 때
/// 런타임 시작 시 적절한 기본 위치에 자동으로 실시간 생성해주는 도우미 스크립트입니다.
/// 하이어라키에서 빈 오브젝트를 만들고 이 스크립트를 붙이거나, HubUIManager 오브젝트에 붙여두면 자동으로 동작합니다.
/// </summary>
public class HubSceneInitializer : MonoBehaviour
{
    [Header("자동 생성 여부")]
    [Tooltip("씬에 관련 포인트들이 없을 경우, 런타임 시작 시 자동으로 생성할지 여부입니다.")]
    public bool autoCreateIfMissing = true;

    [Header("자동 트리거 여부")]
    [Tooltip("자동 생성된 포인트들을 밟기만 해도(F키 누르지 않아도) 자동으로 패널이 열리게 설정합니다.")]
    public bool defaultTriggerOnEnter = false;

    [Header("상호작용 반경")]
    public float defaultInteractRadius = 2.5f;

    [Header("생성 위치 설정 (2D 좌표)")]
    public Vector2 garagePointPos = new Vector2(-5f, 3f);
    public Vector2 hologramPointPos = new Vector2(5f, 3f);
    public Vector2 workshopPointPos = new Vector2(0f, -3f);

    void Awake()
    {
        if (!autoCreateIfMissing) return;

        InitializePoints();
    }

    public void InitializePoints()
    {
        // 씬 내에 기존 HubInteractionPoint들이 있는지 먼저 체크합니다.
        HubInteractionPoint[] existingPoints = FindObjectsOfType<HubInteractionPoint>();

        bool hasGarage = false;
        bool hasHologram = false;
        bool hasWorkshop = false;

        foreach (var p in existingPoints)
        {
            if (p.pointType == HubInteractionPoint.PointType.Garage) hasGarage = true;
            if (p.pointType == HubInteractionPoint.PointType.Hologram) hasHologram = true;
            if (p.pointType == HubInteractionPoint.PointType.Workshop) hasWorkshop = true;
        }

        // 1. 차고 포인트 (GaragePoint) 생성
        if (!hasGarage)
        {
            CreatePoint("GaragePoint", HubInteractionPoint.PointType.Garage, garagePointPos);
            Debug.Log($"<color=#00FFFF>✔ [자동 세팅] 차고 상호작용 포인트(GaragePoint)를 {garagePointPos} 위치에 생성했습니다. (TriggerOnEnter: {defaultTriggerOnEnter})</color>");
        }
        else
        {
            Debug.Log("ℹ️ [자동 세팅] 씬에 이미 GaragePoint가 존재하므로 자동 생성을 건너뜁니다.");
        }

        // 2. 홀로그램 포인트 (HologramPoint) 생성
        if (!hasHologram)
        {
            CreatePoint("HologramPoint", HubInteractionPoint.PointType.Hologram, hologramPointPos);
            Debug.Log($"<color=#FF00FF>✔ [자동 세팅] 홀로그램 상호작용 포인트(HologramPoint)를 {hologramPointPos} 위치에 생성했습니다. (TriggerOnEnter: {defaultTriggerOnEnter})</color>");
        }
        else
        {
            Debug.Log("ℹ️ [자동 세팅] 씬에 이미 HologramPoint가 존재하므로 자동 생성을 건너뜁니다.");
        }

        // 3. 작업실 포인트 (WorkshopPoint) 생성
        if (!hasWorkshop)
        {
            CreatePoint("WorkshopPoint", HubInteractionPoint.PointType.Workshop, workshopPointPos);
            Debug.Log($"<color=#FFFF00>✔ [자동 세팅] 작업실 상호작용 포인트(WorkshopPoint)를 {workshopPointPos} 위치에 생성했습니다. (TriggerOnEnter: {defaultTriggerOnEnter})</color>");
        }
        else
        {
            Debug.Log("ℹ️ [자동 세팅] 씬에 이미 WorkshopPoint가 존재하므로 자동 생성을 건너뜁니다.");
        }
    }

    private void CreatePoint(string name, HubInteractionPoint.PointType type, Vector2 position)
    {
        GameObject pointObj = new GameObject(name);
        pointObj.transform.position = new Vector3(position.x, position.y, 0f);

        // 상호작용 포인트 스크립트 장착 및 초기 설정
        HubInteractionPoint ip = pointObj.AddComponent<HubInteractionPoint>();
        ip.pointType = type;
        ip.triggerOnEnter = defaultTriggerOnEnter;
        ip.interactRadius = defaultInteractRadius;
        ip.interactKey = KeyCode.F;

        // Gizmo 등이 정상적으로 그려질 수 있도록 씬 뷰에 표시되도록 설정
        // (GameObject.FindWithTag나 FindObjectOfType으로 찾아지도록 적절한 네이밍 제공)
    }
}
