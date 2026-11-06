using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vergrößert nur die Grundfläche des vorhandenen Gartenhauses; Tür und Raumhöhe bleiben erhalten.</summary>
public static class VillaGardenShedResize
{
    private static float Ground(MeshCollider ground, Vector3 p)
    {
        RaycastHit hit;
        if (!ground.Raycast(new Ray(new Vector3(p.x, 60, p.z), Vector3.down), out hit, 120))
            throw new InvalidOperationException("Gartenboden unter dem Gartenhaus fehlt.");
        return hit.point.y;
    }

    [MenuItem("Familiar Dread/Villa/Gartenhaus auf 9 x 8 Meter vergrößern")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Zuerst den Spielmodus beenden.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var villa = GameObject.Find("VILLA_PROTOTYPE");
        if (!villa || scene.path != "Assets/Scenes/VillaPrototype.unity")
            throw new InvalidOperationException("Bitte VillaPrototype öffnen.");
        var garden = villa.transform.Find("Garten_und_Zufahrt");
        var details = garden.Find("Garten_Ausstattung");
        var shed = details.Find("Gartenhaus");
        var ramp = details.Find("Gartenhaus_Zugangsrampe");
        var ground = garden.Find("Garten_Hoehenprofil").GetComponent<MeshCollider>();
        if (!shed || !ramp || !ground || !shed.Find("Boden") || !shed.Find("Fundament"))
            throw new InvalidOperationException("Gartenhaus oder Gelände fehlt.");

        const float width = 9, depth = 8;
        Physics.SyncTransforms();
        float lowest = float.PositiveInfinity, highest = float.NegativeInfinity;
        foreach (float x in new[] { -width / 2 - .15f, 0, width / 2 + .15f })
        foreach (float z in new[] { -depth / 2 - .15f, 0, depth / 2 + .15f })
        {
            float y = Ground(ground, shed.TransformPoint(new Vector3(x, 0, z)));
            lowest = Mathf.Min(lowest, y);
            highest = Mathf.Max(highest, y);
        }

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Gartenhaus-Grundfläche auf 9 x 8 Meter vergrößern");
        Undo.RecordObjects(shed.GetComponentsInChildren<Transform>(true).Cast<UnityEngine.Object>()
            .Concat(new UnityEngine.Object[] { ramp }).ToArray(), "Gartenhaus-Bauteile anpassen");
        float level = highest + .08f;
        var origin = shed.position;
        origin.y = level;
        shed.position = origin;

        foreach (Transform part in shed)
        {
            Vector3 p = part.localPosition;
            Vector3 size = part.localScale;
            switch (part.name)
            {
                case "Boden": size.x = width; size.z = depth; break;
                case "Fundament":
                    float bottom = lowest - .15f;
                    p.y = (bottom + level - .18f) / 2 - level;
                    size = new Vector3(width + .05f, level - .18f - bottom, depth + .05f);
                    break;
                case "Rueckwand": p.z = depth / 2; size.x = width; break;
                case "Seitenwand": p.x = Mathf.Sign(p.x) * width / 2; size.z = depth; break;
                case "Frontwand":
                    p.x = Mathf.Sign(p.x) * (width / 2 + .6f) / 2;
                    p.z = -depth / 2;
                    size.x = width / 2 - .6f;
                    break;
                case "Tuerrahmen_Seite":
                case "Tuerrahmen_Oben":
                case "Tuersturz":
                case "Gartenhaustuer_E": p.z = -depth / 2; break;
                case "Holzgiebel":
                    p.z = Mathf.Sign(p.z) * depth / 2;
                    size.x = width / part.GetComponent<MeshFilter>().sharedMesh.bounds.size.x;
                    break;
                case "Satteldach":
                    float span = width / 2 + .25f;
                    float rise = .85f / (width / 2) * span;
                    int side = p.x < 0 ? -1 : 1;
                    p.x = side * span / 2;
                    p.y = 3.35f - rise / 2;
                    size = new Vector3(Mathf.Sqrt(span * span + rise * rise), .15f, depth + .5f);
                    part.localRotation = Quaternion.Euler(0, 0, -side * Mathf.Atan2(rise, span) * Mathf.Rad2Deg);
                    break;
            }
            part.localPosition = p;
            part.localScale = size;
        }

        Vector3 a = shed.TransformPoint(new Vector3(0, 0, -depth / 2 - .1f));
        Vector3 b = shed.TransformPoint(new Vector3(0, 0, -depth / 2 - 3.1f));
        b.y = Ground(ground, b) + .04f;
        ramp.position = (a + b) / 2 - Vector3.up * .07f;
        ramp.rotation = Quaternion.LookRotation((a - b).normalized, Vector3.up);
        ramp.localScale = new Vector3(1.5f, .14f, Vector3.Distance(a, b));
        Physics.SyncTransforms();
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Szene konnte nicht gespeichert werden.");
        Selection.activeGameObject = shed.gameObject;
        if (SceneView.lastActiveSceneView)
            SceneView.lastActiveSceneView.LookAt(shed.position + Vector3.up, Quaternion.Euler(24, 30, 0), 10);
        SceneView.RepaintAll();
        Debug.Log("Gartenhaus auf 9 x 8 m vergrößert (3,6-fache Grundfläche) und gespeichert. "
            + "Tür und Raumhöhe erhalten; Fundament, Giebel, Dach und Rampe angepasst.");
    }
}
