using BOTF3D.Combat;
using BOTF3D.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BOTF3D.UI
{
    /// <summary>
    /// Sits on the CargoIndicator child of ShipListingTransportUI. Shows which cargo/designation
    /// the transport currently carries: a Colony Kit (Dilithium + population), Troops, a Terraform
    /// designation, or - as a fallback for Dilithium loaded on its own outside the Colony Kit flow -
    /// the plain Dilithium icon. Call Refresh(shipData) whenever cargo state changes.
    ///
    /// Background stays visible at all times (a plain white swatch when nothing is loaded); CargoIconOne
    /// and LoadText layer on top of it together and are hidden as a pair when there's no cargo. LoadText
    /// reads "loaded/capacity" - the real CargoCapacity for Troops (a transport can carry more than one
    /// ground force unit, sharing capacity with Dilithium/Population per ShipData's own comments), or a
    /// fixed "1/1" for Colony Kit/plain Dilithium/Terraform since those are single designations, not a
    /// stackable count.
    /// </summary>
    public class TransportCargoIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject background;
        [SerializeField] private Image cargoIconOne;
        [SerializeField] private TextMeshProUGUI loadText;
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
            // Always visible - the plain white swatch itself IS the "empty" state once
            // CargoIconOne/LoadText are hidden below.
            background.SetActive(true);

            // Order matters: Colony Kit (Dilithium + population together) is checked before the
            // plain-Dilithium fallback. DesignatedForTerraform is checked last since it carries no
            // cargo of its own and would otherwise never lose to an actual loaded cargo type.
            if (shipData.LoadedGroundForces > 0)
            {
                ShowLoad(troopsSprite, shipData.LoadedGroundForces, shipData.CargoCapacity);
            }
            else if (shipData.LoadedDilithium > 0 && shipData.LoadedPopulation > 0)
            {
                ShowLoad(colonySprite, 1, 1);
            }
            else if (shipData.LoadedDilithium > 0)
            {
                ShowLoad(dilithiumSprite, 1, 1);
            }
            else if (shipData.DesignatedForTerraform)
            {
                ShowLoad(terraformSprite, 1, 1);
            }
            else
            {
                cargoIconOne.gameObject.SetActive(false);
                loadText.gameObject.SetActive(false);
            }
        }

        private void ShowLoad(Sprite sprite, int loaded, int capacity)
        {
            cargoIconOne.gameObject.SetActive(true);
            cargoIconOne.sprite = sprite;

            loadText.gameObject.SetActive(true);
            loadText.text = $"{loaded}/{capacity}";
        }
    }
}
