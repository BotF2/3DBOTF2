using BOTF3D.Combat;
using BOTF3D.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Sits on the CargoIndicator child of ShipListingTransportUI. Shows which cargo type
    /// the transport is carrying: a Dilithium crystal, a troops helmet, or nothing.
    /// Call Refresh(shipData) whenever cargo state changes.
    /// </summary>
    public class TransportCargoIndicator : MonoBehaviour
    {
        [SerializeField] private Image cargoIcon;
        [SerializeField] private Sprite dilithiumSprite;
        [SerializeField] private Sprite troopsSprite;

        public void Refresh(ShipData shipData)
        {
            if (shipData == null || shipData.ShipType != ShipType.Transport)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (shipData.LoadedDilithium > 0)
            {
                cargoIcon.enabled = true;
                cargoIcon.sprite = dilithiumSprite;
            }
            else if (shipData.LoadedGroundForces > 0)
            {
                cargoIcon.enabled = true;
                cargoIcon.sprite = troopsSprite;
            }
            else
            {
                cargoIcon.enabled = false;
            }
        }
    }
}
