using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FamiliarDread
{
    /// <summary>Reduziert nur das indirekte Umgebungslicht der zugewiesenen Dachbodenoberflächen.</summary>
    /// <remarks>Direkte Lichtquellen, insbesondere die Taschenlampe, bleiben voll wirksam.</remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class VillaAtticAmbient : MonoBehaviour
    {
        [Tooltip("Dachboden, Möbel, Kisten und zusätzliche Bodenplatten. Keine gemeinsam genutzten Materialien ändern.")]
        public Renderer[] surfaces = new Renderer[0];
        [HideInInspector] public LightProbeUsage[] savedProbeUsage = new LightProbeUsage[0];

        [ColorUsage(false, true)]
        [Tooltip("Sehr schwaches Restlicht in linearen Farbwerten.")]
        public Color ambientColor = new Color(.001f, .0015f, .002f);

        readonly Dictionary<Renderer, LightProbeUsage> originalUsage = new Dictionary<Renderer, LightProbeUsage>();
        readonly Dictionary<Renderer, MaterialPropertyBlock> originalBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
        bool refresh;

        void OnEnable() { Apply(); }
        void OnValidate() { refresh = true; }
        void Update()
        {
            if (!refresh)
                return;
            refresh = false;
            Apply();
        }

        [ContextMenu("Dachboden-Restlicht aktualisieren")]
        public void Apply()
        {
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(ambientColor);
            var probes = new[] { probe };
            for (int i = 0; i < surfaces.Length; i++)
            {
                var surface = surfaces[i];
                if (!surface)
                    continue;
                if (!originalUsage.ContainsKey(surface))
                {
                    originalUsage.Add(surface, i < savedProbeUsage.Length ? savedProbeUsage[i] : surface.lightProbeUsage);
                    var saved = new MaterialPropertyBlock();
                    surface.GetPropertyBlock(saved);
                    originalBlocks.Add(surface, saved);
                }
                var block = new MaterialPropertyBlock();
                surface.GetPropertyBlock(block);
                block.CopySHCoefficientArraysFrom(probes);
                surface.lightProbeUsage = LightProbeUsage.CustomProvided;
                surface.SetPropertyBlock(block);
            }
        }

        // Beim Abschalten die vorherige Beleuchtung wiederherstellen.
        void OnDisable()
        {
            foreach (var entry in originalUsage)
            {
                if (!entry.Key)
                    continue;
                entry.Key.lightProbeUsage = entry.Value;
                var block = originalBlocks[entry.Key];
                entry.Key.SetPropertyBlock(block.isEmpty ? null : block);
            }
            originalUsage.Clear();
            originalBlocks.Clear();
        }
    }
}
