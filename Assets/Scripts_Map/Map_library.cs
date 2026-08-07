using System;
using UnityEngine;
using System.Collections.Generic;

public class Map_library : MonoBehaviour
{
    [System.Serializable]
    public struct PlatformData
    {
        public Vector2 position;    // Mittelpunkt der Plattform in der Welt
        public Vector2 size;        // Breite/Höhe
        public float rotation;      //
        public PlatformType type;   // welcher Prefab/welches Verhalten
    }

    public enum PlatformType
    {
        Thin,       // schmaler vertikaler Balken (wie in eurer Skizze)
        Whide,      // breiter horizontaler Balken
        Small,      // kleine Plattform/Item
    }
    
    public class GeneratedMap
    {
        public List<PlatformData> platforms = new List<PlatformData>();
    }

    public PlatformData MirrorPlatform(PlatformData original, Vector2 center)
    {
        PlatformData mirrored = original;
        mirrored.position = (2f * center) - original.position;
        return mirrored;
    }

    public GeneratedMap BuildSymmetricMap(List<PlatformData> halfMap, Vector2 center)
    {
        GeneratedMap fullMap = new GeneratedMap();

        foreach (PlatformData platform in halfMap)
        {
            fullMap.platforms.Add(platform);
            fullMap.platforms.Add(MirrorPlatform(platform,center));

        }

        return fullMap;
    }

    public List<PlatformData> GenerateHalfMap (int platformCount, Rect bounds, float minDistance, PlatformType[] possibleTypes )
    {
        List<PlatformData> result = new List<PlatformData>();
        int maxAttempts = 50;

        for (int i = 0; i < platformCount; i++)
        {
            bool placed =false;
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
                    PlatformData newPlatform = new PlatformData
                    {
                        position = candidatePos,
                        size = new Vector2(2f, 0.5f),
                        type = possibleTypes[UnityEngine.Random.Range(0, possibleTypes.Length)]
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
            if(Vector2.Distance(candidate, platform.position) < minDistance)
            {
                return false;
            }
        }
        return true;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
