// Assets/Scripts/Voxel/Lighting/SunController.cs
// Fait tourner le soleil et ajuste intensité/couleur. Utilise URP shadows.

using UnityEngine;

namespace Voxel.Lighting
{
    [ExecuteAlways]
    public sealed class SunController : MonoBehaviour
    {
        [Header("Cycle")]
        public float dayLengthSeconds = 600f; // 10 minutes
        [Range(0f,1f)] public float timeOfDay = 0.25f; // 0=minuit, 0.25=6h, 0.5=12h, 0.75=18h
        public bool runInPlayMode = true;

        [Header("Sun")]
        public Light sun;
        public Gradient sunColor;
        public AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1f);

        private void Reset()
        {
            sun = GetComponent<Light>();
            if (!sun) sun = FindFirstObjectByType<Light>();
            // Dégradé simple jour/soir/nuit
            sunColor = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.05f,0.1f,0.2f), 0f),
                    new GradientColorKey(new Color(1f,0.95f,0.85f), 0.5f),
                    new GradientColorKey(new Color(0.9f,0.5f,0.3f), 0.75f),
                    new GradientColorKey(new Color(0.05f,0.1f,0.2f), 1f),
                },
                alphaKeys = new[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(1f,1f) }
            };
        }

        private void Update()
        {
            if (!sun) return;

            if (Application.isPlaying && runInPlayMode && dayLengthSeconds>0f)
                timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / dayLengthSeconds, 1f);

            // Rotation: 0.0 = minuit (sous l’horizon), 0.25 = 6h (levant), 0.5 = midi, 0.75 = 18h
            float angle = (timeOfDay * 360f) - 90f; // 0.25 => 0°
            Vector3 euler = new Vector3(angle, 30f, 0f); // azimut fixe 30°
            sun.transform.rotation = Quaternion.Euler(euler);

            // Couleur/intensité
            sun.color = sunColor.Evaluate(timeOfDay);
            float t = Mathf.Clamp01(Mathf.Cos((timeOfDay-0.5f)*Mathf.PI*2f)*0.5f+0.5f); // 0 nuit, 1 midi
            sun.intensity = sunIntensity.Evaluate(1f - Mathf.Abs(timeOfDay-0.5f)*2f) * 1.0f;

            // Ambient simple (optionnel)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(new Color(0.02f,0.03f,0.05f), new Color(0.6f,0.7f,0.8f), t);
        }
    }
}