using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 차량 커스터마이징 시스템의 조율자.
/// 파츠 설정기(Accessory, Wheel, Color)와 AI 추천 서비스를 중재하고,
/// 파츠 적용 및 견적 계산, UI 통지 이벤트를 총괄합니다.
/// </summary>
[DisallowMultipleComponent]
public class CarConfiguratorManager : MonoBehaviour
{
    [Header("카테고리별 컨피규레이터 컴포넌트")]
    [SerializeField] private CarPartConfigurator accessoryConfigurator;
    [SerializeField] private CarPartConfigurator wheelConfigurator;
    [SerializeField] private ColorConfigurator colorConfigurator;

    [Header("기본 차량 설정")]
    [Tooltip("기본 차량 가격 (트림 단일화에 따른 기본 차체 가격)")]
    [SerializeField] private int baseVehiclePrice = 35000000;

    // AI 파츠 추천 서비스
    private IAiRecommendationService aiService = new GeminiRecommendationService();

    // 씬 내 중복 생성 방지를 위한 내부 가드 (외부 노출 없음)
    private static CarConfiguratorManager activeInstance;

    /// <summary>
    /// 견적 및 선택 파츠 설정이 갱신되었을 때 발생하는 이벤트
    /// </summary>
    public event Action<EstimateData> OnConfigUpdated;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Debug.LogError($"[{nameof(CarConfiguratorManager)}] 씬에 이미 매니저가 존재합니다! 중복 컴포넌트를 파괴합니다: {gameObject.name}");
            Destroy(this);
            return;
        }
        activeInstance = this;
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Start()
    {
        try
        {
            List<CarPart> allParts = CarPartDataLoader.LoadCarParts();
            Initialize(allParts);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[{nameof(CarConfiguratorManager)}] CarParts 데이터 로드 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 파츠 목록을 주입받아 카테고리별 컨피규레이터를 초기화하고 초기 견적을 알립니다.
    /// </summary>
    public void Initialize(List<CarPart> parts)
    {
        if (parts == null) return;

        if (accessoryConfigurator != null)
            accessoryConfigurator.Initialize(parts.Where(p => p.category == PartCategory.Accessory).ToList());

        if (wheelConfigurator != null)
            wheelConfigurator.Initialize(parts.Where(p => p.category == PartCategory.Wheel).ToList());

        if (colorConfigurator != null)
            colorConfigurator.Initialize(parts.Where(p => p.category == PartCategory.Color).ToList());

        NotifyConfigUpdated();
    }

    /// <summary>
    /// 특정 카테고리에서 선택 가능한 전체 CarPart 옵션 목록을 반환합니다.
    /// </summary>
    public List<CarPart> GetOptions(PartCategory category)
    {
        return GetConfigurator(category)?.GetOptions() ?? new List<CarPart>();
    }

    /// <summary>
    /// 파츠 ID(partId)로 단일 파츠를 찾아 해당 카테고리의 컨피규레이터에 적용하고, 견적을 재계산하여 알립니다.
    /// </summary>
    public void ApplyPart(string partId)
    {
        CarPart part = GetAllAvailableParts().FirstOrDefault(p => p.id == partId);
        if (part == null)
        {
            Debug.LogWarning($"[{nameof(CarConfiguratorManager)}] 적용하려는 CarPart를 찾을 수 없습니다. (partId: {partId})");
            return;
        }

        GetConfigurator(part.category)?.SelectOption(part);
        NotifyConfigUpdated();
    }

    /// <summary>
    /// 사용자 프롬프트와 현재 사용 가능한 파츠 목록을 기반으로 AI 추천을 요청합니다.
    /// </summary>
    public async Task<AiRecommendationData> RequestRecommendationAsync(string prompt)
    {
        try
        {
            return await aiService.RequestRecommendationAsync(prompt, GetAllAvailableParts());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{nameof(CarConfiguratorManager)}] AI 추천 요청 실패: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// AI 추천 결과(AiRecommendationData)의 partIds를 순회하며 차량에 일괄 적용합니다.
    /// </summary>
    public void ApplyAiRecommendation(AiRecommendationData data)
    {
        if (data?.partIds == null) return;

        List<CarPart> availableParts = GetAllAvailableParts();
        foreach (string partId in data.partIds)
        {
            CarPart part = availableParts.FirstOrDefault(p => p.id == partId);
            if (part != null)
            {
                GetConfigurator(part.category)?.SelectOption(part);
            }
        }

        NotifyConfigUpdated();
    }

    /// <summary>
    /// 현재 각 컨피규레이터에서 선택된 파츠들을 취합하여 총 견적 및 상세 목록을 계산합니다.
    /// </summary>
    public EstimateData CalculateEstimate()
    {
        var configurators = new CarPartConfigurator[] { accessoryConfigurator, wheelConfigurator, colorConfigurator };
        var selectedParts = configurators.Where(c => c != null)
                                         .Select(c => c.GetSelectedPart())
                                         .Where(p => p != null)
                                         .ToList();

        return new EstimateData
        {
            totalPrice = baseVehiclePrice + selectedParts.Sum(p => p.price),
            details = selectedParts
        };
    }

    /// <summary>
    /// 카테고리에 해당하는 컨피규레이터를 반환합니다.
    /// </summary>
    private CarPartConfigurator GetConfigurator(PartCategory category) => category switch
    {
        PartCategory.Accessory => accessoryConfigurator,
        PartCategory.Wheel => wheelConfigurator,
        PartCategory.Color => colorConfigurator,
        _ => null
    };

    /// <summary>
    /// 전체 컨피규레이터에서 제공하는 모든 CarPart 목록을 수집하여 반환합니다.
    /// </summary>
    private List<CarPart> GetAllAvailableParts()
    {
        var allParts = new List<CarPart>();
        if (accessoryConfigurator != null) allParts.AddRange(accessoryConfigurator.GetOptions());
        if (wheelConfigurator != null) allParts.AddRange(wheelConfigurator.GetOptions());
        if (colorConfigurator != null) allParts.AddRange(colorConfigurator.GetOptions());
        return allParts;
    }

    /// <summary>
    /// 견적을 갱신하고 OnConfigUpdated 이벤트를 통지합니다.
    /// </summary>
    private void NotifyConfigUpdated()
    {
        OnConfigUpdated?.Invoke(CalculateEstimate());
    }
}
