using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    private IAiRecommendationService aiService;

    // 씬 내 중복 생성 방지를 위한 내부 가드 (외부 노출 없음)
    private static CarConfiguratorManager activeInstance;

    /// <summary>
    /// 견적 및 선택 파츠 설정이 갱신되었을 때 발생하는 이벤트
    /// </summary>
    public event Action<EstimateData> OnConfigUpdated;

    /// <summary>
    /// AI 추천 요청 실패 또는 추천 파츠 적용 실패 시 UI에 안내 메시지를 전달하는 이벤트
    /// </summary>
    public event Action<string> OnRecommendationFailed;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Debug.LogError($"[{nameof(CarConfiguratorManager)}] 씬에 이미 매니저가 존재합니다! 중복 컴포넌트를 파괴합니다: {gameObject.name}");
            Destroy(this);
            return;
        }
        activeInstance = this;
        aiService = new GeminiRecommendationService();
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
    /// 실패 시 예외 종류(API 키 누락, 타임아웃, 503/429 서버 과부하, 네트워크 오류 등)를 구분하여
    /// OnRecommendationFailed 이벤트로 UI에 안내 메시지를 전달합니다.
    /// </summary>
    public async Task<AiRecommendationData> RequestRecommendationAsync(string prompt)
    {
        try
        {
            return await aiService.RequestRecommendationAsync(prompt, GetAllAvailableParts());
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogError($"[{nameof(CarConfiguratorManager)}] AI 추천 요청 실패 (API 키 오류): {ex.Message}");
            OnRecommendationFailed?.Invoke("API 키가 설정되지 않았습니다. 설정 파일(config.json)을 확인해주세요.");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Debug.LogError($"[{nameof(CarConfiguratorManager)}] AI 추천 요청 실패 (응답 시간 초과): {ex.Message}");
            OnRecommendationFailed?.Invoke("AI 서버 응답 시간이 초과되었습니다. 잠시 후 다시 시도해주세요.");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Debug.LogError($"[{nameof(CarConfiguratorManager)}] AI 추천 요청 실패 (통신 오류): {ex.Message}");
            string userMessage = (ex.Message.Contains("503") || ex.Message.Contains("429"))
                ? "현재 AI 서버가 혼잡합니다. 잠시 후 다시 시도해주세요."
                : "네트워크 통신 오류가 발생했습니다. 인터넷 연결을 확인해주세요.";
            OnRecommendationFailed?.Invoke(userMessage);
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{nameof(CarConfiguratorManager)}] AI 추천 요청 실패: {ex.Message}");
            OnRecommendationFailed?.Invoke("AI 추천을 처리하는 중 오류가 발생했습니다.");
            return null;
        }
    }

    /// <summary>
    /// AI 추천 결과(AiRecommendationData)의 partIds를 순회하며 차량에 일괄 적용합니다.
    /// 동일 카테고리가 중복 수신될 경우 경고를 남기고 덮어쓰기를 방지하며,
    /// 실제로 적용된 파츠가 0개이면 "추천을 적용하지 못했습니다." 경고 및 이벤트를 발생시킵니다.
    /// </summary>
    public void ApplyAiRecommendation(AiRecommendationData data)
    {
        int appliedCount = 0;

        if (data?.partIds != null)
        {
            List<CarPart> availableParts = GetAllAvailableParts();
            var appliedCategories = new HashSet<PartCategory>();

            foreach (string partId in data.partIds)
            {
                CarPart part = availableParts.FirstOrDefault(p => p.id == partId);
                if (part == null)
                {
                    Debug.LogWarning($"[{nameof(CarConfiguratorManager)}] 추천된 파츠 ID '{partId}'를 찾을 수 없어 건너뜁니다.");
                    continue;
                }

                if (!appliedCategories.Add(part.category))
                {
                    Debug.LogWarning($"[{nameof(CarConfiguratorManager)}] '{part.category}' 카테고리에 중복 파츠('{part.id}')가 감지되어 덮어쓰기를 건너뜁니다.");
                    continue;
                }

                CarPartConfigurator configurator = GetConfigurator(part.category);
                if (configurator != null)
                {
                    configurator.SelectOption(part);
                    appliedCount++;
                }
            }
        }

        if (appliedCount == 0)
        {
            Debug.LogWarning($"[{nameof(CarConfiguratorManager)}] 추천을 적용하지 못했습니다.");
            OnRecommendationFailed?.Invoke("추천을 적용하지 못했습니다.");
            return;
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
