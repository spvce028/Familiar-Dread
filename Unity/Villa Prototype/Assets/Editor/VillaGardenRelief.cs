using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Ersetzt ausschließlich die vier ebenen Gartenflächen durch ein begehbares Höhenprofil.</summary>
/// <remarks>Die Originalflächen bleiben deaktiviert erhalten. Gebäude, Böden, Wege und Treppen bleiben unverändert.</remarks>
public static class VillaGardenRelief
{
    private static float Outside(Vector3 p, Bounds b)
    {
        float x = Mathf.Max(b.min.x - p.x, 0, p.x - b.max.x);
        float z = Mathf.Max(b.min.z - p.z, 0, p.z - b.max.z);
        return Mathf.Sqrt(x * x + z * z);
    }

    private static float Hill(float x, float z, float cx, float cz, float radius, float height)
    {
        float dx = (x - cx) / radius;
        float dz = (z - cz) / radius;
        return height * Mathf.Exp(-(dx * dx + dz * dz));
    }

    private static float Relief(float x, float z)
    {
        return Hill(x, z, -26, 15, 9, 1.45f)
            + Hill(x, z, 24, 14, 10, 1.9f)
            + Hill(x, z, 20, -18, 8, .85f)
            + Hill(x, z, -28, -19, 9, .75f)
            + Hill(x, z, -2, 25, 9, 1.2f)
            - Hill(x, z, -24, -1, 7, .75f)
            - Hill(x, z, 16, -4, 6, .65f)
            - Hill(x, z, 9, 21, 7, .5f)
            + .12f * Mathf.Sin(x * .25f) * Mathf.Sin(z * .22f);
    }

    private static float Height(float x, float z, float baseline, List<Bounds> clearances)
    {
        var p = new Vector3(x, baseline, z);
        float fade = 1;
        foreach (var b in clearances)
        {
            // Ein Meter eben um feste Bauteile, anschließend fünf Meter weicher Übergang.
            float weight = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Outside(p, b) - 1) / 5));
            fade = Mathf.Min(fade, weight);
        }
        return baseline + Relief(x, z) * fade;
    }

    private static List<float> Grid(float min, float max, IEnumerable<float> boundaries)
    {
        var values = new SortedSet<float> { min, max };
        for (float v = min + .5f; v < max - .001f; v += .5f)
        {
            // Winzige Dreiecke neben einer exakten ursprünglichen Flächengrenze vermeiden.
            if (!boundaries.Any(b => Mathf.Abs(b - v) < .04f))
                values.Add(v);
        }
        foreach (float v in boundaries)
            if (v >= min && v <= max)
                values.Add(v);
        return values.ToList();
    }

    [MenuItem("Familiar Dread/Villa/Garten mit Steigung und Gefälle gestalten")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Zuerst den Spielmodus beenden.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var villa = GameObject.Find("VILLA_PROTOTYPE");
        if (!villa || scene.path != "Assets/Scenes/VillaPrototype.unity")
            throw new InvalidOperationException("Bitte VillaPrototype öffnen.");
        var root = villa.transform;
        var garden = root.Find("Garten_und_Zufahrt");
        if (!garden || garden.Find("Garten_Hoehenprofil"))
            throw new InvalidOperationException("Garten fehlt oder Höhenprofil ist bereits vorhanden.");

        var patches = garden.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Garten_Seite" || t.name == "Garten_Vorne_Hinten")
            .Select(t => t.GetComponent<Renderer>()).Where(r => r && r.gameObject.activeSelf).ToArray();
        if (patches.Length != 4 || !patches[0].sharedMaterial)
            throw new InvalidOperationException("Vier ursprüngliche Gartenflächen mit Material erwartet.");
        var footprint = patches.Select(p => p.bounds).ToArray();
        float baseline = footprint.Average(b => b.max.y);
        if (footprint.Any(b => Mathf.Abs(b.max.y - baseline) > .01f))
            throw new InvalidOperationException("Die Ausgangsflächen haben unterschiedliche Höhen.");

        var clearances = new List<Bounds>();
        var exterior = root.Find("Erdgeschoss").GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Front_Wand" || t.name == "Gartenseite_Wand" || t.name == "Seitenwand_Wand")
            .Select(t => t.GetComponent<Renderer>()).Where(r => r).ToArray();
        if (exterior.Length == 0)
            throw new InvalidOperationException("Gebäudegrundriss nicht gefunden.");
        Bounds house = exterior[0].bounds;
        foreach (var renderer in exterior)
            house.Encapsulate(renderer.bounds);
        clearances.Add(house);

        var garage = root.Find("Doppelgarage/Garage_Boden");
        if (!garage || !garage.GetComponent<Renderer>())
            throw new InvalidOperationException("Garagenboden nicht gefunden.");
        clearances.Add(garage.GetComponent<Renderer>().bounds);
        foreach (Transform t in garden)
            if (t.name == "Zufahrt" || t.name == "Eingangsweg")
                clearances.Add(t.GetComponent<Renderer>().bounds);

        var stairs = root.Find("Aussentreppe");
        if (!stairs)
            throw new InvalidOperationException("Eingangstreppen nicht gefunden.");
        var stairParts = stairs.GetComponentsInChildren<Renderer>();
        Bounds stairArea = stairParts[0].bounds;
        foreach (var renderer in stairParts)
            stairArea.Encapsulate(renderer.bounds);
        clearances.Add(stairArea);

        float xmin = footprint.Min(b => b.min.x), xmax = footprint.Max(b => b.max.x);
        float zmin = footprint.Min(b => b.min.z), zmax = footprint.Max(b => b.max.z);
        var xs = Grid(xmin, xmax, footprint.SelectMany(b => new[] { b.min.x, b.max.x }));
        var zs = Grid(zmin, zmax, footprint.SelectMany(b => new[] { b.min.z, b.max.z }));
        var vertices = new Vector3[xs.Count * zs.Count];
        var uv = new Vector2[vertices.Length];
        for (int z = 0; z < zs.Count; z++)
        for (int x = 0; x < xs.Count; x++)
        {
            int i = z * xs.Count + x;
            vertices[i] = new Vector3(xs[x], Height(xs[x], zs[z], baseline, clearances), zs[z]);
            uv[i] = new Vector2(xs[x] * .2f, zs[z] * .2f);
        }

        var triangles = new List<int>();
        for (int z = 0; z < zs.Count - 1; z++)
        for (int x = 0; x < xs.Count - 1; x++)
        {
            float cx = (xs[x] + xs[x + 1]) / 2;
            float cz = (zs[z] + zs[z + 1]) / 2;
            if (!footprint.Any(b => cx > b.min.x && cx < b.max.x && cz > b.min.z && cz < b.max.z))
                continue; // Den bisherigen Ausschnitt für das Haus nicht auffüllen.
            int a = z * xs.Count + x, b0 = a + 1, c = a + xs.Count, d = c + 1;
            triangles.AddRange(new[] { a, c, b0, b0, c, d });
        }

        // Die tatsächlichen Dreiecksneigungen prüfen und bei Bedarf alle Höhen sanft reduzieren.
        float maxGradient = 0;
        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]],
                vertices[triangles[i + 2]] - vertices[triangles[i]]);
            maxGradient = Mathf.Max(maxGradient,
                new Vector2(normal.x, normal.z).magnitude / Mathf.Max(.000001f, normal.y));
        }
        float factor = Mathf.Min(1, Mathf.Tan(18 * Mathf.Deg2Rad) / Mathf.Max(.000001f, maxGradient));
        for (int i = 0; i < vertices.Length; i++)
            vertices[i].y = baseline + (vertices[i].y - baseline) * factor;
        float low = vertices.Min(v => v.y), high = vertices.Max(v => v.y);

        const string folder = "Assets/VillaPrototype/Garten";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/VillaPrototype", "Garten");

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Begehbares Garten-Höhenprofil ergänzen");
        var surface = new GameObject("Garten_Hoehenprofil");
        surface.transform.SetParent(garden, false);
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = surface.transform.InverseTransformPoint(vertices[i]);
        var mesh = new Mesh { name = "Garten_Hoehenprofil", indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/Garten_Hoehenprofil.asset");
        AssetDatabase.CreateAsset(mesh, path);
        surface.AddComponent<MeshFilter>().sharedMesh = mesh;
        surface.AddComponent<MeshRenderer>().sharedMaterial = patches[0].sharedMaterial;
        surface.AddComponent<MeshCollider>().sharedMesh = mesh;
        Undo.RegisterCreatedObjectUndo(surface, "Garten-Höhenprofil ergänzen");
        foreach (var patch in patches)
        {
            Undo.RecordObject(patch.gameObject, "Ebene Originalfläche als Sicherung deaktivieren");
            patch.gameObject.SetActive(false);
        }
        Undo.CollapseUndoOperations(undo);
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Szene konnte nicht gespeichert werden.");

        Selection.activeGameObject = surface;
        if (SceneView.lastActiveSceneView)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0, 1, 0), Quaternion.Euler(30, 27, 0), 38);
        SceneView.RepaintAll();
        Debug.Log("Garten-Höhenprofil gespeichert: " + (high - low).ToString("F2")
            + " m Höhenunterschied, maximal " + (Mathf.Atan(maxGradient * factor) * Mathf.Rad2Deg).ToString("F1")
            + "° Neigung. Haus, Garage, Zufahrt und Treppenanschlüsse bleiben eben. Originalflächen deaktiviert erhalten.");
    }
}
