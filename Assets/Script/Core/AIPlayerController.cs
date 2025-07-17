using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Core;

public class AIPlayerController : IPlayerController
{
    public PlayerData PlayerData { get; private set; } 
    public CivEnum PlayerCiv { get; private set; }
    public bool IsLocal => false;

    public void GiveOrder(Orders order)
    {
        // Handle AI logic, computes behavior logic.
    }
}
