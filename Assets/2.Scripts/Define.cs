using UnityEngine;

public class Define
{
    // Lane 열거형
    public enum ELane { Zero, One, Two, Length }

    // Lane별 Sorting Layer ID
    public static int[] LaneSortingLayerID = new int[(int)ELane.Length]
    {
        SortingLayer.NameToID("Lane0"),
        SortingLayer.NameToID("Lane1"),
        SortingLayer.NameToID("Lane2")
    };
}
