using TMPro;
using UnityEngine;
using BOTF3D.Core;
using BOTF3D.Combat;
using BOTF3D.Civilization;
using BOTF3D.Galaxy;
using BOTF3D.Audio;



namespace BOTF3D.UI
{
    public class StardateUIController : MonoBehaviour
    {
        public void Initialize() { }
        public void UpdateState() { }
        public TextMeshProUGUI stardateText;

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnStardateChanged += UpdateDateText;
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnStardateChanged -= UpdateDateText;
        }
        void UpdateDateText()
        {
            stardateText.text = TimeManager.Instance.currentStardate.ToString();
        }
    }
}
