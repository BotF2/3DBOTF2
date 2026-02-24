using UnityEngine;

namespace BOTF3D.UI
{
    public class WarpSpeedBackground : MonoBehaviour
    {
        [Header("Particle System Reference")]
        [SerializeField] private ParticleSystem warpParticles;

        [Header("Speed Settings")]
        [SerializeField] private float baseSpeed = 10f;        // ⬇️ Was 30
        [SerializeField] private float maxSpeed = 25f;         // ⬇️ Was 80
        [SerializeField] private float acceleration = 2f;      // ⬇️ Was 5

        private float currentSpeed;

        private void Start()
        {
            if (warpParticles == null)
            {
                warpParticles = GetComponent<ParticleSystem>();
            }

            currentSpeed = baseSpeed;
            UpdateParticleSpeed();
        }

        private void Update()
        {
            // Optional: Gradually increase speed for dramatic effect
            if (currentSpeed < maxSpeed)
            {
                currentSpeed += acceleration * Time.deltaTime;
                UpdateParticleSpeed();
            }
        }

        private void UpdateParticleSpeed()
        {
            var velocityModule = warpParticles.velocityOverLifetime;
            velocityModule.z = currentSpeed;
        }

        /// <summary>
        /// Change star color based on selected civilization theme
        /// </summary>
        public void SetThemeColor(Color themeColor)
        {
            var main = warpParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(themeColor, Color.white);

            var trails = warpParticles.trails;
            trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(themeColor, new Color(themeColor.r, themeColor.g, themeColor.b, 0));
        }
    }
}