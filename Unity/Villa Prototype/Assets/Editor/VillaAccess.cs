using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FamiliarDread;
using System;
using System.Collections.Generic;
public static class VillaAccess {
 static Transform group;static Material floor,wall,wood,iron;
 static GameObject Box(string n,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(group,false);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
 static void Beam(string n,Vector3 a,Vector3 b,float w,Material m){var g=Box(n,(a+b)/2,new Vector3(w,Vector3.Distance(a,b),w),m);g.transform.up=(b-a).normalized;}
 static void Guard(Vector3 a,Vector3 b){Beam("Luken_Handlauf",a+Vector3.up*.95f,b+Vector3.up*.95f,.055f,wood);int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.4f);for(int i=0;i<=count;i++){var p=Vector3.Lerp(a,b,i/(float)count);Beam("Luken_Stab",p,p+Vector3.up*.92f,.035f,iron);}}
 static Transform Point(string n,Vector3 p){var g=new GameObject(n);g.transform.SetParent(group,false);g.transform.position=p;return g.transform;}
 static float Under(float x){return Mathf.Min(2.92f,1.4f+(x+1.8f)*(1.6f/2.7f)-.09f/Mathf.Cos(Mathf.Atan2(1.6f,2.7f))-.02f);}
 static void Prism(string n,float x0,float x1,float z0,float z1,Func<float,float> bottom,Func<float,float> top){
 var corners=new[]{new Vector3(x0,bottom(x0),z0),new Vector3(x1,bottom(x1),z0),new Vector3(x1,bottom(x1),z1),new Vector3(x0,bottom(x0),z1),new Vector3(x0,top(x0),z0),new Vector3(x1,top(x1),z0),new Vector3(x1,top(x1),z1),new Vector3(x0,top(x0),z1)};
 int[] faces={0,1,2,0,2,3,4,6,5,4,7,6,0,4,5,0,5,1,1,5,6,1,6,2,2,6,7,2,7,3,3,7,4,3,4,0};
 var vertices=new Vector3[faces.Length];var triangles=new int[faces.Length];for(int i=0;i<faces.Length;i++){vertices[i]=corners[faces[i]];triangles[i]=i;}
 var mesh=new Mesh();mesh.name=n;mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath("Assets/VillaPrototype/"+n+".asset"));
 var g=new GameObject(n);g.transform.SetParent(group,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=wall;g.AddComponent<MeshCollider>().sharedMesh=mesh;
 }
 [MenuItem("Familiar Dread/Villa/Leiter und abgetrennten Kellerzugang bauen")]
 public static void Apply(){
 if(EditorApplication.isPlaying)throw new InvalidOperationException("Spielmodus beenden.");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/VillaPrototype.unity")throw new Exception("VillaPrototype öffnen.");
 var villa=GameObject.Find("VILLA_PROTOTYPE");if(!villa)throw new Exception("Villa fehlt.");var root=villa.transform;if(root.Find("Nebenraeume_Zugaenge"))return;
 var secondary=root.Find("Nebentreppen_Anpassung");var attic=root.Find("Dachboden");var right=attic?attic.Find("Boden_Rechts"):null;
 if(!secondary||!secondary.Find("Dachbodentreppe")||!right)throw new Exception("Ausgangsgeometrie fehlt.");
 floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Boden_Holz.mat");wall=AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Innenwand.mat");wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Dunkles_Holz.mat");iron=AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Eisen.mat");
 var doorPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VillaPrototype/Prefabs/Innentuer.prefab");if(!floor||!wall||!wood||!iron||!doorPrefab)throw new Exception("Material oder Türvorlage fehlt.");
 Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Dachbodenleiter und Kellerraum");
 Undo.DestroyObjectImmediate(secondary.Find("Dachbodentreppe").gameObject);
 var remove=new List<GameObject>();foreach(Transform t in secondary)if(t.name.StartsWith("Nebentreppe_")||t.name.StartsWith("Dachbodenzugang_"))remove.Add(t.gameObject);foreach(var g in remove)Undo.DestroyObjectImmediate(g);
 Undo.DestroyObjectImmediate(right.gameObject);
 var patch=new GameObject("Nebenraeume_Zugaenge");patch.transform.SetParent(root,false);group=patch.transform;
 Box("Dachboden_AlteOeffnung_Geschlossen",new Vector3(0,9.46f,-.2f),new Vector3(1.6f,.28f,6),floor);
 Box("Dachboden_Rechts_Bis_Luke",new Vector3(5.65f,9.46f,0),new Vector3(4.9f,.28f,16),floor);
 Box("Dachboden_Rechts_Am_Rand",new Vector3(9.65f,9.46f,0),new Vector3(.7f,.28f,16),floor);
 Box("Dachboden_Rechts_Vor_Luke",new Vector3(8.7f,9.46f,-3.025f),new Vector3(1.2f,.28f,9.95f),floor);
 Box("Dachboden_Rechts_Hinter_Luke",new Vector3(8.7f,9.46f,5.575f),new Vector3(1.2f,.28f,4.85f),floor);
 var ladderRoot=new GameObject("Dachbodenleiter");ladderRoot.transform.SetParent(patch.transform,false);group=ladderRoot.transform;var ladder=ladderRoot.AddComponent<VillaLadder>();
 ladder.lowerExit=Point("Unten_Austritt",new Vector3(8.7f,6.43f,3.3f));ladder.upperExit=Point("Oben_Austritt",new Vector3(8.7f,9.63f,1.45f));ladder.lowerClimb=Point("Unten_Leiter",new Vector3(8.7f,6.43f,2.65f));ladder.upperClimb=Point("Oben_Leiter",new Vector3(8.7f,9.63f,2.65f));
 foreach(float x in new[]{8.25f,9.15f})Box("Leiter_Holm",new Vector3(x,7.94f,2.2f),new Vector3(.06f,3.08f,.08f),wood);
 for(int i=0;i<11;i++)Box("Leiter_Sprosse",new Vector3(8.7f,6.65f+i*.28f,2.2f),new Vector3(.84f,.045f,.08f),wood);
 var interaction=ladderRoot.AddComponent<BoxCollider>();interaction.center=new Vector3(8.7f,7.94f,2.2f);interaction.size=new Vector3(.96f,3.08f,.12f);
 foreach(float x in new[]{8.1f,9.3f})Box("Lukenrahmen_Seite",new Vector3(x,9.61f,2.55f),new Vector3(.06f,.06f,1.26f),wood);
 foreach(float z in new[]{1.95f,3.15f})Box("Lukenrahmen_Quer",new Vector3(8.7f,9.61f,z),new Vector3(1.2f,.06f,.06f),wood);
 var pivot=new GameObject("Luke_Scharnier").transform;pivot.SetParent(group,false);pivot.position=new Vector3(8.1f,9.6f,2.55f);ladder.hatchPivot=pivot;
 var hatch=Box("Dachbodenluke_E",new Vector3(8.7f,9.55f,2.55f),new Vector3(1.18f,.1f,1.18f),wood);hatch.transform.SetParent(pivot,true);ladder.hatchCollider=hatch.GetComponent<Collider>();
 Guard(new Vector3(8.1f,9.6f,1.95f),new Vector3(8.1f,9.6f,3.15f));Guard(new Vector3(9.3f,9.6f,1.95f),new Vector3(9.3f,9.6f,3.15f));Guard(new Vector3(8.1f,9.6f,3.15f),new Vector3(9.3f,9.6f,3.15f));
 var enclosure=new GameObject("Kellerzugang_Abgetrennt");enclosure.transform.SetParent(patch.transform,false);group=enclosure.transform;
 foreach(int s in new[]{-1,1}){
 Box("Kellerraum_Seitenwand_Vorne",new Vector3(s*.88f,1.46f,-2.15f),new Vector3(.16f,2.92f,3.9f),wall);
 Box("Kellerraum_Seitenwand_Hinten",new Vector3(s*.88f,1.46f,1.44f),new Vector3(.16f,2.92f,.88f),wall);
 float x0=s<0?-.96f:.8f,x1=s<0?-.8f:.96f;Prism("Kellerraum_Wand_UnterTreppe_"+s,x0,x1,-.2f,1,x=>0,x=>Under(x)-.06f);
 }
 Prism("Kellerraum_Schraegdecke",-.96f,.96f,-.2f,1,x=>Under(x)-.06f,x=>Under(x));
 Prism("Kellerraum_Deckenanschluss_Vorne",-.96f,.96f,-.23f,-.2f,x=>Under(x)-.06f,x=>2.92f);
 Prism("Kellerraum_Deckenanschluss_Hinten",-.96f,.96f,1,1.03f,x=>Under(x)-.06f,x=>2.92f);
 Box("Kellerraum_Rueckwand",new Vector3(0,1.46f,1.88f),new Vector3(1.92f,2.92f,.16f),wall);
 Box("EG_Durchgang_Boden_Ergaenzung",new Vector3(0,-.14f,2.38f),new Vector3(1.6f,.28f,.84f),floor);
 foreach(int s in new[]{-1,1})Box("Kellertuer_Seitenwand",new Vector3(s*.83f,1.46f,-4.1f),new Vector3(.26f,2.92f,.16f),wall);
 // Sturzunterseite 1 cm im oberen Holzrahmen verbergen, um Z-Fighting zu vermeiden.
 Box("Kellertuer_Sturz",new Vector3(0,2.69f,-4.1f),new Vector3(1.4f,.46f,.16f),wall);
 foreach(int s in new[]{-1,1})Box("Kellertuer_Rahmen",new Vector3(s*.7f,1.225f,-4.1f),new Vector3(.075f,2.53f,.25f),wood);
 Box("Kellertuer_Rahmen_Oben",new Vector3(0,2.485f,-4.1f),new Vector3(1.55f,.07f,.25f),wood);
 var door=(GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);door.name="Kellertuer_E";door.transform.SetParent(group,false);door.transform.position=new Vector3(-.675f,0,-4.1f);door.transform.rotation=Quaternion.identity;door.GetComponent<VillaDoor>().angle=95;
 // Keep the hall fixture clear of the new front wall.
 var eg=root.Find("Erdgeschoss");foreach(Transform t in eg)if((t.name=="Deckenhalterung"||t.name=="Deckenleuchte"||t.name=="Raumlicht")&&Mathf.Abs(t.position.x)<.01f&&Mathf.Abs(t.position.z+4.3f)<.01f){Undo.RecordObject(t,"Flurlicht vor Kellertür freihalten");var p=t.position;p.z=-4.9f;t.position=p;}
 Undo.RegisterCreatedObjectUndo(patch,"Neue Nebenzugänge");Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Selection.activeGameObject=patch;
 Debug.Log("Dachboden über Leiter am rechten Flurrand; alte Öffnung geschlossen. Kellerzugang mit Wänden und Tür abgetrennt.");
 }
}
