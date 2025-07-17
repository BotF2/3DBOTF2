using UnityEngine;
using Assets.Core;
using System.Collections.Generic;

public class LocalHumanPlayerController : IPlayerController
{
    public PlayerData PlayerData { get; private set; }
    public CivEnum PlayerCiv { get; private set; }
    public bool IsLocal => false;

    public void GiveOrder(Orders order)
    {
        // Handle user UI input logic, reads Unity input.
    }
}
