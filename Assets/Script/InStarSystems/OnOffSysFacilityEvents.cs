using Assets.Core;
using System;
using UnityEngine;


public class OnOffSysFacilityEvents : MonoBehaviour
{
    public static OnOffSysFacilityEvents current;

    public Action<StarSysController> FactoryButtonOnClicked;
    public Action<StarSysController> FactoryButtonOffClicked;
    public Action<StarSysController> ShipyardButtonOnClicked;
    public Action<StarSysController> ShipyardButtonOffClicked;
    public Action<StarSysController> ShieldButtonOnClicked;
    public Action<StarSysController> ShieldButtonOffClicked;
    public Action<StarSysController> OBButtonOnClicked;
    public Action<StarSysController> OBButtonOffClicked;
    public Action<StarSysController> ResearchButtonOnClicked;
    public Action<StarSysController> ResearchButtonOffClicked;


    private void Awake()
    {
        if (current != null) { Destroy(gameObject); }
        else
        {
            current = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    private void Start()
    {
        FactoryButtonOnClicked += DoFactoryOn;
        FactoryButtonOffClicked += DoFactoryOff;
        ShipyardButtonOnClicked += DoShipyardOn;
    }

    private void DoShipyardOn(StarSysController controller)
    {
        if (ShipyardButtonOnClicked != null)
            ShipyardButtonOnClicked?.Invoke(controller);
    }

    private void DoFactoryOff(StarSysController controller)
    {
        if (FactoryButtonOffClicked != null)
            FactoryButtonOffClicked?.Invoke(controller);
    }

    public void DoFactoryOn(StarSysController sysCon) //, string name)
    {
        if (FactoryButtonOnClicked != null)
        {
            FactoryButtonOnClicked?.Invoke(sysCon); //,name);
        }
    }
}


