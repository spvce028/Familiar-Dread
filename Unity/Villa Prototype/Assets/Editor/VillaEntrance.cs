using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class VillaEntrance {
 static Transform parent;
 static Material stone,wood,iron;
 static GameObject Box(string name,Vector3 position,Vector3 size,Material material){
 var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;return g;
 }
 static void Beam(string name,Vector3 a,Vector3 b,float width,Material material){var g=Box(name,(a+b)/2,new Vector3(width,Vector3.Distance(a,b),width),material);g.transform.up=(b-a).normalized;}
 static void Guard(string name,Vector3 a,Vector3 b){
 Beam(name+"_Handlauf",a+Vector3.up*.95f,b+Vector3.up*.95f,.055f,wood);
 int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.4f);for(int i=0;i<=count;i++){var p=Vector3.Lerp(a,b,i/(float)count);Beam(name+"_Stab",p,p+Vector3.up*.92f,.035f,iron);}
 }
 static void Run(string name,Vector3 a,Vector3 b){
 var direction=new Vector3(b.x-a.x,0,b.z-a.z).normalized;float length=Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));var side=Vector3.Cross(Vector3.up,direction);
 for(int i=0;i<9;i++){float top=Mathf.Lerp(a.y,b.y,(i+1)/9f);var p=a+direction*(length*(i+.5f)/9);p.y=top-.12f;var step=Box(name+"_Stufe_"+(i+1),p,new Vector3(1.4f,.24f,length/9+.015f),stone);step.transform.rotation=Quaternion.LookRotation(direction);}
 var ac=a-Vector3.up*.2f;var bc=b-Vector3.up*.2f;var support=Box(name+"_Unterbau",(ac+bc)/2,new Vector3(1.4f,.18f,Vector3.Distance(ac,bc)),stone);support.transform.rotation=Quaternion.LookRotation((bc-ac).normalized,Vector3.up);
 foreach(int s in new[]{-1,1}){var offset=side*(s*.65f);Guard(name,a+offset,b+offset);}
 }
 [MenuItem("Familiar Dread/Villa/Eingangstreppen über Eck bauen")]
 public static void Apply(){
 if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Spielmodus beenden.");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/VillaPrototype.unity")throw new System.Exception("VillaPrototype öffnen.");
 var villa=GameObject.Find("VILLA_PROTOTYPE");if(!villa)throw new System.Exception("Villa fehlt.");
 stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Stein.mat");wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Dunkles_Holz.mat");iron=AssetDatabase.LoadAssetAtPath<Material>("Assets/VillaPrototype/Materials/Eisen.mat");if(!stone||!wood||!iron)throw new System.Exception("Materialien fehlen.");
 Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("L-Eingangstreppen und verlängertes Podest");
 var old=villa.transform.Find("Aussentreppe");if(old)Undo.DestroyObjectImmediate(old.gameObject);
 var entrance=new GameObject("Aussentreppe");entrance.transform.SetParent(villa.transform,false);parent=entrance.transform;
 // One continuous slab, flush with the entrance threshold and extended towards the garden.
 Box("Eingangspodest",new Vector3(0,3.05f,-9.9f),new Vector3(4.2f,.3f,3.8f),stone);
 foreach(int s in new[]{-1,1})foreach(float z in new[]{-8.5f,-11.4f})Box("Podeststuetze",new Vector3(s*1.8f,1.425f,z),new Vector3(.2f,2.95f,.2f),stone);
 Guard("Podest_Seite_Links",new Vector3(-2.05f,3.2f,-11.8f),new Vector3(-2.05f,3.2f,-8.16f));
 Guard("Podest_Seite_Rechts",new Vector3(2.05f,3.2f,-11.8f),new Vector3(2.05f,3.2f,-8.16f));
 Guard("Podest_Mitte",new Vector3(-.75f,3.2f,-11.8f),new Vector3(.75f,3.2f,-11.8f));
 var flights=new GameObject("L_Eingangstreppen");flights.transform.SetParent(entrance.transform,false);
 foreach(int s in new[]{-1,1}){
 var branch=new GameObject(s<0?"Links":"Rechts");branch.transform.SetParent(flights.transform,false);parent=branch.transform;
 Run("Unterer_Lauf",new Vector3(s*4.8f,0,-15.2f),new Vector3(s*2.1f,1.6f,-15.2f));
 Box("Eckpodest",new Vector3(s*1.4f,1.48f,-15.2f),new Vector3(1.4f,.24f,1.4f),stone);
 Box("Eckpodeststuetze",new Vector3(s*1.4f,.655f,-15.2f),new Vector3(.2f,1.41f,.2f),stone);
 Run("Oberer_Lauf",new Vector3(s*1.4f,1.6f,-14.5f),new Vector3(s*1.4f,3.2f,-11.8f));
 Guard("Eckpodest_Hinten",new Vector3(s*2.05f,1.6f,-15.85f),new Vector3(s*.75f,1.6f,-15.85f));
 Guard("Eckpodest_Innen",new Vector3(s*.75f,1.6f,-15.85f),new Vector3(s*.75f,1.6f,-14.5f));
 }
 parent=entrance.transform;Undo.RegisterCreatedObjectUndo(entrance,"L-Eingangstreppen");Undo.CollapseUndoOperations(undo);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=entrance;
 Debug.Log("Eingangstreppen: beidseitig eine 90-Grad-Wendung, 1,40 m Laufbreite, 3,80 m tiefes Eingangspodest.");
 }
}
