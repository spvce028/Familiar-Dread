using System;
using System.Collections.Generic;
using System.Linq;
using FamiliarDread;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Ergänzt den unterirdischen Geheimraum, ohne Haus oder Garten neu zu erzeugen.</summary>
public static class VillaSecretRoom
{
    private const float FloorY = -3.2f;
    private const float CeilingY = -.8f;
    private static Transform root;
    private static Material stone, wood, iron;
    // Der Schacht liegt im hinteren linken Bereich des Gartenhauses.
    private static readonly Rect Shaft = Rect.MinMaxRect(18.8f, 17.3f, 20.2f, 18.5f);

    private static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    private static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material material)
    {
        if (size.x <= .0001f || size.y <= .0001f || size.z <= .0001f)
            throw new InvalidOperationException("Ungültige Abmessung: " + name);
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.position = center;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = material;
        return g;
    }

    private static void Slab(Transform parent, string name, Rect area, float bottom, float top, Material material)
    {
        Box(parent, name, new Vector3(area.center.x, (bottom + top) / 2, area.center.y),
            new Vector3(area.width, top - bottom, area.height), material);
    }

    /// <summary>Vier rechteckige Reststücke lassen einen echten, kollisionsfreien Durchstieg frei.</summary>
    private static void SlabWithHole(Transform parent, string name, Bounds bounds, Material material)
    {
        if (Shaft.xMin <= bounds.min.x || Shaft.xMax >= bounds.max.x
            || Shaft.yMin <= bounds.min.z || Shaft.yMax >= bounds.max.z)
            throw new InvalidOperationException("Schacht liegt außerhalb von " + name);
        Slab(parent, name + "_Links", Rect.MinMaxRect(bounds.min.x, bounds.min.z, Shaft.xMin, bounds.max.z), bounds.min.y, bounds.max.y, material);
        Slab(parent, name + "_Rechts", Rect.MinMaxRect(Shaft.xMax, bounds.min.z, bounds.max.x, bounds.max.z), bounds.min.y, bounds.max.y, material);
        Slab(parent, name + "_Vorne", Rect.MinMaxRect(Shaft.xMin, bounds.min.z, Shaft.xMax, Shaft.yMin), bounds.min.y, bounds.max.y, material);
        Slab(parent, name + "_Hinten", Rect.MinMaxRect(Shaft.xMin, Shaft.yMax, Shaft.xMax, bounds.max.z), bounds.min.y, bounds.max.y, material);
    }

    private static void Wall(Transform parent, string name, Vector2 a, Vector2 b, float bottom = FloorY, float top = CeilingY)
    {
        Vector2 d = b - a;
        var g = Box(parent, name, new Vector3((a.x + b.x) / 2, (bottom + top) / 2, (a.y + b.y) / 2),
            new Vector3(.2f, top - bottom, d.magnitude), stone);
        g.transform.rotation = Quaternion.LookRotation(new Vector3(d.x, 0, d.y));
    }

    private static Transform Point(Transform parent, string name, Vector3 position)
    {
        var point = Group(parent, name);
        point.position = position;
        return point;
    }

    // Beim Ausschneiden werden Positionen, Normalen und UVs interpoliert. Der Garten außerhalb
    // des Schachts behält dadurch sein bisheriges Höhenprofil und seine Oberfläche.
    private struct Vertex
    {
        public Vector3 position, normal;
        public Vector2 uv;
        public static Vertex Lerp(Vertex a, Vertex b, float t)
        {
            return new Vertex { position = Vector3.Lerp(a.position, b.position, t),
                normal = Vector3.Lerp(a.normal, b.normal, t).normalized, uv = Vector2.Lerp(a.uv, b.uv, t) };
        }
    }

    private static List<Vertex> Clip(List<Vertex> polygon, Func<Vertex, float> distance, bool inside)
    {
        var result = new List<Vertex>();
        for (int i = 0; i < polygon.Count; i++)
        {
            Vertex a = polygon[i], b = polygon[(i + 1) % polygon.Count];
            float da = distance(a), db = distance(b);
            bool keepA = inside ? da >= 0 : da <= 0;
            bool keepB = inside ? db >= 0 : db <= 0;
            if (keepA) result.Add(a);
            if (keepA != keepB) result.Add(Vertex.Lerp(a, b, da / (da - db)));
        }
        return result;
    }

    private static Mesh CutGround(MeshFilter filter)
    {
        Mesh original = filter.sharedMesh;
        var positions = original.vertices;
        var normals = original.normals;
        var uv = original.uv;
        var triangles = original.triangles;
        var output = new List<Vertex>();
        var indices = new List<int>();
        Action<List<Vertex>> emit = polygon =>
        {
            for (int j = 1; j + 1 < polygon.Count; j++)
            {
                if (Vector3.Cross(polygon[j].position - polygon[0].position,
                    polygon[j + 1].position - polygon[0].position).sqrMagnitude < 1e-12f) continue;
                foreach (var vertex in new[] { polygon[0], polygon[j], polygon[j + 1] })
                {
                    indices.Add(output.Count);
                    output.Add(vertex);
                }
            }
        };
        Func<Vertex, float>[] edges = {
            v => filter.transform.TransformPoint(v.position).x - Shaft.xMin,
            v => Shaft.xMax - filter.transform.TransformPoint(v.position).x,
            v => filter.transform.TransformPoint(v.position).z - Shaft.yMin,
            v => Shaft.yMax - filter.transform.TransformPoint(v.position).z
        };
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var polygon = new List<Vertex>();
            for (int j = 0; j < 3; j++)
            {
                int k = triangles[i + j];
                polygon.Add(new Vertex { position = positions[k], normal = normals[k], uv = uv[k] });
            }
            // Je Ebene den äußeren Teil behalten; nur der innere Rest wird weiter zerlegt.
            foreach (var edge in edges)
            {
                emit(Clip(polygon, edge, false));
                polygon = Clip(polygon, edge, true);
                if (polygon.Count == 0) break;
            }
        }
        var mesh = new Mesh { name = "Garten_Hoehenprofil_Mit_Geheimschacht", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(output.Select(v => v.position).ToList());
        mesh.SetNormals(output.Select(v => v.normal).ToList());
        mesh.SetUVs(0, output.Select(v => v.uv).ToList());
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddLadder(float shedLevel)
    {
        var group = Group(root, "Gartenhaus_Leiterzugang");
        var ladder = group.gameObject.AddComponent<VillaLadder>();
        const float x = 19.5f, z = 18;
        ladder.lowerExit = Point(group, "Unten_Austritt", new Vector3(x, FloorY + .03f, z + .65f));
        ladder.lowerClimb = Point(group, "Unten_Leiter", new Vector3(x, FloorY + .03f, z));
        ladder.upperClimb = Point(group, "Oben_Leiter", new Vector3(x, shedLevel + .03f, z));
        ladder.upperExit = Point(group, "Oben_Austritt", new Vector3(x, shedLevel + .03f, z - 1.2f));

        float height = shedLevel - FloorY;
        foreach (float side in new[] { -1f, 1f })
            Box(group, "Leiter_Holm", new Vector3(x + side * .45f, FloorY + height / 2, z - .45f),
                new Vector3(.06f, height, .08f), iron);
        int steps = Mathf.CeilToInt(height / .28f);
        for (int i = 0; i < steps; i++)
            Box(group, "Leiter_Sprosse", new Vector3(x, FloorY + .2f + i * (height - .3f) / (steps - 1), z - .45f),
                new Vector3(.84f, .045f, .08f), iron);

        // Schachtwände reichen bis unter den Holzboden. Oberhalb bleibt der Austritt frei.
        float top = shedLevel - .18f;
        Wall(group, "Schacht_Links", new Vector2(Shaft.xMin - .1f, Shaft.yMin - .2f), new Vector2(Shaft.xMin - .1f, Shaft.yMax + .2f), CeilingY, top);
        Wall(group, "Schacht_Rechts", new Vector2(Shaft.xMax + .1f, Shaft.yMin - .2f), new Vector2(Shaft.xMax + .1f, Shaft.yMax + .2f), CeilingY, top);
        Wall(group, "Schacht_Vorne", new Vector2(Shaft.xMin, Shaft.yMin - .1f), new Vector2(Shaft.xMax, Shaft.yMin - .1f), CeilingY, top);
        Wall(group, "Schacht_Hinten", new Vector2(Shaft.xMin, Shaft.yMax + .1f), new Vector2(Shaft.xMax, Shaft.yMax + .1f), CeilingY, top);

        foreach (float edge in new[] { Shaft.xMin, Shaft.xMax })
            Box(group, "Lukenrahmen_Seite", new Vector3(edge - (edge < x ? .04f : -.04f), shedLevel + .02f, Shaft.center.y),
                new Vector3(.08f, .06f, Shaft.height + .16f), wood);
        foreach (float edge in new[] { Shaft.yMin, Shaft.yMax })
            Box(group, "Lukenrahmen_Quer", new Vector3(x, shedLevel + .02f, edge - (edge < z ? .04f : -.04f)),
                new Vector3(Shaft.width, .06f, .08f), wood);

        var pivot = Point(group, "Luke_Scharnier", new Vector3(Shaft.xMin, shedLevel, Shaft.center.y));
        ladder.hatchPivot = pivot;
        var hatch = Box(group, "Gartenhausluke_E", new Vector3(x, shedLevel - .05f, Shaft.center.y),
            new Vector3(Shaft.width - .02f, .1f, Shaft.height - .02f), wood);
        hatch.transform.SetParent(pivot, true);
        ladder.hatchCollider = hatch.GetComponent<Collider>();
        var handle = Box(group, "Lukengriff", new Vector3(Shaft.xMax - .2f, shedLevel + .035f, Shaft.center.y),
            new Vector3(.15f, .04f, .08f), iron);
        handle.transform.SetParent(hatch.transform, true);
        UnityEngine.Object.DestroyImmediate(handle.GetComponent<Collider>());

        // Drei Schutzseiten, der vordere Austritt in Richtung Gartenhaustür bleibt offen.
        foreach (float edge in new[] { Shaft.xMin - .12f, Shaft.xMax + .12f })
        {
            Box(group, "Schachtgeländer_Handlauf", new Vector3(edge, shedLevel + .95f, Shaft.center.y), new Vector3(.06f, .06f, Shaft.height + .25f), wood);
            foreach (float end in new[] { Shaft.yMin - .05f, Shaft.yMax + .12f })
                Box(group, "Schachtgeländer_Pfosten", new Vector3(edge, shedLevel + .475f, end), new Vector3(.06f, .95f, .06f), iron);
        }
        Box(group, "Schachtgeländer_Hinten", new Vector3(x, shedLevel + .95f, Shaft.yMax + .12f), new Vector3(Shaft.width + .3f, .06f, .06f), wood);
    }

    private static void Lamp(Transform parent, Vector3 position)
    {
        var fixture = Group(parent, "Deckenlampe");
        Box(fixture, "Halterung", position + Vector3.up * .1f, new Vector3(.1f, .2f, .1f), iron);
        Box(fixture, "Leuchte", position, new Vector3(.35f, .08f, .25f),
            AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Leuchte.mat"));
        var point = Point(fixture, "Raumlicht", position + Vector3.down * .15f);
        var light = point.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1, .78f, .52f);
        light.intensity = 1.2f;
        light.range = 6;
        light.shadows = LightShadows.Soft;
    }

    [MenuItem("Familiar Dread/Villa/Geheimraum zwischen Keller und Gartenhaus bauen")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/VillaPrototype.unity")
            throw new InvalidOperationException("VillaPrototype außerhalb des Spielmodus öffnen.");
        var villa = GameObject.Find("VILLA_PROTOTYPE");
        if (!villa || villa.transform.Find("Geheimraum_Verbindung"))
            throw new InvalidOperationException("Villa fehlt oder Geheimraum ist bereits vorhanden.");
        var basement = villa.transform.Find("Keller");
        var shed = villa.transform.Find("Garten_und_Zufahrt/Garten_Ausstattung/Gartenhaus");
        var ground = villa.transform.Find("Garten_und_Zufahrt/Garten_Hoehenprofil");
        if (!basement || !shed || !ground) throw new InvalidOperationException("Ausgangsgeometrie fehlt.");
        var rear = basement.GetComponentsInChildren<Renderer>().Single(r => r.name == "Kellerwand_Wand" && Mathf.Abs(r.bounds.center.z - 8) < .05f);
        var shedFloor = shed.Find("Boden").GetComponent<Renderer>();
        var foundation = shed.Find("Fundament").GetComponent<Renderer>();
        float shedLevel = shedFloor.bounds.max.y;
        var filter = ground.GetComponent<MeshFilter>();
        var groundCollider = ground.GetComponent<MeshCollider>();
        if (!filter || !groundCollider || shedFloor.bounds.min.x > Shaft.xMin || shedFloor.bounds.max.x < Shaft.xMax)
            throw new InvalidOperationException("Gartenhaus oder Boden wurde verschoben; Schachtposition prüfen.");
        stone = rear.sharedMaterial;
        wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Dunkles_Holz.mat");
        iron = AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Eisen.mat");
        // Vor dem Umbau kontrollieren, dass die unterirdische Decke nicht über das Gelände ragt.
        Physics.SyncTransforms();
        foreach (var point in new[] { new Vector2(6, 9), new Vector2(6, 15.6f), new Vector2(12, 15.6f), new Vector2(18, 12), new Vector2(26.5f, 20.5f) })
        {
            RaycastHit hit;
            if (!groundCollider.Raycast(new Ray(new Vector3(point.x, 30, point.y), Vector3.down), out hit, 60) || hit.point.y < CeilingY + .25f)
                throw new InvalidOperationException("Zu wenig Erdüberdeckung bei " + point);
        }

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Geheime Keller-Gartenhaus-Verbindung");
        string meshPath = null;
        try
        {
            root = Group(villa.transform, "Geheimraum_Verbindung");
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Geheimraum ergänzen");
            var passage = Group(root, "Geheimgang");
            var room = Group(root, "Geheimraum");
            Rect first = Rect.MinMaxRect(4.8f, 8.175f, 7.2f, 16.8f);
            Rect second = Rect.MinMaxRect(7.2f, 14.4f, 17.5f, 16.8f);
            Rect roomArea = Rect.MinMaxRect(17.5f, 12, 26.5f, 20.5f);
            foreach (var area in new[] { first, second })
            {
                Slab(passage, "Gangboden", area, FloorY - .2f, FloorY, stone);
                Slab(passage, "Gangdecke", area, CeilingY, CeilingY + .18f, stone);
            }
            Slab(room, "Boden", roomArea, FloorY - .2f, FloorY, stone);
            SlabWithHole(room, "Decke", new Bounds(new Vector3(roomArea.center.x, CeilingY + .09f, roomArea.center.y), new Vector3(roomArea.width, .18f, roomArea.height)), stone);
            Wall(passage, "Gang_Links", new Vector2(4.8f, 8.175f), new Vector2(4.8f, 16.9f));
            Wall(passage, "Gang_Rechts", new Vector2(7.2f, 8.175f), new Vector2(7.2f, 14.4f));
            Wall(passage, "Gang_Hinten", new Vector2(4.7f, 16.8f), new Vector2(17.5f, 16.8f));
            Wall(passage, "Gang_Vorne", new Vector2(7.1f, 14.4f), new Vector2(17.5f, 14.4f));
            Wall(room, "Raum_Links_Vorne", new Vector2(17.5f, 11.9f), new Vector2(17.5f, 14.4f));
            Wall(room, "Raum_Links_Hinten", new Vector2(17.5f, 16.8f), new Vector2(17.5f, 20.6f));
            Wall(room, "Raum_Vorne", new Vector2(17.4f, 12), new Vector2(26.6f, 12));
            Wall(room, "Raum_Rechts", new Vector2(26.5f, 12), new Vector2(26.5f, 20.5f));
            Wall(room, "Raum_Hinten", new Vector2(17.4f, 20.5f), new Vector2(26.6f, 20.5f));

            // Die Originalwand bleibt als deaktivierte Sicherung erhalten. Keine Holzrahmen oder Griffe:
            // Das Türblatt setzt geschlossen die steinerne Kellerwand bündig fort.
            var entry = Group(root, "Keller_Geheimtuer");
            Bounds b = rear.bounds;
            Box(entry, "Wand_Links", new Vector3((b.min.x + 5.3f) / 2, b.center.y, b.center.z), new Vector3(5.3f - b.min.x, b.size.y, b.size.z), stone);
            Box(entry, "Wand_Rechts", new Vector3((6.7f + b.max.x) / 2, b.center.y, b.center.z), new Vector3(b.max.x - 6.7f, b.size.y, b.size.z), stone);
            Box(entry, "Wand_Sturz", new Vector3(6, (-.9f + b.max.y) / 2, b.center.z), new Vector3(1.4f, b.max.y + .9f, b.size.z), stone);
            var hinge = Point(entry, "Geheimtuer_Scharnier", new Vector3(5.3f, FloorY, b.center.z));
            var door = hinge.gameObject.AddComponent<VillaDoor>();
            door.angle = -95;
            door.hideInteractionHint = true;
            var leaf = Box(entry, "Getarntes_Wandelement", new Vector3(6, FloorY + 1.15f, b.center.z), new Vector3(1.4f, 2.3f, b.size.z), stone);
            leaf.transform.SetParent(hinge, true);
            // Kleine Auflage verbindet den bestehenden Kellerboden über die frühere Wandstärke mit dem Gang.
            Slab(entry, "Tuerschwelle", Rect.MinMaxRect(5.3f, b.min.z, 6.7f, 8.175f), FloorY - .2f, FloorY, stone);
            Undo.RecordObject(rear.gameObject, "Original-Kellerwand aufbewahren");
            rear.gameObject.SetActive(false);

            var opening = Group(root, "Gartenhaus_Bodendurchstieg");
            foreach (var original in new[] { shedFloor, foundation })
            {
                SlabWithHole(opening, original.name, original.bounds, original.sharedMaterial);
                Undo.RecordObject(original.gameObject, "Original-Gartenhausbauteil aufbewahren");
                original.gameObject.SetActive(false);
            }
            Mesh mesh = CutGround(filter);
            meshPath = AssetDatabase.GenerateUniqueAssetPath("Assets/VillaPrototype/Garten/Garten_Hoehenprofil_Mit_Geheimschacht.asset");
            AssetDatabase.CreateAsset(mesh, meshPath);
            Undo.RecordObjects(new UnityEngine.Object[] { filter, groundCollider }, "Gartenöffnung ausschneiden");
            filter.sharedMesh = mesh;
            groundCollider.sharedMesh = mesh;
            AddLadder(shedLevel);
            Lamp(passage, new Vector3(6, CeilingY - .12f, 11));
            Lamp(passage, new Vector3(12, CeilingY - .12f, 15.6f));
            Lamp(room, new Vector3(22, CeilingY - .12f, 16));
            Physics.SyncTransforms();
            Validate();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Szene konnte nicht gespeichert werden.");
            Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject = room.gameObject;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.LookAt(new Vector3(22, -2, 16), Quaternion.Euler(0, 90, 0), 3);
            Debug.Log("Geheimraum gespeichert: 9 × 8,5 m, 2,4 m Höhe; Geheimgang, getarnte Kellertür, Gartenhausluke und W/S-Leiter. Geometrieprüfung bestanden.");
        }
        catch
        {
            Undo.RevertAllDownToGroup(undo);
            if (meshPath != null) AssetDatabase.DeleteAsset(meshPath);
            throw;
        }
    }

    /// <summary>Prüft lichte Wege, Türschwenkraum und den gesamten Leiterweg einschließlich oberem Ausstieg.</summary>
    [MenuItem("Familiar Dread/Villa/Geheimraum-Verbindung prüfen")]
    public static void Validate()
    {
        var villa = GameObject.Find("VILLA_PROTOTYPE");
        var connection = villa.transform.Find("Geheimraum_Verbindung");
        if (!connection) throw new InvalidOperationException("Geheimraum fehlt.");
        var door = connection.Find("Keller_Geheimtuer/Geheimtuer_Scharnier");
        var ladder = connection.Find("Gartenhaus_Leiterzugang").GetComponent<VillaLadder>();
        // Öffnungsstellungen nur für die Prüfung simulieren und danach sicher wiederherstellen.
        Quaternion doorRotation = door.localRotation, hatchRotation = ladder.hatchPivot.localRotation;
        try
        {
            door.localRotation = doorRotation * Quaternion.Euler(0, -95, 0);
            ladder.hatchPivot.localRotation = hatchRotation * Quaternion.Euler(0, 0, 85);
            Physics.SyncTransforms();
            var points = new List<Vector3>();
            Action<Vector3, Vector3> path = (a, b) =>
            {
                int count = Mathf.CeilToInt(Vector3.Distance(a, b) / .15f);
                for (int i = 0; i <= count; i++) points.Add(Vector3.Lerp(a, b, i / (float)count));
            };
            path(new Vector3(6, FloorY + .03f, 7), new Vector3(6, FloorY + .03f, 15.6f));
            path(new Vector3(6, FloorY + .03f, 15.6f), new Vector3(22, FloorY + .03f, 15.6f));
            path(new Vector3(22, FloorY + .03f, 15.6f), new Vector3(22, FloorY + .03f, 18));
            path(new Vector3(22, FloorY + .03f, 18), new Vector3(19.5f, FloorY + .03f, 18));
            path(ladder.lowerClimb.position, ladder.upperClimb.position);
            path(ladder.upperClimb.position, ladder.upperExit.position);
            path(ladder.upperExit.position, new Vector3(22, ladder.upperExit.position.y, 13));
            foreach (var feet in points)
            {
                var hits = Physics.OverlapCapsule(feet + Vector3.up * .27f, feet + Vector3.up * 1.13f, .24f,
                    ~0, QueryTriggerInteraction.Ignore).Where(c => !(c is CharacterController)).ToArray();
                if (hits.Length != 0)
                    throw new InvalidOperationException("Weg blockiert bei " + feet + ": " + string.Join(", ", hits.Select(h => h.name)));
            }
            bool fromTop;
            if (!ladder.CanGrab(ladder.lowerClimb.position, 1, out fromTop) || fromTop)
                throw new InvalidOperationException("Untere Leiteraufnahme fehlt.");
            RaycastHit groundHit;
            var garden = villa.transform.Find("Garten_und_Zufahrt/Garten_Hoehenprofil").GetComponent<MeshCollider>();
            if (garden.Raycast(new Ray(new Vector3(19.5f, 30, 18), Vector3.down), out groundHit, 60))
                throw new InvalidOperationException("Gartenboden versperrt den Schacht.");
            CheckClimbing(ladder);
            Debug.Log("Geheimraumprüfung: " + points.Count + " Spielerpositionen kollisionsfrei; Leiteraufnahme und Geländeöffnung korrekt.");
        }
        finally
        {
            door.localRotation = doorRotation;
            ladder.hatchPivot.localRotation = hatchRotation;
            Physics.SyncTransforms();
        }
    }

    /// <summary>Verwendet dieselbe CharacterController-Bewegung wie im Spiel und entfernt die Prüffigur danach.</summary>
    private static void CheckClimbing(VillaLadder ladder)
    {
        var probe = new GameObject("Geheimraum_Prueffigur") { hideFlags = HideFlags.HideAndDontSave };
        var serialized = new SerializedObject(ladder);
        bool wasOpen = serialized.FindProperty("open").boolValue;
        bool registered = VillaLadder.Active.Contains(ladder);
        try
        {
            serialized.FindProperty("open").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!registered) VillaLadder.Active.Add(ladder);
            probe.transform.position = ladder.lowerClimb.position;
            var body = probe.AddComponent<CharacterController>();
            body.height = 1.4f;
            body.radius = .25f;
            body.center = Vector3.up * .7f;
            body.skinWidth = .03f;
            body.stepOffset = .2f;
            var camera = new GameObject("Pruefkamera") { hideFlags = HideFlags.HideAndDontSave };
            camera.transform.SetParent(probe.transform, false);
            camera.transform.localPosition = Vector3.up * 1.25f;
            var explorer = probe.AddComponent<VillaExplorer>();
            explorer.view = camera.AddComponent<Camera>();
            explorer.view.enabled = false;
            Physics.SyncTransforms();

            explorer.StepMovement(Vector2.up, .02f);
            if (!explorer.IsClimbing) throw new InvalidOperationException("Leiter lässt sich unten nicht betreten.");
            // A/D dürfen keinen seitlichen Ausstieg ermöglichen; ohne W/S muss die Figur halten.
            Vector3 held = probe.transform.position;
            explorer.StepMovement(Vector2.right, .1f);
            if (!explorer.IsClimbing || Vector3.Distance(held, probe.transform.position) > .001f)
                throw new InvalidOperationException("Leiterbindung bei A/D fehlerhaft.");
            for (int i = 0; i < 600 && explorer.IsClimbing; i++) explorer.StepMovement(Vector2.up, .02f);
            if (explorer.IsClimbing || Vector3.Distance(probe.transform.position, ladder.upperExit.position) > .2f)
                throw new InvalidOperationException("Aufstieg oder oberer Ausstieg blockiert: " + probe.transform.position);
            // Kurze Wiedereinstiegssperre ohne Bewegung verstreichen lassen.
            for (int i = 0; i < 20; i++) explorer.StepMovement(Vector2.zero, .02f);
            explorer.StepMovement(Vector2.down, .02f);
            if (!explorer.IsClimbing) throw new InvalidOperationException("Leiter lässt sich oben nicht betreten.");
            for (int i = 0; i < 600 && explorer.IsClimbing; i++) explorer.StepMovement(Vector2.down, .02f);
            if (explorer.IsClimbing || Mathf.Abs(probe.transform.position.y - ladder.lowerExit.position.y) > .12f)
                throw new InvalidOperationException("Abstieg oder unterer Ausstieg blockiert: " + probe.transform.position);
            Debug.Log("Geheimraum-Leiter: W-Aufstieg, S-Abstieg, oberer Ausstieg sowie A/D-Sperre mit echter Spielerbewegung bestanden.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
            if (!registered) VillaLadder.Active.Remove(ladder);
            serialized.Update();
            serialized.FindProperty("open").boolValue = wasOpen;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();
        }
    }
}
