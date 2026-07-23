using BOTF3D.Core;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BOTF3D.UI
{
    /// <summary>
    /// Banner shown in the Galaxy scene to human players who are not part of an in-progress
    /// combat. They stay on the Galaxy map (see FleetController's bystander guard around
    /// RpcStartCombat/Rpc*Encounter) while the two combatants resolve their fight, so this just
    /// tells them why the turn clock is on hold - it doesn't block their input.
    /// </summary>
    public class CombatPausedNoticeUI : MonoBehaviour
    {
        public static CombatPausedNoticeUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject noticePanel;
        public TextMeshProUGUI messageText;

        [Header("Flash Settings")]
        public float flashInterval = 0.5f;

        private Coroutine flashRoutine;
        private Color baseColor;

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
                return;
            }

            if (messageText != null)
                baseColor = messageText.color;

            if (noticePanel != null)
                noticePanel.SetActive(false);
        }

        public void Show(CivEnum civA, CivEnum civB)
        {
            if (messageText != null)
                messageText.text = $"Paused for combat: {civA} vs {civB}";

            if (noticePanel != null)
                noticePanel.SetActive(true);
            else
                Debug.LogWarning("CombatPausedNoticeUI: noticePanel not assigned in Inspector.");

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            if (messageText != null)
                flashRoutine = StartCoroutine(FlashMessage());
        }

        public void Hide()
        {
            if (noticePanel != null)
                noticePanel.SetActive(false);

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            if (messageText != null)
                messageText.color = baseColor;
        }

        private IEnumerator FlashMessage()
        {
            var wait = new WaitForSecondsRealtime(flashInterval);
            var visibleColor = baseColor;
            var hiddenColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            while (true)
            {
                messageText.color = hiddenColor;
                yield return wait;
                messageText.color = visibleColor;
                yield return wait;
            }
        }
    }
}
