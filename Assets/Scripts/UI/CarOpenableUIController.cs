using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플로팅 바의 도어/트렁크 개폐 UI 컨트롤러.
/// Button_Door와 Button_Trunk 클릭 입력을 받아 씬 내 CarOpenableController를 제어하고,
/// 활성화 시 은은한 둥근 사각 ActiveHighlight를 토글합니다.
/// </summary>
[DisallowMultipleComponent]
public class CarOpenableUIController : MonoBehaviour
{
    [Header("UI 버튼 및 하이라이트 (비워두면 자식에서 자동 탐색)")]
    [SerializeField] private Button doorButton;
    [SerializeField] private GameObject doorActiveHighlight;

    [SerializeField] private Button trunkButton;
    [SerializeField] private GameObject trunkActiveHighlight;

    [Header("3D 도어/트렁크 컨트롤러 (비워두면 씬에서 자동 탐색)")]
    [Tooltip("씬의 도어·트렁크 CarOpenableController 목록")]
    [SerializeField] private List<CarOpenableController> openableControllers = new List<CarOpenableController>();

    private bool isDoorsOpened = false;
    private bool isTrunkOpened = false;

    public bool IsDoorsOpened => isDoorsOpened;
    public bool IsTrunkOpened => isTrunkOpened;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureControllerInitializedOnPlay()
    {
        CarOpenableUIController controller =
            FindAnyObjectByType<CarOpenableUIController>(FindObjectsInactive.Include);

        if (controller != null)
        {
            if (!controller.enabled)
            {
                controller.enabled = true;
            }

            controller.Initialize();
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    /// <summary>
    /// 참조 검색 및 버튼 이벤트 바인딩을 수행합니다.
    /// </summary>
    public void Initialize()
    {
        ResolveReferences();
        BindEvents();
    }

    /// <summary>
    /// 비어있는 참조들을 자동 탐색하여 연결합니다.
    /// </summary>
    public void ResolveReferences()
    {
        // 1. 도어 버튼 및 하이라이트 탐색
        if (doorButton == null)
        {
            Transform t = transform.Find("Button_Door");
            if (t != null) doorButton = t.GetComponent<Button>();

            if (doorButton == null)
            {
                GameObject obj = GameObject.Find("Button_Door");
                if (obj != null) doorButton = obj.GetComponent<Button>();
            }
        }

        if (doorActiveHighlight == null && doorButton != null)
        {
            Transform hl = doorButton.transform.Find("ActiveHighlight");
            if (hl != null) doorActiveHighlight = hl.gameObject;
        }

        // 2. 트렁크 버튼 및 하이라이트 탐색
        if (trunkButton == null)
        {
            Transform t = transform.Find("Button_Trunk");
            if (t != null) trunkButton = t.GetComponent<Button>();

            if (trunkButton == null)
            {
                GameObject obj = GameObject.Find("Button_Trunk");
                if (obj != null) trunkButton = obj.GetComponent<Button>();
            }
        }

        if (trunkActiveHighlight == null && trunkButton != null)
        {
            Transform hl = trunkButton.transform.Find("ActiveHighlight");
            if (hl != null) trunkActiveHighlight = hl.gameObject;
        }

        // 3. 3D CarOpenableController 목록 정리 및 자동 탐색
        if (openableControllers != null)
        {
            openableControllers.RemoveAll(c => c == null);
        }

        if (openableControllers == null || openableControllers.Count == 0)
        {
            openableControllers = new List<CarOpenableController>(
                FindObjectsByType<CarOpenableController>(FindObjectsInactive.Include)
            );
        }
    }

    private void BindEvents()
    {
        if (doorButton != null)
        {
            doorButton.onClick.RemoveListener(ToggleDoors);
            doorButton.onClick.AddListener(ToggleDoors);
        }

        if (trunkButton != null)
        {
            trunkButton.onClick.RemoveListener(ToggleTrunk);
            trunkButton.onClick.AddListener(ToggleTrunk);
        }
    }

    private void UnbindEvents()
    {
        if (doorButton != null)
        {
            doorButton.onClick.RemoveListener(ToggleDoors);
        }

        if (trunkButton != null)
        {
            trunkButton.onClick.RemoveListener(ToggleTrunk);
        }
    }

    private bool IsDoorController(CarOpenableController openable)
    {
        if (openable == null) return false;
        if (openable.IsDoor) return true;
        string n = openable.name.ToLower();
        return n.Contains("door") || n.Contains("fl") || n.Contains("fr");
    }

    private bool IsTrunkController(CarOpenableController openable)
    {
        if (openable == null) return false;
        if (openable.Type == CarOpenableController.OpenableType.Trunk) return true;
        string n = openable.name.ToLower();
        return n.Contains("trunk") || n.Contains("tailgate") || n.Contains("boot");
    }

    /// <summary>
    /// 좌/우 도어를 동시에 토글하고 하이라이트 UI를 갱신합니다.
    /// </summary>
    public void ToggleDoors()
    {
        ResolveReferences();

        List<CarOpenableController> doors = new List<CarOpenableController>();
        if (openableControllers != null)
        {
            foreach (CarOpenableController openable in openableControllers)
            {
                if (IsDoorController(openable))
                {
                    doors.Add(openable);
                }
            }
        }

        // 도어가 없으면 씬에서 즉시 재탐색
        if (doors.Count == 0)
        {
            CarOpenableController[] found = FindObjectsByType<CarOpenableController>(FindObjectsInactive.Include);
            foreach (CarOpenableController openable in found)
            {
                if (IsDoorController(openable))
                {
                    doors.Add(openable);
                    if (!openableControllers.Contains(openable))
                    {
                        openableControllers.Add(openable);
                    }
                }
            }
        }

        // 현재 도어 중 하나라도 열려있으면 닫기, 아니면 열기
        bool anyDoorOpened = false;
        foreach (CarOpenableController door in doors)
        {
            if (door.IsOpened)
            {
                anyDoorOpened = true;
                break;
            }
        }

        bool targetOpened;
        if (doors.Count > 0)
        {
            targetOpened = !anyDoorOpened;
            isDoorsOpened = targetOpened;
        }
        else
        {
            // 3D 컨트롤러 탐색 결과와 무관하게 UI 플래그 반전 보장
            isDoorsOpened = !isDoorsOpened;
            targetOpened = isDoorsOpened;
        }

        // 도어 회전 적용
        foreach (CarOpenableController door in doors)
        {
            door.SetOpened(targetOpened);
        }

        // 하이라이트 표시 갱신
        if (doorActiveHighlight != null)
        {
            doorActiveHighlight.SetActive(targetOpened);
        }

        Debug.Log($"[CarOpenableUIController] 도어 개폐 토글: {(targetOpened ? "열림" : "닫힘")} (연결된 도어: {doors.Count}개)");
    }

    /// <summary>
    /// 트렁크를 토글하고 하이라이트 UI를 갱신합니다.
    /// </summary>
    public void ToggleTrunk()
    {
        ResolveReferences();

        List<CarOpenableController> trunks = new List<CarOpenableController>();
        if (openableControllers != null)
        {
            foreach (CarOpenableController openable in openableControllers)
            {
                if (IsTrunkController(openable))
                {
                    trunks.Add(openable);
                }
            }
        }

        // 트렁크가 없으면 씬에서 즉시 재탐색
        if (trunks.Count == 0)
        {
            CarOpenableController[] found = FindObjectsByType<CarOpenableController>(FindObjectsInactive.Include);
            foreach (CarOpenableController openable in found)
            {
                if (IsTrunkController(openable))
                {
                    trunks.Add(openable);
                    if (!openableControllers.Contains(openable))
                    {
                        openableControllers.Add(openable);
                    }
                }
            }
        }

        // 현재 트렁크가 열려있으면 닫기, 아니면 열기
        bool anyTrunkOpened = false;
        foreach (CarOpenableController trunk in trunks)
        {
            if (trunk.IsOpened)
            {
                anyTrunkOpened = true;
                break;
            }
        }

        bool targetOpened;
        if (trunks.Count > 0)
        {
            targetOpened = !anyTrunkOpened;
            isTrunkOpened = targetOpened;
        }
        else
        {
            // 3D 컨트롤러 탐색 결과와 무관하게 UI 플래그 반전 보장
            isTrunkOpened = !isTrunkOpened;
            targetOpened = isTrunkOpened;
        }

        // 트렁크 회전 적용
        foreach (CarOpenableController trunk in trunks)
        {
            trunk.SetOpened(targetOpened);
        }

        // 하이라이트 표시 갱신
        if (trunkActiveHighlight != null)
        {
            trunkActiveHighlight.SetActive(targetOpened);
        }

        Debug.Log($"[CarOpenableUIController] 트렁크 개폐 토글: {(targetOpened ? "열림" : "닫힘")} (연결된 트렁크: {trunks.Count}개)");
    }
}

