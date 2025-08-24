using Assets.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CivRelationsManager : MonoBehaviour
{
    public static CivRelationsManager Instance;
    public Dictionary<(CivEnum, CivEnum), CivRelationsData> RelationsDictionary = new Dictionary<(CivEnum, CivEnum), CivRelationsData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public CivRelationsData GetOrCreateRelationsData(CivEnum civA, CivEnum civB, FleetController fleetA, FleetController fleetB, StarSysController sysController)
    {
        var key = (civA, civB);
        if (!RelationsDictionary.TryGetValue(key, out var relationsData))
        {
            relationsData = new CivRelationsData(civA, civB, fleetA, fleetB, sysController);
            RelationsDictionary[key] = relationsData;
        }
        return relationsData;
    }
    public CivRelationsData GetRelationsData(CivEnum civA, CivEnum civB)
    {
        var key = (civA, civB);
        RelationsDictionary.TryGetValue(key, out var relationsData);
        return relationsData;
    }

    internal void UpdateCivRelationsIntelData(IntelligenceData intelligenceData)
    {
        var relationsData = GetRelationsData(intelligenceData.CivSideOne, intelligenceData.CivSideTwo);
        if (relationsData != null)
        {
            relationsData.IntelData = intelligenceData;
        }
    }
}
