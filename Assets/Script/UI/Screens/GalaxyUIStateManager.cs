using BOTF3D.Core;
using UnityEngine;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    /// <summary>
    /// Manages galaxy menu state, transitions, and visibility.
    /// Handles opening/closing menus, tracking current menu state.
    /// </summary>
    public class GalaxyUIStateManager : IManager
    {
        public void Initialize() {}
        public void Cleanup() {}
        // Current menu state
        public Menu CurrentOpenMenu { get; private set; } = Menu.None;
        public GameObject CurrentOpenMenuObject { get; private set; }
        public GalaxyClickMode CurrentClickMode { get; set; } = GalaxyClickMode.Normal;

        // Menu GameObjects
        private readonly GameObject sysBuildMenu;
        private readonly GameObject intelMenuView;
        private readonly GameObject encyclopediaMenuView;
        private readonly GameObject techTreeMenuView;
        private readonly GameObject habitableSysMenu;
        private readonly GameObject diplomacyNoContacts;

        // Background GameObjects
        private readonly GameObject sysBackground;
        private readonly GameObject fleetsBackground;
        private readonly GameObject diplomacyBackground;
        private readonly GameObject intelBackground;
        private readonly GameObject encyclopediaBackground;
        private readonly GameObject techTreeBackground;

        // UI elements
        private readonly GameObject closeMenuButton;
        private readonly GameObject selectOtherSysOrFleetButtonGO;

        public GalaxyUIStateManager(
            GameObject sysBuildMenu,
            GameObject intelMenuView,
            GameObject encyclopediaMenuView,
            GameObject habitableSysMenu,
            GameObject diplomacyNoContacts,
            GameObject sysBackground,
            GameObject fleetsBackground,
            GameObject diplomacyBackground,
            GameObject intelBackground,
            GameObject encyclopediaBackground,
            GameObject closeMenuButton,
            GameObject selectOtherSysOrFleetButtonGO,
            GameObject techTreeMenuView = null,
            GameObject techTreeBackground = null)
        {
            this.sysBuildMenu = sysBuildMenu;
            this.intelMenuView = intelMenuView;
            this.encyclopediaMenuView = encyclopediaMenuView;
            this.habitableSysMenu = habitableSysMenu;
            this.diplomacyNoContacts = diplomacyNoContacts;
            this.sysBackground = sysBackground;
            this.fleetsBackground = fleetsBackground;
            this.diplomacyBackground = diplomacyBackground;
            this.intelBackground = intelBackground;
            this.encyclopediaBackground = encyclopediaBackground;
            this.closeMenuButton = closeMenuButton;
            this.selectOtherSysOrFleetButtonGO = selectOtherSysOrFleetButtonGO;
            this.techTreeMenuView = techTreeMenuView;
            this.techTreeBackground = techTreeBackground;
        }

        /// <summary>
        /// Initialize all menus to their default state
        /// </summary>
        public void InitializeMenuStates()
        {
            Debug.Log("GalaxyUIStateManager: Initializing menu states");

            if (intelMenuView != null) intelMenuView.SetActive(false);
            if (encyclopediaMenuView != null) encyclopediaMenuView.SetActive(false);
            if (techTreeMenuView != null) techTreeMenuView.SetActive(false);
            if (closeMenuButton != null) closeMenuButton.SetActive(true);
            if (sysBackground != null) sysBackground.SetActive(false);
            if (fleetsBackground != null) fleetsBackground.SetActive(false);
            if (diplomacyBackground != null) diplomacyBackground.SetActive(false);
            if (intelBackground != null) intelBackground.SetActive(false);
            if (encyclopediaBackground != null) encyclopediaBackground.SetActive(false);
            if (techTreeBackground != null) techTreeBackground.SetActive(false);
            if (habitableSysMenu != null) habitableSysMenu.SetActive(false);
        }

        /// <summary>
        /// Open a specific menu
        /// </summary>
        public void OpenMenu(Menu menuEnum, GameObject menuObject)
        {
            Debug.Log($"GalaxyUIStateManager: Opening menu {menuEnum}");

            CurrentOpenMenu = menuEnum;
            CurrentOpenMenuObject = menuObject;

            // Activate appropriate background
            ActivateBackgroundForMenu(menuEnum);

            // Show menu object
            if (menuObject != null)
            {
                menuObject.SetActive(true);
            }
        }

        /// <summary>
        /// Close the currently open menu
        /// </summary>
        public void CloseCurrentMenu()
        {
            Debug.Log($"GalaxyUIStateManager: Closing menu {CurrentOpenMenu}");

            // A DiplomacyController's own GameObject is never deactivated here - unlike every other
            // tracked menu object, it isn't a disposable UI panel; it's the persistent per-civ-pair
            // relationship tracker. Resolution is instant and action-driven now (see
            // DiplomacyController.ServerForceOtherSideIfStillUndecided / ServerImplicitlyWithdrawFleet),
            // not a per-frame Update() loop, but deactivating it here would still needlessly tear
            // down/respawn the instance the moment a second, unrelated first-contact encounter opened
            // its own panel on top (OpenMenu always calls CloseCurrentMenu before opening anything
            // new), so it stays excluded.
            if (CurrentOpenMenuObject != null && CurrentOpenMenuObject.GetComponent<DiplomacyController>() == null)
            {
                CurrentOpenMenuObject.SetActive(false);
            }

            CloseAllBackgrounds();
            CurrentOpenMenu = Menu.None;
            CurrentOpenMenuObject = null;
        }

        /// <summary>
        /// Close all menus
        /// </summary>
        public void CloseAllMenus()
        {
            Debug.Log("GalaxyUIStateManager: Closing all menus");

            if (sysBuildMenu != null) sysBuildMenu.SetActive(false);
            if (intelMenuView != null) intelMenuView.SetActive(false);
            if (encyclopediaMenuView != null) encyclopediaMenuView.SetActive(false);
            if (techTreeMenuView != null) techTreeMenuView.SetActive(false);
            if (habitableSysMenu != null) habitableSysMenu.SetActive(false);

            CloseAllBackgrounds();

            CurrentOpenMenu = Menu.None;
            CurrentOpenMenuObject = null;
        }

        /// <summary>
        /// Close all background panels
        /// </summary>
        public void CloseAllBackgrounds()
        {
            if (sysBackground != null) sysBackground.SetActive(false);
            if (fleetsBackground != null) fleetsBackground.SetActive(false);
            if (diplomacyBackground != null) diplomacyBackground.SetActive(false);
            if (intelBackground != null) intelBackground.SetActive(false);
            if (encyclopediaBackground != null) encyclopediaBackground.SetActive(false);
            if (techTreeBackground != null) techTreeBackground.SetActive(false);
        }

        /// <summary>
        /// Activate the appropriate background for a menu
        /// </summary>
        private void ActivateBackgroundForMenu(Menu menuEnum)
        {
            // First close all backgrounds
            CloseAllBackgrounds();

            // Then activate the correct one
            switch (menuEnum)
            {
                case Menu.SystemsMenu:
                case Menu.ASystemMenu:
                case Menu.StarSys:
                    if (sysBackground != null) sysBackground.SetActive(true);
                    break;
                case Menu.FleetMenu:
                case Menu.AFleetMenu:
                case Menu.Fleet:
                    if (fleetsBackground != null) fleetsBackground.SetActive(true);
                    break;
                case Menu.DiplomacyMenu:
                case Menu.ADiplomacyMenu:
                case Menu.Diplomacy:
                    if (diplomacyBackground != null) diplomacyBackground.SetActive(true);
                    break;
                case Menu.IntellMenu:
                case Menu.Intel:
                    if (intelBackground != null) intelBackground.SetActive(true);
                    break;
                case Menu.EncyclopedianMenu:
                case Menu.Encyclopedia:
                    if (encyclopediaBackground != null) encyclopediaBackground.SetActive(true);
                    break;
                case Menu.TechTree:
                    if (techTreeBackground != null) techTreeBackground.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// Set click mode for galaxy interactions
        /// </summary>
        public void SetClickMode(GalaxyClickMode mode)
        {
            CurrentClickMode = mode;
        }

        /// <summary>
        /// Reset click mode to normal
        /// </summary>
        public void ResetClickMode()
        {
            CurrentClickMode = GalaxyClickMode.Normal;
            Debug.Log("GalaxyUIStateManager: Click mode reset to Normal");
        }

        /// <summary>
        /// Show/hide the "select other" button
        /// </summary>
        public void SetSelectOtherButtonVisible(bool visible)
        {
            if (selectOtherSysOrFleetButtonGO != null)
            {
                selectOtherSysOrFleetButtonGO.SetActive(visible);
            }
        }

        /// <summary>
        /// Hide diplomacy "no contacts" UI
        /// </summary>
        public void HideNoContactUI()
        {
            // [DiploPanelDiag] Catches every caller (DiplomacyManager.InstantiateDiplomacyController,
            // DiplomacyMenuUIController, GalaxyMenuUIController's Diplomacy click), not just the
            // ribbon-click path, so we can see whether this ever fires proactively after combat
            // resolves a new contact vs. only firing once the player manually opens the menu.
            Debug.LogWarning($"[DiploPanelDiag] HideNoContactUI called. diplomacyNoContacts={(diplomacyNoContacts != null ? "OK" : "NULL")} wasActive={(diplomacyNoContacts != null ? diplomacyNoContacts.activeSelf.ToString() : "N/A")}\n{System.Environment.StackTrace}");
            if (diplomacyNoContacts != null)
            {
                diplomacyNoContacts.SetActive(false);
            }
        }

        /// <summary>
        /// Show diplomacy "no contacts" UI
        /// </summary>
        public void ShowNoContactUI()
        {
            Debug.LogWarning($"[DiploPanelDiag] ShowNoContactUI called. diplomacyNoContacts={(diplomacyNoContacts != null ? "OK" : "NULL")} wasActive={(diplomacyNoContacts != null ? diplomacyNoContacts.activeSelf.ToString() : "N/A")}\n{System.Environment.StackTrace}");
            if (diplomacyNoContacts != null)
            {
                diplomacyNoContacts.SetActive(true);
            }
        }
    }
}
