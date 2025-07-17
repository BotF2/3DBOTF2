using UnityEngine;
using Assets.Core;
using System.Collections.Generic;

public interface IPlayerController
{
    PlayerData PlayerData { get; } 
    void GiveOrder(Orders order);
    CivEnum PlayerCiv { get; }
    bool IsLocal { get; }
}
