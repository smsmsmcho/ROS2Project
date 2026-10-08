using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// CarParts.json 파일을 로드하여 CarPart 리스트로 변환하는 유틸리티
/// 외부 라이브러리 없이 Unity 내장 JsonUtility로 안전하게 역직렬화합니다.
/// </summary>
public static class CarPartDataLoader
{
    [Serializable]
    private class RawCarPart
    {
        public string id;
        public string name;
        public int price;
        public string category;
        public string description;
        public string colorHex;
    }

    [Serializable]
    private class RawCarPartListWrapper
    {
        public RawCarPart[] items;
    }

    public static List<CarPart> LoadCarParts(string customPath = null)
    {
        string filePath = customPath;

        if (string.IsNullOrEmpty(filePath))
        {
            string[] searchPaths = new[]
            {
                Path.Combine(Application.dataPath, "Data", "CarParts.json"),
                Path.Combine(Application.dataPath, "Data", "CarParts.json.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "CarParts.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "CarParts.json.json")
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    filePath = path;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            throw new FileNotFoundException($"CarParts.json 파일을 찾을 수 없습니다. 경로를 확인하세요.");
        }

        string jsonContent = File.ReadAllText(filePath).Trim();

        string wrappedJson = $"{{\"items\":{jsonContent}}}";
        RawCarPartListWrapper wrapper = JsonUtility.FromJson<RawCarPartListWrapper>(wrappedJson);

        List<CarPart> parts = new List<CarPart>();
        if (wrapper != null && wrapper.items != null)
        {
            foreach (var raw in wrapper.items)
            {
                PartCategory cat = PartCategory.Trim;
                if (!string.IsNullOrEmpty(raw.category))
                {
                    Enum.TryParse(raw.category, true, out cat);
                }

                parts.Add(new CarPart
                {
                    id = raw.id,
                    name = raw.name,
                    price = raw.price,
                    category = cat,
                    description = raw.description,
                    colorHex = raw.colorHex
                });
            }
        }

        return parts;
    }
}
