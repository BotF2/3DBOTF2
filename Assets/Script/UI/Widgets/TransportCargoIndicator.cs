using BOTF3D.Combat;
using BOTF3D.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Sits on the CargoIndicator child of ShipListingTransportUI. Shows which cargo/designation
    /// the transport currently carries: a Colony Kit (Dilithium + population), Troops, a Terraform
    /// designation, or - as a fallback for Dilithium loaded on its own outside the Colony Kit flow -
    /// the plain Dilithium icon. Call Refresh(shipData) whenever cargo state changes.
    /// </summary>
    public class TransportCargoIndicator : MonoBehaviour
    {
        [SerializeField] private Image cargoIcon;
        [SerializeField] private Sprite dilithiumSprite;
        [SerializeField] private Sprite troopsSprite;
        [SerializeField] private Sprite colonySprite;
        [SerializeField] private Sprite terraformSprite;

        public void Refresh(ShipData shipData)
        {
            if (shipData == null || shipData.ShipType != ShipType.Transport)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // Order matters: a Colony Kit is Dilithium + population together, so check it before the
            // plain-Dilithium fallback. DesignatedForTerraform is checked last since it carries no
            // cargo of its own and would otherwise never lose to an actual loaded cargo type.
            if (shipData.LoadedDilithium > 0 && shipData.LoadedPopulation > 0)
            {
                cargoIcon.enabled = true;
                cargoIcon.sprite = colonySprite;
            }
            else if (shipData.LoadedGroundForces > 0)
            {
                cargoIcon.enabled = true;
                cargoIcon.sprite = troopsSprite;
            }
            else if (shipData.LoadedDilithium > 0)
            {
                cargoIcon.enabled = true;
                cargoIcon.sprite = dilithiumSprite;
            }
            else if (shipData.DesignatedForTerraform)
            {
                cargoIcon.enabled = true;
                cargoIcon.sprite = terraformSprite;
            }
            else
            {
                cargoIcon.enabled = false;
            }
        }
    }
}
