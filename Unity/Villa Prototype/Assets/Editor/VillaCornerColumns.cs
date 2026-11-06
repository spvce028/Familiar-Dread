using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Ergänzt Eckstützen anhand der vorhandenen, manuell angelegten Dachgesimse.</summary>
/// <remarks>Bestehende Böden, Gesimse, Treppen und Geländer werden nicht verändert.</remarks>
public static class VillaCornerColumns
{
    private struct Plan
    {
        public string name;
        public Vector3 center;
        public float top, shaftX, trimX;
        public Material material;
    }

    private static Renderer Unique(Transform root, string name)
    {
        var matches = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == name).Select(t => t.GetComponent<Renderer>()).Where(r => r).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Gesims nicht eindeutig gefunden: " + name);
        return matches[0];
    }

    private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = position;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = material;
    }

    [MenuItem("Familiar Dread/Villa/Ecksäulen an Dachgesimsen ergänzen")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Zuerst den Spielmodus beenden.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var villa = GameObject.Find("VILLA_PROTOTYPE");
        if (!villa || scene.path != "Assets/Scenes/VillaPrototype.unity")
            throw new InvalidOperationException("Bitte VillaPrototype öffnen.");
        if (villa.transform.Find("Fassaden_Ecksaeulen"))
            throw new InvalidOperationException("Ecksäulen sind bereits vorhanden; bitte direkt in der Szene bearbeiten.");

        var root = villa.transform;
        var front = Unique(root, "Dach_Gesims_Vorne");
        var back = Unique(root, "Dach_Gesims_Hinten");
        var sides = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("Dach_Gesims_Seite", StringComparison.Ordinal))
            .Select(t => t.GetComponent<Renderer>()).Where(r => r).ToArray();
        if (sides.Length != 2 || !front.sharedMaterial)
            throw new InvalidOperationException("Haus-Dachgesimse fehlen oder Material fehlt.");

        var garageFront = Unique(root, "Garage_Gesims_Vorne");
        var garageBack = Unique(root, "Garage_Gesims_Hinten");
        var garageSide = Unique(root, "Garage_Gesims_Seite");
        if (!garageFront.sharedMaterial)
            throw new InvalidOperationException("Garagengesims hat kein Material.");

        // Maße aus den vorhandenen Gesimsen und Fassaden ablesen, nicht aus den vom Nutzer angepassten Böden.
        var exteriorLeftWalls = root.Find("Erdgeschoss").GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Seitenwand_Wand" && t.position.x < 0)
            .Select(t => t.GetComponent<Renderer>()).Where(r => r).ToArray();
        if (exteriorLeftWalls.Length == 0)
            throw new InvalidOperationException("Linke Außenfassade nicht gefunden.");

        float houseLeft = sides.Min(r => r.bounds.min.x);
        float houseRight = sides.Max(r => r.bounds.max.x);
        float houseFront = front.bounds.min.z;
        float houseBack = back.bounds.max.z;
        float houseTop = front.bounds.max.y;
        float garageLeft = garageSide.bounds.min.x;
        float garageJoin = exteriorLeftWalls.Min(r => r.bounds.min.x);
        float garageFrontZ = garageFront.bounds.min.z;
        float garageBackZ = garageBack.bounds.max.z;
        float garageTop = garageFront.bounds.max.y;

        var gardens = root.Find("Garten_und_Zufahrt").GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Garten_Seite").Select(t => t.GetComponent<Renderer>()).Where(r => r).ToArray();
        if (gardens.Length == 0)
            throw new InvalidOperationException("Gartenhöhe nicht gefunden.");
        float ground = gardens.Max(r => r.bounds.max.y);
        var plans = new List<Plan>();
        foreach (bool left in new[] { true, false })
        foreach (bool rear in new[] { false, true })
        {
            plans.Add(new Plan {
                name = "Haus_" + (left ? "Links_" : "Rechts_") + (rear ? "Hinten" : "Vorne"),
                center = new Vector3(left ? houseLeft + .35f : houseRight - .35f, 0,
                    rear ? houseBack - .35f : houseFront + .35f),
                top = houseTop, shaftX = .5f, trimX = .7f, material = front.sharedMaterial
            });
            plans.Add(new Plan {
                name = "Garage_" + (left ? "Aussen_" : "Hausanschluss_") + (rear ? "Hinten" : "Vorne"),
                center = new Vector3(left ? garageLeft + .35f : garageJoin, 0,
                    rear ? garageBackZ - .35f : garageFrontZ + .35f),
                top = garageTop, shaftX = left ? .5f : .28f, trimX = left ? .7f : .4f,
                material = garageFront.sharedMaterial
            });
        }

        // Alle Pläne prüfen, bevor neue Objekte entstehen. Die Pfeiler dürfen keine Toröffnung verengen.
        var garageOpenings = root.Find("Doppelgarage").GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Garage_Front_Rahmen_oben").Select(t => t.GetComponent<Renderer>()).Where(r => r).ToArray();
        foreach (var plan in plans)
        {
            if (plan.top - ground < 1)
                throw new InvalidOperationException("Ungültige Säulenhöhe: " + plan.name);
            if (!plan.name.StartsWith("Garage_", StringComparison.Ordinal) || !plan.name.EndsWith("Vorne", StringComparison.Ordinal))
                continue;
            foreach (var opening in garageOpenings)
            {
                // Der Rahmen ist 15 cm breiter als die lichte Toröffnung.
                float openingLeft = opening.bounds.min.x + .075f;
                float openingRight = opening.bounds.max.x - .075f;
                float columnLeft = plan.center.x - plan.trimX / 2;
                float columnRight = plan.center.x + plan.trimX / 2;
                if (columnLeft < openingRight && columnRight > openingLeft)
                    throw new InvalidOperationException("Säule würde Toröffnung berühren: " + plan.name);
            }
        }

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Ecksäulen an bestehenden Gesimsen ergänzen");
        var group = new GameObject("Fassaden_Ecksaeulen");
        group.transform.SetParent(root, false);
        foreach (var plan in plans)
        {
            var column = new GameObject(plan.name).transform;
            column.SetParent(group.transform, false);
            const float baseHeight = .22f;
            const float capitalHeight = .25f;
            float shaftBottom = ground + baseHeight;
            float shaftTop = plan.top - capitalHeight;
            var center = plan.center;
            center.y = ground + baseHeight / 2;
            Box(column, "Sockel", center, new Vector3(plan.trimX, baseHeight, .7f), plan.material);
            center.y = (shaftBottom + shaftTop) / 2;
            Box(column, "Schaft", center, new Vector3(plan.shaftX, shaftTop - shaftBottom, .5f), plan.material);
            center.y = plan.top - capitalHeight / 2;
            Box(column, "Kapitell", center, new Vector3(plan.trimX, capitalHeight, .7f), plan.material);
        }
        Undo.RegisterCreatedObjectUndo(group, "Acht Ecksäulen ergänzen");
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Szene konnte nicht gespeichert werden.");
        Physics.SyncTransforms();
        SceneView.RepaintAll();
        Selection.activeGameObject = group;
        if (SceneView.lastActiveSceneView)
            SceneView.lastActiveSceneView.LookAt(new Vector3(-3, 5, 0), Quaternion.Euler(14, 24, 0), 26);
        Debug.Log("8 Ecksäulen bis zu den vorhandenen Dachgesimsen ergänzt und gespeichert. "
            + "Gesimsmaterial übernommen; Böden und Geländer unverändert; Garagenöffnungen frei.");
    }
}
