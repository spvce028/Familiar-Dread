using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using FamiliarDread;

/// <summary>Ergänzt einfache Garten-Platzhalter auf dem bestehenden Höhenprofil.</summary>
public static class VillaGardenDetails
{
    private static MeshCollider ground;
    private static Material wood, floor, stone, roof, leaves, leavesLight;

    private static float Ground(float x, float z)
    {
        RaycastHit hit;
        if (!ground.Raycast(new Ray(new Vector3(x, 60, z), Vector3.down), out hit, 120))
            throw new InvalidOperationException("Gartenboden fehlt bei " + x + ", " + z);
        return hit.point.y;
    }

    private static Transform Group(Transform parent, string name, Vector3 position)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = position;
        return t;
    }

    private static GameObject Shape(Transform parent, string name, Vector3 position, Vector3 size,
        Material material, PrimitiveType type = PrimitiveType.Cube, bool collision = true)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = material;
        if (!collision)
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    private static void Beam(Transform parent, string name, Vector3 a, Vector3 b, float width, Material material)
    {
        var g = Shape(parent, name, (a + b) / 2, new Vector3(width, Vector3.Distance(a, b), width), material);
        g.transform.up = (b - a).normalized;
    }

    private static void Fence(Transform parent, Vector2 from, Vector2 to)
    {
        int segments = Mathf.CeilToInt(Vector2.Distance(from, to) / 2.6f);
        Vector3 previous = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            Vector2 p = Vector2.Lerp(from, to, i / (float)segments);
            var basePoint = new Vector3(p.x, Ground(p.x, p.y), p.y);
            Shape(parent, "Zaunpfosten", basePoint + Vector3.up * .65f,
                new Vector3(.13f, 1.5f, .13f), wood);
            if (i > 0)
            {
                Beam(parent, "Zaunriegel_Unten", previous + Vector3.up * .43f,
                    basePoint + Vector3.up * .43f, .09f, wood);
                Beam(parent, "Zaunriegel_Oben", previous + Vector3.up * 1.08f,
                    basePoint + Vector3.up * 1.08f, .09f, wood);
            }
            previous = basePoint;
        }
    }

    private static void Tree(Transform parent, int index, Vector2 point, float height)
    {
        var t = Group(parent, "Baum_" + (index + 1).ToString("00"),
            new Vector3(point.x, Ground(point.x, point.y), point.y));
        Shape(t, "Stamm", new Vector3(0, height * .29f, 0), new Vector3(.55f, height * .31f, .55f),
            wood, PrimitiveType.Cylinder);
        Shape(t, "Krone_Mitte", new Vector3(0, height * .72f, 0),
            new Vector3(height * .57f, height * .58f, height * .54f), leaves, PrimitiveType.Sphere, false);
        Shape(t, "Krone_Seitlich", new Vector3(-height * .17f, height * .61f, height * .08f),
            new Vector3(height * .43f, height * .43f, height * .43f), leavesLight, PrimitiveType.Sphere, false);
        Shape(t, "Krone_Oben", new Vector3(height * .12f, height * .84f, -height * .06f),
            new Vector3(height * .39f, height * .4f, height * .4f), leaves, PrimitiveType.Sphere, false);
    }

    private static Mesh Gable()
    {
        Vector3[] points = {
            new Vector3(-2.5f, 0, -.06f), new Vector3(2.5f, 0, -.06f), new Vector3(0, .85f, -.06f),
            new Vector3(-2.5f, 0, .06f), new Vector3(2.5f, 0, .06f), new Vector3(0, .85f, .06f)
        };
        int[] faces = { 0,2,1, 3,4,5, 0,1,4, 0,4,3, 1,2,5, 1,5,4, 2,0,3, 2,3,5 };
        var vertices = faces.Select(i => points[i]).ToArray();
        var mesh = new Mesh { name = "Gartenhaus_Giebel" };
        mesh.vertices = vertices;
        mesh.triangles = Enumerable.Range(0, faces.Length).ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/VillaPrototype/Garten/Gartenhaus_Giebel.asset");
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void Shed(Transform parent)
    {
        const float x = 22, z = 16;
        float lowest = float.PositiveInfinity, highest = float.NegativeInfinity;
        foreach (float dx in new[] { -4.65f, 0, 4.65f })
        foreach (float dz in new[] { -4.15f, 0, 4.15f })
        {
            float y = Ground(x + dx, z + dz);
            lowest = Mathf.Min(lowest, y);
            highest = Mathf.Max(highest, y);
        }
        float level = highest + .08f;
        var shed = Group(parent, "Gartenhaus", new Vector3(x, level, z));
        float bottom = lowest - .15f;
        Shape(shed, "Fundament", new Vector3(0, (bottom + level - .18f) / 2 - level, 0),
            new Vector3(9.05f, level - .18f - bottom, 8.05f), stone);
        Shape(shed, "Boden", new Vector3(0, -.09f, 0), new Vector3(9, .18f, 8), floor);
        Shape(shed, "Rueckwand", new Vector3(0, 1.25f, 4), new Vector3(9, 2.5f, .12f), floor);
        foreach (int side in new[] { -1, 1 })
        {
            Shape(shed, "Seitenwand", new Vector3(side * 4.5f, 1.25f, 0), new Vector3(.12f, 2.5f, 8), floor);
            Shape(shed, "Frontwand", new Vector3(side * 2.55f, 1.25f, -4), new Vector3(3.9f, 2.5f, .12f), floor);
            Shape(shed, "Tuerrahmen_Seite", new Vector3(side * .6f, 1.1f, -4),
                new Vector3(.08f, 2.28f, .2f), wood);
        }
        Shape(shed, "Tuersturz", new Vector3(0, 2.355f, -4), new Vector3(1.2f, .29f, .12f), floor);
        Shape(shed, "Tuerrahmen_Oben", new Vector3(0, 2.235f, -4), new Vector3(1.36f, .07f, .2f), wood);
        var hinge = Group(shed, "Gartenhaustuer_E", shed.TransformPoint(new Vector3(-.6f, 0, -4)));
        hinge.gameObject.AddComponent<VillaDoor>().angle = -95;
        Shape(hinge, "Tuerblatt", new Vector3(.6f, 1.1f, 0), new Vector3(1.16f, 2.2f, .075f), wood);
        foreach (int side in new[] { -1, 1 })
            Shape(hinge, "Tuergriff", new Vector3(1, 1.05f, side * .1f), new Vector3(.16f, .04f, .16f), stone,
                PrimitiveType.Cube, false);

        float angle = Mathf.Atan2(.8972222f, 4.75f) * Mathf.Rad2Deg;
        foreach (int side in new[] { -1, 1 })
        {
            var panel = Shape(shed, "Satteldach", new Vector3(side * 2.375f, 2.9013889f, 0),
                new Vector3(Mathf.Sqrt(4.75f * 4.75f + .8972222f * .8972222f), .15f, 8.5f), roof);
            panel.transform.localRotation = Quaternion.Euler(0, 0, -side * angle);
        }
        var gable = Gable();
        foreach (int side in new[] { -1, 1 })
        {
            var g = Group(shed, "Holzgiebel", shed.TransformPoint(new Vector3(0, 2.5f, side * 4)));
            g.localScale = new Vector3(1.8f, 1, 1);
            g.gameObject.AddComponent<MeshFilter>().sharedMesh = gable;
            g.gameObject.AddComponent<MeshRenderer>().sharedMaterial = floor;
            g.gameObject.AddComponent<MeshCollider>().sharedMesh = gable;
        }

        // Kleine Rampe statt schwebender Türschwelle auf dem hügeligen Untergrund.
        var a = new Vector3(x, level, z - 4.1f);
        var b = new Vector3(x, Ground(x, z - 7.1f) + .04f, z - 7.1f);
        var ramp = Shape(parent, "Gartenhaus_Zugangsrampe", (a + b) / 2 - Vector3.up * .07f,
            new Vector3(1.5f, .14f, Vector3.Distance(a, b)), floor);
        ramp.transform.rotation = Quaternion.LookRotation((a - b).normalized, Vector3.up);
    }

    private static void ExtendPath(Transform parent, Renderer original, float endZ)
    {
        Bounds bounds = original.bounds;
        int count = Mathf.CeilToInt((bounds.min.z - endZ) / .5f);
        if (count <= 0) return;
        float length = (bounds.min.z - endZ) / count;
        for (int i = 0; i < count; i++)
        {
            float z = bounds.min.z - (i + .5f) * length;
            float y = Ground(bounds.center.x, z) + .005f;
            Shape(parent, original.name + "_Zum_Zaun", new Vector3(bounds.center.x, y, z),
                new Vector3(bounds.size.x, .08f, length + .01f), original.sharedMaterial);
        }
    }

    [MenuItem("Familiar Dread/Villa/Gartenzaun Bäume und Gartenhaus ergänzen")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Zuerst den Spielmodus beenden.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var villa = GameObject.Find("VILLA_PROTOTYPE");
        if (!villa || scene.path != "Assets/Scenes/VillaPrototype.unity")
            throw new InvalidOperationException("Bitte VillaPrototype öffnen.");
        var garden = villa.transform.Find("Garten_und_Zufahrt");
        if (!garden || garden.Find("Garten_Ausstattung"))
            throw new InvalidOperationException("Garten fehlt oder Ausstattung ist bereits vorhanden.");
        ground = garden.Find("Garten_Hoehenprofil").GetComponent<MeshCollider>();
        wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Dunkles_Holz.mat");
        floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Boden_Holz.mat");
        stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Stein.mat");
        roof = AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Dach_Schiefer.mat");
        if (!ground || !wood || !floor || !stone || !roof) throw new InvalidOperationException("Boden oder Material fehlt.");
        Physics.SyncTransforms();
        var drive = garden.Find("Zufahrt").GetComponent<Renderer>();
        var entry = garden.Find("Eingangsweg").GetComponent<Renderer>();
        Bounds yard = ground.bounds;
        float left = yard.min.x + .8f, right = yard.max.x - .8f;
        float front = yard.min.z + .8f, rear = yard.max.z - .8f;
        float driveLeft = drive.bounds.min.x - .5f, driveRight = drive.bounds.max.x + .5f;
        float entryLeft = entry.bounds.min.x - .4f, entryRight = entry.bounds.max.x + .4f;
        if (!(left < driveLeft && driveRight < entryLeft && entryRight < right))
            throw new InvalidOperationException("Zufahrt und Eingang für Zaunöffnungen prüfen.");

        Vector2[] trees = {
            new Vector2(-27,17), new Vector2(-28,1), new Vector2(-26,-20), new Vector2(-13,23),
            new Vector2(5,25), new Vector2(28,23), new Vector2(27,3), new Vector2(24,-18)
        };
        foreach (var point in trees) Ground(point.x, point.y);
        Ground(22,16);

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Garten mit einfachen Platzhaltern ausstatten");
        var root = Group(garden, "Garten_Ausstattung", Vector3.zero);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Gartenausstattung ergänzen");
        try
        {
            leaves = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Baumlaub_Dunkel" };
            leaves.color = new Color(.12f,.2f,.095f);
            leavesLight = new Material(leaves) { name = "Baumlaub_Hell" };
            leavesLight.color = new Color(.19f,.27f,.13f);
            AssetDatabase.CreateAsset(leaves, AssetDatabase.GenerateUniqueAssetPath("Assets/VillaPrototype/Garten/Baumlaub_Dunkel.mat"));
            AssetDatabase.CreateAsset(leavesLight, AssetDatabase.GenerateUniqueAssetPath("Assets/VillaPrototype/Garten/Baumlaub_Hell.mat"));
            var fence = Group(root, "Gartenzaun", Vector3.zero);
            Fence(fence, new Vector2(left,front), new Vector2(left,rear));
            Fence(fence, new Vector2(left,rear), new Vector2(right,rear));
            Fence(fence, new Vector2(right,rear), new Vector2(right,front));
            Fence(fence, new Vector2(left,front), new Vector2(driveLeft,front));
            Fence(fence, new Vector2(driveRight,front), new Vector2(entryLeft,front));
            Fence(fence, new Vector2(entryRight,front), new Vector2(right,front));
            var treeGroup = Group(root, "Baeume", Vector3.zero);
            for (int i = 0; i < trees.Length; i++) Tree(treeGroup, i, trees[i], 6.8f + (i % 4) * .7f);
            Shed(root);
            var paths = Group(root, "Wege_Zum_Zaun", Vector3.zero);
            ExtendPath(paths, drive, front - .35f);
            ExtendPath(paths, entry, front - .35f);
            Physics.SyncTransforms();
            Undo.CollapseUndoOperations(undo);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Szene konnte nicht gespeichert werden.");
        }
        catch
        {
            Undo.RevertAllDownToGroup(undo);
            throw;
        }
        Selection.activeGameObject = root.gameObject;
        if (SceneView.lastActiveSceneView)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0,2,0), Quaternion.Euler(32,25,0), 39);
        SceneView.RepaintAll();
        Debug.Log("Garten ausgestattet und gespeichert: geländeangepasster Holzzaun mit freien Zugängen, "
            + "8 Bäume und begehbares Gartenhaus mit E-Tür und Zugangsrampe. Bestehende Gebäude, Böden und Gelände unverändert.");
    }
}
