using UnityEngine;
using TMPro;

public class KillerKeyInventory : MonoBehaviour
{
    public bool hatSchluessel = false;
    public TextMeshProUGUI inventarText; // Optional um eigene UI-Textfelder zu verknüpfen
    void Update()
    {
        if (inventarText != null)
        {
            inventarText.text = hatSchluessel ? "Inventar: Haustürschlüssel" : "Inventar: Leer";
        }
    }
}
