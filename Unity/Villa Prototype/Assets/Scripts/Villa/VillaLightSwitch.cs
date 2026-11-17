using UnityEngine;

namespace FamiliarDread
{
    /// <summary>Schaltet einen im Inspector zugewiesenen Lichtkreis über VillaExplorer ein und aus.</summary>
    [DisallowMultipleComponent]
    public class VillaLightSwitch : MonoBehaviour
    {
        [Tooltip("Nur diese Lampen werden geschaltet. Tageslicht und Taschenlampe gehören nicht hier hinein.")]
        public Light[] controlledLights = new Light[0];

        [Tooltip("Sichtbare Leuchtflächen der zugehörigen Lampen, ohne Halterungen oder Lampengehäuse.")]
        public Renderer[] bulbRenderers = new Renderer[0];

        [Tooltip("Die bewegliche Schalterwippe; ihre lokale X-Drehung zeigt den Schaltzustand.")]
        public Transform rocker;

        MaterialPropertyBlock[] originalBulbProperties;
        Color[] originalBulbColors;
        Quaternion rockerRestRotation;
        bool initialized;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public bool IsOn
        {
            get
            {
                foreach (var light in controlledLights)
                    if (light && light.enabled)
                        return true;
                return false;
            }
        }

        void Start()
        {
            Initialize();
            UpdateAppearance();
        }

        // Ausgangswerte einmal merken; Materialien werden nie direkt geändert.
        void Initialize()
        {
            if (initialized)
                return;

            if (rocker)
                rockerRestRotation = rocker.localRotation;
            originalBulbProperties = new MaterialPropertyBlock[bulbRenderers.Length];
            originalBulbColors = new Color[bulbRenderers.Length];
            for (int i = 0; i < bulbRenderers.Length; i++)
            {
                var renderer = bulbRenderers[i];
                if (!renderer)
                    continue;

                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                originalBulbProperties[i] = properties;
                var material = renderer.sharedMaterial;
                originalBulbColors[i] = properties.HasColor(BaseColorId)
                    ? properties.GetColor(BaseColorId)
                    : material && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            }
            initialized = true;
        }

        /// <summary>Wechselt den Lichtzustand. Ohne gültige Lampenreferenz passiert nichts.</summary>
        public bool TryToggle()
        {
            Initialize();
            bool foundLight = false;
            bool turnOn = !IsOn;
            foreach (var light in controlledLights)
            {
                if (!light)
                    continue;
                light.enabled = turnOn;
                foundLight = true;
            }

            if (foundLight)
            {
                UpdateAppearance();
                var audio = GetComponent<VillaInteractionAudio>();
                if (audio)
                    audio.Play(turnOn);
            }
            return foundLight;
        }

        void UpdateAppearance()
        {
            bool on = IsOn;
            if (rocker)
                rocker.localRotation = rockerRestRotation * Quaternion.Euler(on ? -8 : 8, 0, 0);

            for (int i = 0; i < bulbRenderers.Length; i++)
            {
                var renderer = bulbRenderers[i];
                if (!renderer || originalBulbProperties[i] == null)
                    continue;

                if (on)
                {
                    renderer.SetPropertyBlock(originalBulbProperties[i].isEmpty ? null : originalBulbProperties[i]);
                    continue;
                }

                // Ausgeschaltete Lampen sollen auch optisch nicht weiter leuchten.
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor(BaseColorId, originalBulbColors[i] * .12f);
                properties.SetColor(EmissionColorId, Color.black);
                renderer.SetPropertyBlock(properties);
            }
        }

        void OnDestroy()
        {
            if (!initialized)
                return;
            for (int i = 0; i < bulbRenderers.Length; i++)
                if (bulbRenderers[i] && originalBulbProperties[i] != null)
                    bulbRenderers[i].SetPropertyBlock(originalBulbProperties[i].isEmpty ? null : originalBulbProperties[i]);
        }
    }
}
