using UnityEngine;
using UnityEngine.InputSystem;

namespace FamiliarDread
{
    public class KeyItem : MonoBehaviour
    {
        [Header("Bestehende Team-Systeme")]
        public VillaPuzzleState story;
        public KillerKeyInventory spielerInventar;

        [Header("Killer-Verknüpfungen")]
        public KillerPatrol killer;
        public GameObject schluesselAnHuefte;
        public float aufhebeReichweite = 3.5f;
        public float taschendiebstahlReichweite = 2.2f;

        private bool wurdeAufgehoben = false;
        private bool killerFordertSchluessel = false;
        private bool schluesselHaengtAnHuefte = false;

        void Start()
        {
            if (schluesselAnHuefte != null)
            {
                schluesselAnHuefte.SetActive(false);
            }
        }

        void Update()
        {
            if (spielerInventar == null) return;

            var k = Keyboard.current;
            if (k == null) return;

            Renderer sichtbaresTeil = GetComponentInChildren<Renderer>();
            Vector3 schluesselPosition = sichtbaresTeil != null ? sichtbaresTeil.bounds.center : transform.position;

            // Schlüssel aufheben
            if (!wurdeAufgehoben)
            {
                float distanz = Vector3.Distance(schluesselPosition, spielerInventar.transform.position);
                
                if (distanz <= aufhebeReichweite && k.eKey.wasPressedThisFrame)
                {
                    wurdeAufgehoben = true;
                    spielerInventar.hatSchluessel = true;

                    if (killer != null && killer.siehtSpielerGerade)
                    {
                        killerFordertSchluessel = true;
                        if (story != null && story.ui != null)
                            story.ui.ShowToast("Gib mir den Schlüssel [G drücken]");
                    }
                    else if (story != null && story.ui != null)
                    {
                        story.ui.ShowToast("Haustürschlüssel aufgenommen");
                    }

                    foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
                    {
                        renderer.enabled = false;
                    }
                    foreach (var col in GetComponentsInChildren<Collider>())
                    {
                        col.enabled = false;
                    }
                }
            }
            // Schlüssel an Jess abgeben
            else if (killerFordertSchluessel && spielerInventar.hatSchluessel)
            {
                if (k.gKey.wasPressedThisFrame)
                {
                    spielerInventar.hatSchluessel = false;
                    killerFordertSchluessel = false;
                    schluesselHaengtAnHuefte = true;

                    if (schluesselAnHuefte != null) schluesselAnHuefte.SetActive(true);
                    if (story != null && story.ui != null) story.ui.ShowToast("Du hast den Schlüssel abgegeben.");
                }
            }
            // Schlüssel wieder von Jess Hüfte stehlen
            else if (schluesselHaengtAnHuefte && !spielerInventar.hatSchluessel && killer != null)
            {
                float distanzZuJess = Vector3.Distance(killer.transform.position, spielerInventar.transform.position);

                if (distanzZuJess <= taschendiebstahlReichweite && k.eKey.wasPressedThisFrame)
                {
                    schluesselHaengtAnHuefte = false;
                    spielerInventar.hatSchluessel = true;

                    if (schluesselAnHuefte != null) schluesselAnHuefte.SetActive(false);

                    if (killer.siehtSpielerGerade)
                    {
                        killerFordertSchluessel = true;
                        if (story != null && story.ui != null)
                            story.ui.ShowToast("Sie hat es bemerkt! Gib mir den Schlüssel [G drücken]");
                    }
                    else
                    {
                        if (story != null && story.ui != null)
                            story.ui.ShowToast("Schlüssel heimlich von der Hüfte gestohlen!");
                    }
                }
            }
        }
    }
}
