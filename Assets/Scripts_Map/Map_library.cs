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

    public GeneratedMap BuildSymmetricMap(List<PlatformData> halfMap, Vector 2 center)
    {
        GeneratedMap fullMap = new GeneratedMap();

        foreach (PlatformData platform in halfMap)
        {
            fullMap.platforms.Add(platform);
            fullMap.platforms.Add(MirrorPlatform(platform,center));

        }

        return fullMap;
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
