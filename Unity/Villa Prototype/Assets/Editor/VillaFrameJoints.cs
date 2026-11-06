using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Korrigiert die Anschlüsse in der aktuell geladenen Szene, ohne diese neu aufzubauen.
/// Änderungen an einer Szenendatei werden bei einer bereits geöffneten, veränderten Szene
/// nicht automatisch in deren Arbeitsspeicher übernommen. Dieses Werkzeug bearbeitet
/// deshalb ausdrücklich die geladenen Objekte und erhält andere Szenenänderungen.
/// </summary>
public static class VillaFrameJoints
{
    private const float HiddenOverlap = 0.01f;

    [MenuItem("Familiar Dread/Villa/Rahmenanschlüsse korrigieren")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Zuerst den Spielmodus beenden.");

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/VillaPrototype.unity")
            throw new InvalidOperationException("Bitte die VillaPrototype-Szene öffnen.");

        var villa = GameObject.Find("VILLA_PROTOTYPE");
        if (!villa)
            throw new InvalidOperationException("Villa fehlt in der geöffneten Szene.");

        // Erst alle Zuordnungen prüfen; bei einem unerwarteten Anschluss nichts verändern.
        var lintels = new List<Transform>();
        var bottoms = new List<float>();
        Transform preview = null;
        int inspected = 0;
        foreach (var lintel in villa.GetComponentsInChildren<Transform>(true))
        {
            if (!lintel.name.EndsWith("_Sturz", StringComparison.Ordinal))
                continue;

            string frameName = lintel.name.Substring(0, lintel.name.Length - 6) + "_Rahmen_oben";
            Transform frame = null;
            foreach (Transform sibling in lintel.parent)
            {
                if (!string.Equals(sibling.name, frameName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var difference = sibling.localPosition - lintel.localPosition;
                if (Mathf.Abs(difference.x) > .002f || Mathf.Abs(difference.z) > .002f)
                    continue;

                if (frame)
                    throw new InvalidOperationException("Rahmen nicht eindeutig: " + lintel.name);
                frame = sibling;
            }

            if (!frame)
                throw new InvalidOperationException("Oberer Rahmen fehlt: " + lintel.name);

            float frameBottom = frame.localPosition.y - frame.localScale.y / 2;
            float bottom = lintel.localPosition.y - lintel.localScale.y / 2;
            float top = lintel.localPosition.y + lintel.localScale.y / 2;
            float targetBottom = frameBottom + HiddenOverlap;
            if (Mathf.Abs(bottom - frameBottom) > .02f || top <= targetBottom)
                throw new InvalidOperationException("Unerwarteter Wandanschluss: " + lintel.name);

            inspected++;
            if (bottom < targetBottom - .00001f)
            {
                lintels.Add(lintel);
                bottoms.Add(targetBottom);
            }

            if (!preview && frame.name == "Front_Rahmen_oben" && frame.parent.name == "Erdgeschoss")
                preview = frame;
        }

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Obere Rahmenanschlüsse korrigieren");
        for (int i = 0; i < lintels.Count; i++)
        {
            var lintel = lintels[i];
            Undo.RecordObject(lintel, "Sturzfläche im Holz verbergen");
            var position = lintel.localPosition;
            var scale = lintel.localScale;
            float top = position.y + scale.y / 2;

            // Die Sturzunterseite liegt im Holz statt auf dessen sichtbarer Unterseite.
            // Wandoberkante, Rahmen und Tür-/Fensteröffnung bleiben dabei unverändert.
            position.y = (top + bottoms[i]) / 2;
            scale.y = top - bottoms[i];
            lintel.localPosition = position;
            lintel.localScale = scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(lintel);
        }

        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Die Villa-Szene konnte nicht gespeichert werden.");

        Physics.SyncTransforms();
        if (preview && SceneView.lastActiveSceneView)
        {
            // Den Anschluss von schräg unten zur Sichtkontrolle zeigen.
            var pivot = preview.position - Vector3.up * .035f;
            SceneView.lastActiveSceneView.LookAt(pivot, Quaternion.Euler(-25, 0, 0), 1.8f);
        }

        SceneView.RepaintAll();
        Debug.Log("Rahmenanschlüsse: " + inspected + " geprüft, " + lintels.Count
            + " in der geladenen Szene korrigiert und gespeichert.");
    }

    /// <summary>Verbirgt die Wandoberkante unter jeder Fensterbank im Stein statt auf dessen sichtbarer Oberfläche.</summary>
    [MenuItem("Familiar Dread/Villa/Fensterbankanschlüsse korrigieren")]
    public static void ApplySills()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Zuerst den Spielmodus beenden.");

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/VillaPrototype.unity")
            throw new InvalidOperationException("Bitte die VillaPrototype-Szene öffnen.");

        var villa = GameObject.Find("VILLA_PROTOTYPE");
        if (!villa)
            throw new InvalidOperationException("Villa fehlt in der geöffneten Szene.");

        // Vor dem Ändern sämtliche Anschlüsse prüfen. Andere Szenenobjekte bleiben unberührt.
        var walls = new List<Transform>();
        var tops = new List<float>();
        int inspected = 0;
        const string sillSuffix = "_Fensterbank";
        foreach (var sill in villa.GetComponentsInChildren<Transform>(true))
        {
            if (!sill.name.EndsWith(sillSuffix, StringComparison.Ordinal))
                continue;

            string wallName = sill.name.Substring(0, sill.name.Length - sillSuffix.Length) + "_Brüstung";
            Transform wall = null;
            foreach (Transform sibling in sill.parent)
            {
                if (sibling.name != wallName)
                    continue;

                var difference = sibling.localPosition - sill.localPosition;
                if (Mathf.Abs(difference.x) > .002f || Mathf.Abs(difference.z) > .002f)
                    continue;

                if (wall)
                    throw new InvalidOperationException("Wand nicht eindeutig: " + sill.name);
                wall = sibling;
            }

            if (!wall)
                throw new InvalidOperationException("Wand unter Fensterbank fehlt: " + sill.name);

            float sillTop = sill.localPosition.y + sill.localScale.y / 2;
            float wallTop = wall.localPosition.y + wall.localScale.y / 2;
            float wallBottom = wall.localPosition.y - wall.localScale.y / 2;
            float targetTop = sillTop - HiddenOverlap;
            if (Mathf.Abs(wallTop - sillTop) > .02f || targetTop <= wallBottom)
                throw new InvalidOperationException("Unerwarteter Fensterbankanschluss: " + sill.name);

            inspected++;
            if (wallTop > targetTop + .00001f)
            {
                walls.Add(wall);
                tops.Add(targetTop);
            }
        }

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fensterbankanschlüsse korrigieren");
        for (int i = 0; i < walls.Count; i++)
        {
            var wall = walls[i];
            Undo.RecordObject(wall, "Wandoberkante im Stein verbergen");
            var position = wall.localPosition;
            var scale = wall.localScale;
            float bottom = position.y - scale.y / 2;

            // Die Wand endet 1 cm unter der Steinoberfläche. Die Fensterbank selbst bleibt unverändert.
            position.y = (bottom + tops[i]) / 2;
            scale.y = tops[i] - bottom;
            wall.localPosition = position;
            wall.localScale = scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(wall);
        }

        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Die Villa-Szene konnte nicht gespeichert werden.");

        Physics.SyncTransforms();
        SceneView.RepaintAll();
        Debug.Log("Fensterbankanschlüsse: " + inspected + " geprüft, " + walls.Count
            + " in der geladenen Szene korrigiert und gespeichert.");
    }

}
