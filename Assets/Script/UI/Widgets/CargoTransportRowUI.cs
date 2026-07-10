using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BOTF3D.Combat;

namespace BOTF3D.UI
{
    /// <summary>
    /// One row per transport ship in CargoDeployMenuUIController.TransportListSlot: shows the ship's
    /// name/icon and loaded/capacity, hosts the CargoDropSlot drop target, and offers an unload button.
    /// </summary>
    public class CargoTransportRowUI : MonoBehaviour
    {
        public TextMeshProUGUI shipNameText;
        public TextMeshProUGUI cargoCountText;
        public Image shipIcon;
        public Button unloadButton;
        public CargoDropSlot dropSlot;

        [Header("Loaded Population Icons")]
        public Image loadedPopIcon1;
        public Image loadedPopIcon2;

        [Header("Loaded Ground Force Icons")]
        public Image loadedGroundForceIcon1;
        public Image loadedGroundForceIcon2;

        private ShipController ship;

        public void Setup(ShipController transport)
        {
            ship = transport;
            if (dropSlot != null)
                dropSlot.OwnerTransport = transport;

            if (shipNameText != null)
                shipNameText.text = transport.ShipData.ShipName;

            if (shipIcon != null && transport.ShipData.ShipSprite != null)
                shipIcon.sprite = transport.ShipData.ShipSprite;

            int loadedPopulation = transport.ShipData.LoadedPopulation;
            int loadedGroundForces = transport.ShipData.LoadedGroundForces;

            if (cargoCountText != null)
                cargoCountText.text = $"{loadedPopulation + loadedGroundForces}/{transport.ShipData.CargoCapacity}";

            // Cargo types are mutually exclusive per transport, so only one icon set is ever showing.
            if (loadedPopIcon1 != null)
                loadedPopIcon1.enabled = loadedPopulation >= 1;
            if (loadedPopIcon2 != null)
                loadedPopIcon2.enabled = loadedPopulation >= 2;

            if (loadedGroundForceIcon1 != null)
                loadedGroundForceIcon1.enabled = loadedGroundForces >= 1;
            if (loadedGroundForceIcon2 != null)
                loadedGroundForceIcon2.enabled = loadedGroundForces >= 2;

            if (unloadButton != null)
            {
                unloadButton.onClick.RemoveAllListeners();
                unloadButton.onClick.AddListener(() =>
                {
                    var controller = CargoDeployMenuUIController.Instance;
                    if (controller == null) return;

                    // Population first so it drains before ground forces, matching the load order icons imply.
                    if (!controller.TryUnloadPopulationFrom(ship))
                        controller.TryUnloadGroundForceFrom(ship);
                });
            }
        }
    }
}
