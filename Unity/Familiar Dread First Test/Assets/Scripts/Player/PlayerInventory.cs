using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    //HashSet als Inventar, weil schnelle Abfrage und Item kann nur 1x enthalten sein
    private HashSet<string> items = new HashSet<string>();

    public void AddItem(string itemID)
    {
        items.Add(itemID);

        Debug.Log("Item aufgenommen: " + itemID);
    }

    public bool HasItem(string itemID)
    {
        return items.Contains(itemID);
    }

    public void RemoveItem(string itemID)
    {
        items.Remove(itemID);
    }
}
