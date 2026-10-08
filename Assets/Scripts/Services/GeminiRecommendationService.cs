using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Google Gemini API를 활용한 실제 AI 차량 파츠 추천 서비스 구현체
/// Gemini API의 'responseSchema (Structured Outputs)'를 사용하여
/// 응답이 무조건 AiRecommendationData 규격으로만 생성되도록 서버 레벨에서 강제 제어합니다.
/// </summary>
public class GeminiRecommendationService : IAiRecommendationService
{
    private static readonly HttpClient HttpClient = new HttpClient();

    // 민감한 보안 정보이므로 외부에 프로퍼티로 노출하지 않고 내부에서만 캡슐화
    private readonly string apiKey;

    // gemini-flash-latest는 현재 3.8로 연결되어 503(과부하)이 빈번하므로, 서버가 가장 안정적인 3.5-flash 적용
    private string modelName = "gemini-3.5-flash";

    private string Endpoint => $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

    public GeminiRecommendationService()
    {
        this.apiKey = LoadApiKeyFromConfig();
    }

    /// <summary>
    /// 사용자 프롬프트와 파츠 목록을 기반으로 Gemini API에 구조화된 JSON 추천을 요청합니다.
    /// </summary>
    public async Task<AiRecommendationData> RequestRecommendationAsync(string prompt, List<CarPart> availableParts)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Gemini API Key가 설정되지 않았습니다. Assets/Config/config.json을 확인하세요.");
        }

        if (availableParts == null || availableParts.Count == 0)
        {
            throw new ArgumentException("추천 대상 파츠 목록이 비어있습니다.", nameof(availableParts));
        }

        string payload = BuildStructuredRequestPayload(prompt, availableParts);

        using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
        {
            HttpResponseMessage response = await HttpClient.PostAsync(Endpoint, content);
            string rawResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Gemini API 호출 실패 (HTTP {(int)response.StatusCode}):\n{rawResponse}");
            }

            return ParseStructuredResponse(rawResponse, availableParts);
        }
    }

    /// <summary>
    /// responseSchema를 포함하여 AiRecommendationData 규격을 100% 강제하는 요청 JSON 본문을 생성합니다.
    /// </summary>
    private string BuildStructuredRequestPayload(string userPrompt, List<CarPart> availableParts)
    {
        var partsTextBuilder = new StringBuilder();
        foreach (var part in availableParts)
        {
            partsTextBuilder.AppendLine($"- ID: {part.id} | [{part.category}] {part.name} ({part.price:N0}원) : {part.description}");
        }

        string systemInstruction =
            "당신은 전문 자동차 스타일링 및 커스텀 튜닝 컨설턴트 AI입니다.\n" +
            "사용자의 요구사항을 분석하여 제공된 파츠 목록 중에서 최적의 조합(Trim 1개, Wheel 1개, Color 1개)을 추천하세요.\n" +
            "규칙: 반드시 제공된 파츠 목록에 존재하는 ID만 partIds에 포함해야 합니다.";

        string userContent = $"[선택 가능한 파츠 목록]\n{partsTextBuilder}\n\n[사용자 요청]\n{userPrompt}";
        string combinedPrompt = $"{systemInstruction}\n\n{userContent}";

        // 단순 문자열을 완벽한 JSON 문자열 리터럴로 직렬화 ("..." 형태)
        string jsonEncodedPrompt = ToJsonStringLiteral(combinedPrompt);

        // responseSchema(Structured Outputs)를 통해 AiRecommendationData 구조를 서버 레벨에서 강제
        string requestJson = $@"{{
  ""contents"": [
    {{
      ""parts"": [
        {{ ""text"": {jsonEncodedPrompt} }}
      ]
    }}
  ],
  ""generationConfig"": {{
    ""temperature"": 0.7,
    ""responseMimeType"": ""application/json"",
    ""responseSchema"": {{
      ""type"": ""OBJECT"",
      ""properties"": {{
        ""conceptTitle"": {{ ""type"": ""STRING"" }},
        ""reasoning"": {{ ""type"": ""STRING"" }},
        ""partIds"": {{
          ""type"": ""ARRAY"",
          ""items"": {{ ""type"": ""STRING"" }}
        }}
      }},
      ""required"": [""conceptTitle"", ""reasoning"", ""partIds""]
    }}
  }}
}}";

        return requestJson;
    }

    /// <summary>
    /// Gemini 응답에서 AiRecommendationData를 안전하게 추출합니다.
    /// </summary>
    private AiRecommendationData ParseStructuredResponse(string rawResponse, List<CarPart> availableParts)
    {
        GeminiApiResponse res = JsonUtility.FromJson<GeminiApiResponse>(rawResponse);
        if (res?.candidates == null || res.candidates.Length == 0)
        {
            throw new Exception("Gemini 응답에 candidates가 없습니다: " + rawResponse);
        }

        string text = res.candidates[0]?.content?.parts?[0]?.text;
        if (string.IsNullOrEmpty(text))
        {
            throw new Exception("Gemini 응답에 본문 텍스트가 없습니다: " + rawResponse);
        }

        AiRecommendationData data = JsonUtility.FromJson<AiRecommendationData>(text.Trim());
        if (data == null)
        {
            throw new Exception("AiRecommendationData 역직렬화 실패: " + text);
        }

        // 혹시 모를 존재하지 않는 ID 필터링 (데이터 정합성 보장)
        var validIds = new HashSet<string>(availableParts.Select(p => p.id));
        if (data.partIds != null)
        {
            data.partIds = data.partIds.Where(id => validIds.Contains(id)).ToList();
        }
        else
        {
            data.partIds = new List<string>();
        }

        return data;
    }

    /// <summary>
    /// C# 문자열을 따옴표를 포함한 표준 JSON 문자열 리터럴(예: "내용...")로 인코딩합니다.
    /// </summary>
    private static string ToJsonStringLiteral(string value)
    {
        if (value == null) return "\"\"";

        var sb = new StringBuilder(value.Length + 16);
        sb.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32)
                    {
                        sb.AppendFormat("\\u{0:X04}", (int)c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    private static string LoadApiKeyFromConfig()
    {
        string[] searchPaths = new[]
        {
            Path.Combine(Application.dataPath, "Config", "config.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Config", "config.json")
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    ConfigData cfg = JsonUtility.FromJson<ConfigData>(json);
                    if (!string.IsNullOrEmpty(cfg?.apiKey))
                    {
                        return cfg.apiKey;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GeminiRecommendationService] config.json 읽기 실패: {ex.Message}");
                }
            }
        }

        return Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    #region Response DTOs
    // =========================================================================================
    // [설명: 왜 이 DTO 클래스들이 계층별로 필요한가?]
    //
    // 1. Unity 내장 JsonUtility의 특성:
    //    타 언어(Python, JS)의 Dictionary나 동적 객체(dynamic) 역직렬화를 지원하지 않으며,
    //    오직 [Serializable]이 선언된 정적 C# 클래스 구조만 파싱할 수 있습니다.
    //
    // 2. 외부 패키지(Newtonsoft.Json 등) 의존성 완전 배제:
    //    서드파티 패키지 설치 없이 Unity 순수 기본 기능만으로 크로스 플랫폼에서 안정적으로
    //    초고속 역직렬화(Zero GC / C++ 바인딩)를 수행하기 위해 사용합니다.
    //
    // 3. 구글 Gemini API 응답 JSON과의 1:1 매핑 계층:
    //    구글 API 응답 구조:
    //    {
    //      "candidates": [             <-- [1단계] GeminiApiResponse
    //        {
    //          "content": {            <-- [2단계] Candidate
    //            "parts": [            <-- [3단계] ResponseContent
    //              {
    //                "text": "..."     <-- [4단계] ResponsePart (★ 우리가 필요한 실제 추천 JSON 알맹이!)
    //              }
    //            ]
    //          }
    //        }
    //      ]
    //    }
    //
    //    -> 이 DTO들을 통해 봉투를 벗겨내고 `res.candidates[0].content.parts[0].text`로
    //       최종 추천 JSON 문자열을 안전하게 꺼낸 뒤, 최종 목적지인 [AiRecommendationData]로 변환합니다.
    // =========================================================================================

    /// <summary>
    /// Assets/Config/config.json의 apiKey 필드를 읽기 위한 1회용 설정 DTO
    /// </summary>
    [Serializable]
    private class ConfigData
    {
        public string apiKey;
    }

    /// <summary>
    /// [1단계] 구글 Gemini API 최상위 응답 루트 (candidates 배열 수신)
    /// </summary>
    [Serializable]
    private class GeminiApiResponse
    {
        public Candidate[] candidates;
    }

    /// <summary>
    /// [2단계] 생성된 응답 후보(Candidate) 단일 항목
    /// </summary>
    [Serializable]
    private class Candidate
    {
        public ResponseContent content;
    }

    /// <summary>
    /// [3단계] 후보 내부의 실제 컨텐츠 객체 (parts 배열 수신)
    /// </summary>
    [Serializable]
    private class ResponseContent
    {
        public ResponsePart[] parts;
    }

    /// <summary>
    /// [4단계] 컨텐츠 파트 중 실제 AI 텍스트 결과(text)를 품고 있는 최종 조각
    /// </summary>
    [Serializable]
    private class ResponsePart
    {
        public string text;
    }
    #endregion
}
