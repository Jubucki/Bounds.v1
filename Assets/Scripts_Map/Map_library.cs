using System;
using System.Collections.Generic;
using UnityEngine;

public class Map_library : MonoBehaviour
{
    [System.Serializable]
    public struct PlatformData
    {
        public Vector2 position;
        public Vector2 size;
        public float rotation;
        public PlatformType type;
    }

    public enum PlatformType
    {
        Thin,
        Whide,
        Small,
    }

    public class GeneratedMap
    {
        public List<PlatformData> platforms = new List<PlatformData>();
    }

    // ---- Einstellungen (Inspector) ----
    public MovementConfig movementConfig = new MovementConfig();
    public Rect halfBounds = new Rect(-10, -3, 10, 6);
    public int platformsPerHalf = 6;
    public float minDistance = 1.5f;
    public Vector2 mapCenter = new Vector2(0, 0);

    public GameObject thinPrefab;
    public GameObject widePrefab;
    public GameObject smallPrefab;

    private List<GameObject> spawnedPlatforms = new List<GameObject>();

    // ---- Symmetrie (Achsenspiegelung: nur x, y bleibt gleich) ----
    public PlatformData MirrorPlatform(PlatformData original, float axisX)
    {
        PlatformData mirrored = original;
        mirrored.position.x = 2f * axisX - original.position.x;
        return mirrored;
    }

    public GeneratedMap BuildSymmetricMap(List<PlatformData> halfMap, float axisX)
    {
        GeneratedMap fullMap = new GeneratedMap();

        foreach (PlatformData platform in halfMap)
        {
            fullMap.platforms.Add(platform);
            fullMap.platforms.Add(MirrorPlatform(platform, axisX));
        }

        return fullMap;
    }

    // ---- Zufalls-Platzierung ----
    public List<PlatformData> GenerateHalfMap(int platformCount, Rect bounds, float minDistance, PlatformType[] possibleTypes)
    {
        List<PlatformData> result = new List<PlatformData>();
        int maxAttempts = 50;

        for (int i = 0; i < platformCount; i++)
        {
            bool placed = false;
            int attempts = 0;

            while (!placed && attempts < maxAttempts)
            {
                attempts++;

                Vector2 candidatePos = new Vector2(
                    UnityEngine.Random.Range(bounds.xMin, bounds.xMax),
                    UnityEngine.Random.Range(bounds.yMin, bounds.yMax)
                );

                if (IsFarEnough(candidatePos, result, minDistance))
                {
                    PlatformType chosenType = possibleTypes[UnityEngine.Random.Range(0, possibleTypes.Length)];

                    PlatformData newPlatform = new PlatformData
                    {
                        position = candidatePos,
                        size = GetSizeForType(chosenType),
                        type = chosenType
                    };

                    result.Add(newPlatform);
                    placed = true;
                }
            }
        }

        return result;
    }

    private bool IsFarEnough(Vector2 candidate, List<PlatformData> existing, float minDistance)
    {
        foreach (PlatformData platform in existing)
        {
            if (Vector2.Distance(candidate, platform.position) < minDistance)
            {
                return false;
            }
        }
        return true;
    }

    private Vector2 GetSizeForType(PlatformType type)
    {
        switch (type)
        {
            case PlatformType.Thin: return new Vector2(0.4f, 3f);
            case PlatformType.Whide: return new Vector2(4f, 0.5f);
            case PlatformType.Small: return new Vector2(1f, 0.4f);
            default: return new Vector2(2f, 0.5f);
        }
    }

    // ---- Erreichbarkeits-Validierung ----
    public bool CanReach(PlatformData from, PlatformData to, MovementConfig config)
    {
        float centerDx = Mathf.Abs(to.position.x - from.position.x);
        float dx = Mathf.Max(0f, centerDx - (from.size.x + to.size.x) * 0.5f);

        float maxDistance = config.jumpDistance + config.dashDistance;
        float maxHeight = config.jumpHeight + config.boostHeight;

        if (dx > maxDistance)
            return false;
        if (centerDx > maxHeight)
            return false;
        return true;
    }
    
    public float groundGap = 2f;
    public float groundThickness = 0.6f;

    private void AddGroundPlatforms(GeneratedMap map)
    {
        float fullWidth = halfBounds.width * 2f;
        float plateWidth = (fullWidth - 2f * groundGap) / 3f;
        float step = plateWidth + groundGap;
        float y = halfBounds.yMin + groundThickness * 0.5f;

        for (int i = -1; i <= 1; i++)
        {
            PlatformData ground = new PlatformData
            {
                position = new Vector2(mapCenter.x + i * step, y),
                size = new Vector2(plateWidth, groundThickness),
                type = PlatformType.Whide
            };

            map.platforms.Add(ground);
        }
    }

    public Dictionary<int, List<int>> BuildAdjacency(List<PlatformData> platforms, MovementConfig config)
    {
        Dictionary<int, List<int>> graph = new Dictionary<int, List<int>>();

        for (int i = 0; i < platforms.Count; i++)
        {
            graph[i] = new List<int>();

            for (int j = 0; j < platforms.Count; j++)
            {
                if (i == j) continue;

                if (CanReach(platforms[i], platforms[j], config))
                {
                    graph[i].Add(j);
                }
            }
        }

        return graph;
    }

    public bool IsMapFullyConnected(Dictionary<int, List<int>> graph, int startIndex)
    {
        HashSet<int> visited = new HashSet<int>();
        Queue<int> toVisit = new Queue<int>();

        toVisit.Enqueue(startIndex);
        visited.Add(startIndex);

        while (toVisit.Count > 0)
        {
            int current = toVisit.Dequeue();

            foreach (int neighbor in graph[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    toVisit.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == graph.Count;
    }

    // ---- Map generieren + validieren ----
    private GeneratedMap GenerateValidMap()
    {
        int maxMapAttempts = 20;

        for (int attempt = 0; attempt < maxMapAttempts; attempt++)
        {
            PlatformType[] types = { PlatformType.Thin, PlatformType.Whide, PlatformType.Small };
            List<PlatformData> half = GenerateHalfMap(platformsPerHalf, halfBounds, minDistance, types);

            GeneratedMap candidate = BuildSymmetricMap(half, mapCenter.x);
            AddGroundPlatforms(candidate);
            Dictionary<int, List<int>> graph = BuildAdjacency(candidate.platforms, movementConfig);

            if (IsMapFullyConnected(graph, 0))
            {
                Debug.Log("Gültige Map nach " + (attempt + 1) + " Versuch(en) gefunden.");
                return candidate;
            }
        }

        Debug.LogWarning("Keine gültige Map gefunden.");
        return null;
    }

    // ---- Instanziierung ----
    private void InstantiateMap(GeneratedMap map)
    {
        ClearSpawnedPlatforms();

        foreach (PlatformData platform in map.platforms)
        {
            GameObject prefab = GetPrefabForType(platform.type);
            GameObject instance = Instantiate(prefab, platform.position, Quaternion.identity);
            instance.transform.localScale = new Vector3(platform.size.x, platform.size.y, 1f);
            spawnedPlatforms.Add(instance);
        }
    }

    private GameObject GetPrefabForType(PlatformType type)
    {
        switch (type)
        {
            case PlatformType.Thin: return thinPrefab;
            case PlatformType.Whide: return widePrefab;
            case PlatformType.Small: return smallPrefab;
            default: return thinPrefab;
        }
    }

    private void ClearSpawnedPlatforms()
    {
        foreach (GameObject obj in spawnedPlatforms)
        {
            Destroy(obj);
        }
        spawnedPlatforms.Clear();
    }

    void Start()
    {
        GeneratedMap finalMap = GenerateValidMap();

        if (finalMap == null)
        {
            Debug.LogError("Map-Generierung fehlgeschlagen.");
            return;
        }

        InstantiateMap(finalMap);
        Debug.Log("Fertige Map mit " + finalMap.platforms.Count + " Plattformen generiert.");
    }

    void Update()
    {
    }
}