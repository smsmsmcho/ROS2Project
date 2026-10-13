
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 차량 커스터마이징 시스템의 조율자.
/// 액세서리, 휠, 색상 설정과 AI 추천 및 견적 계산을 관리합니다.
/// </summary>
[DisallowMultipleComponent]
public class CarConfiguratorManager : MonoBehaviour
{
    public bool IsInitialized { get; private set; } = false;
    [Header("카테고리별 컨피규레이터")]
    [SerializeField] private CarPartConfigurator accessoryConfigurator;
    [SerializeField] private ColorConfigurator colorConfigurator;
    [SerializeField] private WheelConfigurator wheelModelConfigurator;

    [Header("기본 차량 설정")]
    [SerializeField] private int baseVehiclePrice = 35000000;

    // JSON에서 읽은 휠 옵션
    private List<CarPart> wheelOptions = new List<CarPart>();

    // 현재 선택한 휠
    private CarPart selectedWheel;

    private IAiRecommendationService aiService;

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
            Debug.LogError(
                $"[{nameof(CarConfiguratorManager)}] " +
                $"중복 매니저를 제거합니다: {gameObject.name}"
            );

            Destroy(this);
            return;
        }

        activeInstance = this;
        aiService = new GeminiRecommendationService();
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
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
            Debug.LogWarning(
                $"[{nameof(CarConfiguratorManager)}] " +
                $"CarParts 데이터 로드 실패: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// JSON 데이터를 카테고리별로 초기화합니다.
    /// </summary>
    public void Initialize(List<CarPart> parts)
    {
        if (parts == null)
            return;

        if (accessoryConfigurator != null)
        {
            accessoryConfigurator.Initialize(
                parts.Where(p => p.category == PartCategory.Accessory)
                     .ToList()
            );
        }

        // 휠은 CarPartConfigurator가 아니라 별도 WheelConfigurator가 처리
        wheelOptions = parts
            .Where(p => p.category == PartCategory.Wheel)
            .ToList();

        if (colorConfigurator != null)
        {
            colorConfigurator.Initialize(
                parts.Where(p => p.category == PartCategory.Color)
                     .ToList()
            );
        }

        // 첫 번째 휠을 기본 선택
        if (wheelOptions.Count > 0 && wheelModelConfigurator != null)
        {
            selectedWheel = wheelOptions[0];
            wheelModelConfigurator.SelectTire(
                GetWheelIndex(selectedWheel.id)
            );
        }

        NotifyConfigUpdated();
        IsInitialized = true;
    }

    /// <summary>
    /// 카테고리별 옵션 목록을 반환합니다.
    /// </summary>
    public List<CarPart> GetOptions(PartCategory category)
    {
        switch (category)
        {
            case PartCategory.Accessory:
                return accessoryConfigurator != null
                    ? accessoryConfigurator.GetOptions()
                    : new List<CarPart>();

            case PartCategory.Wheel:
                return wheelOptions;

            case PartCategory.Color:
                return colorConfigurator != null
                    ? colorConfigurator.GetOptions()
                    : new List<CarPart>();

            default:
                return new List<CarPart>();
        }
    }

    /// <summary>
    /// 파츠 ID를 기준으로 해당 파츠를 적용합니다.
    /// </summary>
    public void ApplyPart(string partId)
    {

        CarPart part = GetAllAvailableParts()
            .FirstOrDefault(p => p.id == partId);

        if (part == null)
        {
            Debug.LogWarning(
                $"[{nameof(CarConfiguratorManager)}] " +
                $"파츠를 찾을 수 없습니다: {partId}"
            );
            return;
        }

        if (part.category == PartCategory.Wheel)
        {
            SelectWheelById(part);
        }
        else
        {
            GetConfigurator(part.category)?.SelectOption(part);
        }

        NotifyConfigUpdated();
    }

    /// <summary>
    /// 휠 ID로 모델을 선택합니다.
    /// </summary>
    private void SelectWheelById(CarPart part)
    {
        if (wheelModelConfigurator == null)
        {
            Debug.LogWarning(
                "WheelConfigurator가 연결되지 않았습니다."
            );
            return;
        }

        int index = GetWheelIndex(part.id);

        if (index < 0 || index >= wheelOptions.Count)
        {
            Debug.LogWarning(
                $"휠 ID에 대응하는 모델이 없습니다: {part.id}"
            );
            return;
        }

        wheelModelConfigurator.SelectTire(index);
        selectedWheel = part;

        Debug.Log($"휠 선택: {part.name} ({part.id})");
    }

    /// <summary>
    /// JSON 휠 ID를 모델 인덱스로 변환합니다.
    /// </summary>
    private int GetWheelIndex(string wheelId)
    {
        switch (wheelId)
        {
            case "wheel_std_twin5_spoke":
                return 0;

            case "wheel_split_v5_spoke":
                return 1;

            case "wheel_multi_fin_spoke":
                return 2;

            case "wheel_mesh_y_forged":
                return 3;

            default:
                return -1;
        }
    }

    public async Task<AiRecommendationData> RequestRecommendationAsync(
        string prompt)
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

                if (part.category == PartCategory.Wheel)
                {
                    if (wheelModelConfigurator != null)
                    {
                        SelectWheelById(part);
                        appliedCount++;
                    }
                }
                else
                {
                    CarPartConfigurator configurator = GetConfigurator(part.category);
                    if (configurator != null)
                    {
                        configurator.SelectOption(part);
                        appliedCount++;
                    }
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
    /// 현재 선택한 파츠들의 견적을 계산합니다.
    /// </summary>
    public EstimateData CalculateEstimate()
    {
        var selectedParts = new List<CarPart>();

        if (accessoryConfigurator != null)
        {
            CarPart accessory =
                accessoryConfigurator.GetSelectedPart();

            if (accessory != null)
                selectedParts.Add(accessory);
        }

        if (selectedWheel != null)
            selectedParts.Add(selectedWheel);

        if (colorConfigurator != null)
        {
            CarPart color = colorConfigurator.GetSelectedPart();

            if (color != null)
                selectedParts.Add(color);
        }

        return new EstimateData
        {
            totalPrice =
                baseVehiclePrice + selectedParts.Sum(p => p.price),
            details = selectedParts
        };
    }

    /// <summary>
    /// 기존 파츠 컨피규레이터를 반환합니다.
    /// 휠은 별도 WheelConfigurator가 담당합니다.
    /// </summary>
    private CarPartConfigurator GetConfigurator(
        PartCategory category)
    {
        switch (category)
        {
            case PartCategory.Accessory:
                return accessoryConfigurator;

            case PartCategory.Color:
                return colorConfigurator;

            default:
                return null;
        }
    }

    /// <summary>
    /// 모든 카테고리의 파츠 목록을 반환합니다.
    /// </summary>
    private List<CarPart> GetAllAvailableParts()
    {
        var allParts = new List<CarPart>();

        if (accessoryConfigurator != null)
            allParts.AddRange(accessoryConfigurator.GetOptions());

        allParts.AddRange(wheelOptions);

        if (colorConfigurator != null)
            allParts.AddRange(colorConfigurator.GetOptions());

        return allParts;
    }

    private void NotifyConfigUpdated()
    {
        OnConfigUpdated?.Invoke(CalculateEstimate());
    }

    /// <summary>
    /// UI에서 휠 인덱스로 직접 선택할 때 사용합니다.
    /// </summary>
    public void SelectWheelModel(int index)
    {
        if (index < 0 || index >= wheelOptions.Count)
        {
            Debug.LogWarning(
                $"잘못된 휠 옵션 인덱스입니다: {index}"
            );
            return;
        }

        SelectWheelById(wheelOptions[index]);
        NotifyConfigUpdated();
    }
}
