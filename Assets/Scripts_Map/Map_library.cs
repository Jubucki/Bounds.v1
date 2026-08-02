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
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
