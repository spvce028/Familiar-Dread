using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class VillaPolish {
 const string AssetsRoot="Assets/VillaPrototype";
 class FixturePlan {
 public string level,label;public Vector2 from,to;public bool add;
 public FixturePlan(string level,string label,float fx,float fz,float tx,float tz,bool add=false){this.level=level;this.label=label;from=new Vector2(fx,fz);to=new Vector2(tx,tz);this.add=add;}
 }
 static FixturePlan[] Plans(){return new[]{
 new FixturePlan("Erdgeschoss","Hauswirtschaft",-6.5f,-5,-7.21f,-4.96f),
 new FixturePlan("Erdgeschoss","Werkstatt",-6.5f,4.5f,-7.21f,2.96f),
 new FixturePlan("Erdgeschoss","Gaeste_WC",6.5f,-5,7.21f,-5.96f),
 new FixturePlan("Erdgeschoss","Gaestezimmer",6.5f,4.5f,7.21f,3.46f),
 new FixturePlan("Erdgeschoss","Hobbyraum",0,-4.3f,0,5.96f,true),
 new FixturePlan("Erdgeschoss","Gaestebad",0,-4.3f,7.21f,-2.5f,true),
 new FixturePlan("1_OG_Wohnen","WC",-6.5f,-5,-7.46f,-5.96f),
 new FixturePlan("1_OG_Wohnen","Kueche",-6.5f,4.5f,-6.52f,1.96f),
 new FixturePlan("1_OG_Wohnen","Wohnen",6.5f,4.5f,6.52f,1.96f),
 new FixturePlan("1_OG_Wohnen","Essen",0,-4.3f,0,4.47f,true),
 new FixturePlan("2_OG_Schlafen","Buero",-6.5f,-5,-7.21f,-3.06f),
 new FixturePlan("2_OG_Schlafen","Eltern",-6.5f,4.5f,-4.96f,5.96f),
 new FixturePlan("2_OG_Schlafen","Schlafzimmer3",6.5f,-5,7.21f,-3.06f),
 new FixturePlan("2_OG_Schlafen","Kind",6.5f,4.5f,4.96f,5.96f),
 new FixturePlan("2_OG_Schlafen","Familienbad",0,-4.3f,0,-6.46f,true)
 };}
 static Transform Part(Transform parent,string name,Vector2 xz){
 Transform found=null;
 foreach(Transform t in parent)if(t.name==name&&Vector2.Distance(new Vector2(t.position.x,t.position.z),xz)<.02f){if(found)throw new System.Exception("Mehrere Leuchtenteile: "+name);found=t;}
 if(!found)throw new System.Exception("Leuchtenteil fehlt: "+parent.name+"/"+name+" "+xz);
 return found;
 }
 static GameObject Beam(Transform parent,string name,Vector3 a,Vector3 b,Material material){
 var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.position=(a+b)/2;g.transform.localScale=new Vector3(.055f,Vector3.Distance(a,b),.055f);g.transform.up=(b-a).normalized;g.GetComponent<Renderer>().sharedMaterial=material;Undo.RegisterCreatedObjectUndo(g,"Handlauf anschließen");return g;
 }
 [MenuItem("Familiar Dread/Villa/Details und Zimmerleuchten anpassen")]
 public static void Apply(){
 if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Spielmodus beenden.");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 if(scene.path!="Assets/Scenes/VillaPrototype.unity")throw new System.InvalidOperationException("VillaPrototype öffnen.");
 var villa=GameObject.Find("VILLA_PROTOTYPE");if(!villa)throw new System.Exception("Villa fehlt.");var root=villa.transform;
 var wood=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/Materials/Dunkles_Holz.mat");
 var iron=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/Materials/Eisen.mat");
 if(!wood||!iron)throw new System.Exception("Geländermaterialien fehlen.");
 var roof=root.Find("Doppelgarage/Garage_Walmdach");
 if(!roof||!roof.GetComponent<MeshFilter>()||!roof.GetComponent<MeshCollider>())throw new System.Exception("Garagendach fehlt.");
 var plans=Plans();string[] parts={"Deckenhalterung","Deckenleuchte","Raumlicht"};
 foreach(var plan in plans){var parent=root.Find(plan.level);if(!parent)throw new System.Exception("Etage fehlt.");if(parent.Find("CeilingFixture_"+plan.label))continue;foreach(var p in parts)Part(parent,p,plan.from);}
 Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Geländer, Garagenanschluss und Zimmerleuchten");
 var porch=root.Find("Aussentreppe");if(!porch)throw new System.Exception("Außentreppe fehlt.");
 if(!porch.Find("L_Eingangstreppen")){
 foreach(Transform t in porch){
 if(t.name!="Podest_Gelaender"&&t.name!="Baluster")continue;
 Undo.RecordObject(t,"Geländerprofil angleichen");var renderer=t.GetComponent<Renderer>();Undo.RecordObject(renderer,"Geländermaterial angleichen");
 if(t.name=="Podest_Gelaender"){renderer.sharedMaterial=wood;var s=t.localScale;s.x=.055f;s.z=.055f;t.localScale=s;}
 else{renderer.sharedMaterial=iron;t.localScale=new Vector3(.035f,.92f,.035f);var p=t.position;p.y=3.66f;t.position=p;}
 }
 if(!porch.Find("Podest_Handlauf_Anschluss_Links"))Beam(porch,"Podest_Handlauf_Anschluss_Links",new Vector3(-2.1f,4.15f,-10.4f),new Vector3(-2,4.15f,-10.2f),wood);
 if(!porch.Find("Podest_Handlauf_Anschluss_Rechts"))Beam(porch,"Podest_Handlauf_Anschluss_Rechts",new Vector3(2.1f,4.15f,-10.4f),new Vector3(2,4.15f,-10.2f),wood);
 }
 ClipGarageRoof(roof,-10.16f);
 var facade=root.Find("Dach_und_Fassade");var oldCornices=new List<Transform>();
 foreach(Transform t in facade)if(t.name=="Gesims_Seite"&&t.position.x<0&&t.position.y<3.6f)oldCornices.Add(t);
 foreach(var cornice in oldCornices){
 float low=cornice.position.z-cornice.localScale.z/2,high=cornice.position.z+cornice.localScale.z/2;
 foreach(var interval in new[]{new Vector2(low,-7.4f),new Vector2(.4f,high)}){
 if(interval.y<=interval.x)continue;var segment=Object.Instantiate(cornice.gameObject,facade);segment.name=interval.x<0?"Gesims_Seite_VorGarage":"Gesims_Seite_HinterGarage";
 var p=segment.transform.position;p.z=(interval.x+interval.y)/2;segment.transform.position=p;var s=segment.transform.localScale;s.z=interval.y-interval.x;segment.transform.localScale=s;Undo.RegisterCreatedObjectUndo(segment,"Gesims am Garagenanbau unterbrechen");
 }
 Undo.DestroyObjectImmediate(cornice.gameObject);
 }
 if(!root.Find("Doppelgarage/Garagenanschluss_Wandabschluss")){
 var closure=GameObject.CreatePrimitive(PrimitiveType.Cube);closure.name="Garagenanschluss_Wandabschluss";closure.transform.SetParent(root.Find("Doppelgarage"),false);closure.transform.position=new Vector3(-10,3.1f,-3.5f);closure.transform.localScale=new Vector3(.32f,.2f,7.8f);closure.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(AssetsRoot+"/Materials/Fassade_Kalk.mat");Undo.RegisterCreatedObjectUndo(closure,"Wandabschluss Garage schließen");
 }
 foreach(var plan in plans){
 var level=root.Find(plan.level);var existing=level.Find("CeilingFixture_"+plan.label);
 if(existing){Undo.RecordObject(existing,"Zimmerleuchte zentrieren");var p=existing.position;p.x=plan.to.x;p.z=plan.to.y;existing.position=p;continue;}
 var originals=new Transform[parts.Length];for(int i=0;i<parts.Length;i++)originals[i]=Part(level,parts[i],plan.from);
 var assembly=new GameObject("CeilingFixture_"+plan.label);assembly.transform.SetParent(level,false);assembly.transform.position=new Vector3(plan.to.x,originals[1].position.y,plan.to.y);
 if(!plan.add)Undo.RegisterCreatedObjectUndo(assembly,"Zimmerleuchte zusammenfassen");
 for(int i=0;i<parts.Length;i++){
 Transform t;
 if(plan.add){var clone=Object.Instantiate(originals[i].gameObject,assembly.transform);clone.name=parts[i];t=clone.transform;var p=originals[i].position;p.x=plan.to.x;p.z=plan.to.y;t.position=p;}
 else{t=originals[i];Undo.SetTransformParent(t,assembly.transform,"Leuchtenteile gruppieren");Undo.RecordObject(t,"Leuchte zentrieren");var p=t.position;p.x=plan.to.x;p.z=plan.to.y;t.position=p;}
 }
 if(plan.add)Undo.RegisterCreatedObjectUndo(assembly,"Fehlende Zimmerleuchte ergänzen");
 }
 Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 Debug.Log("Villa-Details angepasst: Geländer, Garagendach, Gesims und 15 mittige Zimmer-/Bereichsleuchten.");
 }
 static void ClipGarageRoof(Transform roof,float plane){
 var filter=roof.GetComponent<MeshFilter>();var original=filter.sharedMesh;
 bool protrudes=false;foreach(var v in original.vertices)if(roof.TransformPoint(v).x>plane+.0001f){protrudes=true;break;}if(!protrudes)return;
 var vertices=new List<Vector3>();var triangles=new List<int>();var source=original.vertices;var sourceTriangles=original.triangles;
 for(int i=0;i<sourceTriangles.Length;i+=3){
 var input=new List<Vector3>{source[sourceTriangles[i]],source[sourceTriangles[i+1]],source[sourceTriangles[i+2]]};var output=new List<Vector3>();
 for(int j=0;j<input.Count;j++){var a=input[j];var b=input[(j+1)%input.Count];float da=plane-roof.TransformPoint(a).x,db=plane-roof.TransformPoint(b).x;
 if(da>=0)output.Add(a);if((da>=0)!=(db>=0))output.Add(Vector3.Lerp(a,b,da/(da-db)));}
 for(int j=1;j<output.Count-1;j++){int k=vertices.Count;vertices.Add(output[0]);vertices.Add(output[j]);vertices.Add(output[j+1]);triangles.Add(k);triangles.Add(k+1);triangles.Add(k+2);}
 }
 var clipped=new Mesh();clipped.name="Garage_Walmdach_Anschluss";clipped.SetVertices(vertices);clipped.SetTriangles(triangles,0);clipped.RecalculateNormals();clipped.RecalculateBounds();
 string asset=AssetDatabase.GenerateUniqueAssetPath(AssetsRoot+"/Garage_Walmdach_Anschluss.asset");AssetDatabase.CreateAsset(clipped,asset);
 Undo.RecordObject(filter,"Garagendach am Haus abschließen");filter.sharedMesh=clipped;
 var collider=roof.GetComponent<MeshCollider>();Undo.RecordObject(collider,"Dachkollision anpassen");collider.sharedMesh=null;collider.sharedMesh=clipped;
 }
}
