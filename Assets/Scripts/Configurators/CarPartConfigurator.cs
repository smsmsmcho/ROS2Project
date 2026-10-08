using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특정 카테고리의 파츠 옵션들을 관리하고,
/// 선택된 파츠에 따라 실제 3D 게임오브젝트를 활성/비활성화하는 설정 컴포넌트
/// </summary>
public class CarPartConfigurator : MonoBehaviour
{
    [Header("카테고리 설정")]
    [SerializeField] private PartCategory category;

    // 해당 카테고리에서 선택 가능한 파츠 데이터 목록
    private List<CarPart> options = new List<CarPart>();

    [Header("옵션 데이터 및 3D 오브젝트 매핑")]
    [Tooltip("options 목록과 1:1로 매핑되는 3D 모델 GameObject 목록 (인덱스 일치)")]
    [SerializeField] private List<GameObject> optionObjects = new List<GameObject>();

    // 현재 선택된 파츠
    private CarPart selectedPart;

    public void Initialize(List<CarPart> partOptions)
    {
        this.options = partOptions ?? new List<CarPart>();
        if (options.Count > 0)
        {
            SelectOption(options[0]); // 기본 첫 번째 옵션 적용
        }
    }

    /// <summary>
    /// 이 컨피규레이터에서 제공하는 전체 CarPart 옵션 목록을 반환합니다.
    /// </summary>
    public List<CarPart> GetOptions()
    {
        return options;
    }

    /// <summary>
    /// 특정 파츠를 선택하고 차량에 적용합니다.
    /// </summary>
    public void SelectOption(CarPart part)
    {
        if (part == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 선택하려는 CarPart가 null입니다.");
            return;
        }

        selectedPart = part;
        ApplyPart(part);
    }

    /// <summary>
    /// 현재 선택되어 적용 중인 CarPart를 반환합니다.
    /// </summary>
    public CarPart GetSelectedPart()
    {
        return selectedPart;
    }

    /// <summary>
    /// 선택된 파츠를 실제 차량 3D 모델에 적용합니다.
    /// 기본 구현은 1:1 매핑된 GameObject 활성/비활성화이며,
    /// ColorConfigurator 등 하위 클래스에서 머티리얼 변경 등으로 오버라이드할 수 있습니다.
    /// </summary>
    protected virtual void ApplyPart(CarPart part)
    {
        if (options == null || optionObjects == null) return;

        // 선택된 파츠의 인덱스 찾기 (ID 기준 또는 참조 기준)
        int selectedIndex = options.FindIndex(p => p.id == part.id);
        if (selectedIndex == -1)
        {
            selectedIndex = options.IndexOf(part);
        }

        if (selectedIndex == -1)
        {
            Debug.LogWarning($"[{gameObject.name}] 선택된 파츠 '{part.name}({part.id})'가 options 목록에 존재하지 않습니다.");
            return;
        }

        // 인덱스가 일치하는 GameObject만 활성화하고 나머지는 비활성화
        for (int i = 0; i < optionObjects.Count; i++)
        {
            if (optionObjects[i] != null)
            {
                bool isActive = (i == selectedIndex);
                optionObjects[i].SetActive(isActive);
            }
        }

        Debug.Log($"[{category}] 파츠 적용 완료: {part.name} ({part.price:N0}원)");
    }
}

