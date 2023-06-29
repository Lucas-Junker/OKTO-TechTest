using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Slide Data Settings", menuName = "OKTO Tech Test/Slide Data Settings")]
public class SlideDataSettings : ScriptableObject
{
    public List<SlideData> SlideDataList;
}

[Serializable]
public class SlideData
{
    public GameObject Dancer;
    public string Dance;
    public Texture2D Background;
}
