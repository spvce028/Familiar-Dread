using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Verbindet die bestehenden Treppenhandläufe mit Podest- und Brüstungsgeländern.</summary>
public static class VillaRailingJoints
{
    private struct Rail
    {
        public Transform transform;
        public Vector3 a, b;
        public float width;
    }

    private static Rail Read(Transform t)
    {
        Vector3 half = t.up * t.lossyScale.y / 2;
        return new Rail { transform = t, a = t.position - half, b = t.position + half, width = t.lossyScale.x };
    }

    private static void Fit(Transform t, Vector3 a, Vector3 b, float width)
    {
        t.position = (a + b) / 2;
        t.up = (b - a).normalized;
        t.localScale = new Vector3(width, Vector3.Distance(a, b), width);
    }

    private static Vector3 ClosestXZ(Vector3 point, Rail rail)
    {
        Vector3 direction = rail.b - rail.a;
        Vector3 offset = point - rail.a;
        direction.y = 0;
        offset.y = 0;
        float fraction = direction.sqrMagnitude > .000001f
            ? Mathf.Clamp01(Vector3.Dot(offset, direction) / direction.sqrMagnitude) : 0;
        return Vector3.Lerp(rail.a, rail.b, fraction);
    }

    private static GameObject Beam(Transform parent, string name, Vector3 a, Vector3 b, float width, Material material)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(parent, false);
        Fit(g.transform, a, b, width);
        g.GetComponent<Renderer>().sharedMaterial = material;
        return g;
    }

    [MenuItem("Familiar Dread/Villa/Treppengeländer bündig anschließen")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Zuerst den Spielmodus beenden.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var villa = GameObject.Find("VILLA_PROTOTYPE");
        if (!villa || scene.path != "Assets/Scenes/VillaPrototype.unity")
            throw new InvalidOperationException("Bitte VillaPrototype öffnen.");

        var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Dunkles_Holz.mat");
        var iron = AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Eisen.mat");
        if (!wood || !iron)
            throw new InvalidOperationException("Geländermaterialien fehlen.");

        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Treppengeländer bündig anschließen");
        var old = villa.transform.Find("Gelaender_Anschluesse");
        if (old)
            Undo.DestroyObjectImmediate(old.gameObject);

        // Die Haupttreppe auf dieselbe Höhe und dasselbe Holzprofil wie ihre Brüstungen bringen.
        // X/Z bleiben erhalten: Pfosten und Handläufe bleiben an den vorhandenen Treppenrändern.
        var main = villa.transform.Find("Haupttreppe_Kompakt");
        if (!main)
            throw new InvalidOperationException("Kompakte Haupttreppe fehlt.");
        foreach (Transform flight in main)
        {
            if (!flight.name.StartsWith("Haupttreppe_L_", StringComparison.Ordinal))
                continue;
            // Den Startboden aus der ersten Stufe bestimmen; ein separates Antritt-Objekt ist nicht erforderlich.
            var firstStep = flight.Find("Lauf_1_Stufe_1");
            if (!firstStep || !firstStep.GetComponent<Renderer>())
                throw new InvalidOperationException("Erste Treppenstufe fehlt: " + flight.name);
            float floor = firstStep.GetComponent<Renderer>().bounds.max.y - 1.6f / 9;
            foreach (Transform child in flight)
            {
                if (!child.name.Contains("Handlauf"))
                    continue;
                var rail = Read(child);
                if (child.name == "Lauf_1_Handlauf" || child.name == "Lauf_2_Handlauf")
                {
                    if (rail.a.y > rail.b.y) { var swap = rail.a; rail.a = rail.b; rail.b = swap; }
                    bool first = child.name == "Lauf_1_Handlauf";
                    rail.a.y = floor + (first ? 1 : 2.6f);
                    rail.b.y = floor + (first ? 2.6f : 4.2f);
                }
                else
                {
                    rail.a.y = floor + 2.6f;
                    rail.b.y = floor + 2.6f;
                }
                Undo.RecordObject(child, "Handlauf an Brüstungshöhe angleichen");
                Fit(child, rail.a, rail.b, .065f);
            }
        }

        var rails = new List<Rail>();
        var posts = new List<Transform>();
        foreach (var t in villa.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains("Handlauf") && t.GetComponent<MeshRenderer>())
                rails.Add(Read(t));
            if (t.name.EndsWith("_Stab", StringComparison.Ordinal) && t.GetComponent<MeshRenderer>())
                posts.Add(t);
        }

        // Pfosten bis in den zugehörigen Handlauf führen; ihre Fußpunkte bleiben an Ort und Stelle.
        foreach (var post in posts)
        {
            Rail? nearest = null;
            float best = .13f;
            foreach (var rail in rails)
            {
                if (rail.transform.parent != post.parent)
                    continue;
                string prefix = post.name.Substring(0, post.name.Length - 5);
                if (!rail.transform.name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                Vector3 point = ClosestXZ(post.position, rail);
                Vector3 offset = point - post.position;
                offset.y = 0;
                if (offset.magnitude < best) { best = offset.magnitude; nearest = rail; }
            }
            if (!nearest.HasValue)
                continue;
            Vector3 bottom = post.position - Vector3.up * post.lossyScale.y / 2;
            Vector3 top = bottom;
            top.y = ClosestXZ(bottom, nearest.Value).y;
            if (top.y <= bottom.y)
                continue;
            Undo.RecordObject(post, "Pfosten bis zum Handlauf führen");
            Fit(post, bottom, top, post.lossyScale.x);
        }

        var group = new GameObject("Gelaender_Anschluesse");
        group.transform.SetParent(villa.transform, false);
        var joined = new List<Vector3>();
        int connections = 0, endPosts = 0;
        foreach (var rail in rails)
        {
            if (Mathf.Abs(rail.a.y - rail.b.y) < .1f)
                continue;
            foreach (var end in new[] { rail.a, rail.b })
            {
                Vector3 target = end;
                float best = .4f;
                bool found = false;
                // Auf dem Podest quer zum Treppenrand zum vorhandenen Brüstungshandlauf anschließen.
                foreach (var flat in rails)
                {
                    if (Mathf.Abs(flat.a.y - flat.b.y) > .01f)
                        continue;
                    Vector3 point = ClosestXZ(end, flat);
                    float distance = Vector3.Distance(end, point);
                    if (Mathf.Abs(point.y - end.y) <= .08f && distance < best)
                    {
                        target = point;
                        best = distance;
                        found = true;
                    }
                }
                // Am inneren 90°-Knick treffen zwei schräge Handläufe aufeinander.
                if (!found)
                {
                    best = .16f;
                    foreach (var other in rails)
                    {
                        if (other.transform == rail.transform || other.transform.parent != rail.transform.parent)
                            continue;
                        foreach (var point in new[] { other.a, other.b })
                        {
                            float distance = Vector3.Distance(end, point);
                            if (Mathf.Abs(point.y - end.y) < .01f && distance < best)
                            { target = point; best = distance; found = true; }
                        }
                    }
                }
                bool duplicate = joined.Exists(p => Vector3.Distance(p, (end + target) / 2) < .005f);
                if (found && best > .003f && !duplicate)
                {
                    float width = rail.width;
                    // Kleiner Überstand liegt im vorhandenen Holz und vermeidet sichtbare Endfugen.
                    Vector3 inset = (target - end).normalized * .012f;
                    Beam(group.transform, "Handlauf_Anschluss", end - inset, target + inset, width, wood);
                    joined.Add((end + target) / 2);
                    connections++;
                }

                // Bei den neun Stufen fehlte am oberen Laufende bisher ein eigener Endpfosten.
                bool supported = posts.Exists(p => {
                    Vector3 delta = p.position - end; delta.y = 0;
                    return p.parent == rail.transform.parent && delta.magnitude < .06f;
                });
                if (!supported && rail.transform.parent.name.StartsWith("Haupttreppe_L_", StringComparison.Ordinal))
                {
                    Vector3 bottom = end - Vector3.up;
                    Beam(group.transform, "Treppen_Endpfosten", bottom, end, .04f, iron);
                    endPosts++;
                }
            }
        }

        Undo.RegisterCreatedObjectUndo(group, "Geländeranschlüsse ergänzen");
        Undo.CollapseUndoOperations(undo);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Szene konnte nicht gespeichert werden.");
        SceneView.RepaintAll();
        Debug.Log("Treppengeländer: " + connections + " bündige Anschlüsse und " + endPosts
            + " Endpfosten ergänzt; Haupttreppenprofil und Pfosten angepasst, Szene gespeichert.");
    }
}
