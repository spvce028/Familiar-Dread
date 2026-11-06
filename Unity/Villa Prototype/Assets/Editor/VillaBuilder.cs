using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using FamiliarDread;
using System.Collections.Generic;

public static class VillaBuilder {
 static Transform root, group; static Material plaster,inside,wood,stone,roof,metal,glass,ground,floor,white;
 static string basePath="Assets/VillaPrototype";
 static Material Mat(string name,Color c,float smooth=.15f){
 var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.color=c;m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,basePath+"/Materials/"+name+".mat");return m;}
 static GameObject Box(string name,Vector3 p,Vector3 s,Material m,bool collider=true){
 var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(group,false);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;
 if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
 static void Group(string n){group=new GameObject(n).transform;group.SetParent(root,false);}
 static void Beam(string name,Vector3 a,Vector3 b,float width,Material m){var g=Box(name,(a+b)/2,new Vector3(width,(b-a).magnitude,width),m);g.transform.up=(b-a).normalized;}
 static void Label(string text,Vector3 pos, float yaw=0){
 var g=new GameObject("Schild_"+text);g.transform.SetParent(group,false);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw+180,0);
 var t=g.AddComponent<TextMesh>();t.text=text;t.fontSize=48;t.characterSize=.065f;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.12f,.1f,.07f);
 var f=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.font=f;
 var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetTexture("_BaseMap",f.material.mainTexture);mat.SetColor("_BaseColor",t.color);mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.25f);mat.EnableKeyword("_ALPHATEST_ON");g.GetComponent<MeshRenderer>().sharedMaterial=mat;
 }
 struct Opening{public float c,w,b,h;public bool door;public Opening(float c,float w,float b,float h,bool d=false){this.c=c;this.w=w;this.b=b;this.h=h;door=d;}}
 static void Wall(string n,Vector3 a,Vector3 b,float height,float thick,Material m,params Opening[] holes){
 Vector3 dir=(b-a).normalized;float len=Vector3.Distance(a,b);float start=0;
 System.Array.Sort(holes,(x,y)=>x.c.CompareTo(y.c));
 System.Action<float,float,float,float,string> piece=(l,r,bottom,top,suffix)=>{
 if(r-l<.001f||top-bottom<.001f)return;
 var g=Box(n+suffix,a+dir*((l+r)/2)+Vector3.up*((bottom+top)/2),new Vector3(r-l,top-bottom,thick),m);
 g.transform.rotation=Quaternion.LookRotation(Vector3.Cross(dir,Vector3.up),Vector3.up);};
 foreach(var h in holes){float l=h.c-h.w/2,r=h.c+h.w/2;piece(start,l,0,height,"_Wand");piece(l,r,0,Mathf.Max(0,h.b-.01f),"_Brüstung");piece(l,r,h.b+h.h+.01f,height,"_Sturz"); // Sturzunterseite 1 cm im Holz verbergen: keine koplanaren Flächen.
 Vector3 mid=a+dir*h.c;
 var rot=Quaternion.LookRotation(Vector3.Cross(dir,Vector3.up),Vector3.up);
 System.Action<string,Vector3,Vector3,Material,bool> detail=(nn,p,s,mm,col)=>{var obj=Box(nn,p,s,mm,col);obj.transform.rotation=rot;};
 detail(n+"_Rahmen",mid-dir*(h.w/2)+Vector3.up*(h.b+h.h/2),new Vector3(.075f,h.h+.08f,thick+.09f),wood,false);
 detail(n+"_Rahmen",mid+dir*(h.w/2)+Vector3.up*(h.b+h.h/2),new Vector3(.075f,h.h+.08f,thick+.09f),wood,false);
 detail(n+"_Rahmen_oben",mid+Vector3.up*(h.b+h.h+.035f),new Vector3(h.w+.15f,.07f,thick+.09f),wood,false);
 if(!h.door){
 detail(n+"_Fensterbank",mid+Vector3.up*(h.b-.035f),new Vector3(h.w+.25f,.07f,thick+.2f),stone,false);
 detail(n+"_Glas",mid+Vector3.up*(h.b+h.h/2),new Vector3(h.w,h.h,.025f),glass,true);
 detail(n+"_Sprosse",mid+Vector3.up*(h.b+h.h/2),new Vector3(.05f,h.h,.10f),wood,false);
 detail(n+"_Querbalken",mid+Vector3.up*(h.b+h.h*.62f),new Vector3(h.w,.05f,.10f),wood,false);
 }start=r;}
 piece(start,len,0,height,"_Wand");
 }
 static void Door(Vector3 center, bool alongZ=false,float width=1.35f){
 var hinge=new GameObject("Tür_E_zum_Öffnen");hinge.transform.SetParent(group,false);hinge.transform.position=center+(alongZ?Vector3.back:Vector3.left)*(width/2);
 hinge.transform.rotation=Quaternion.Euler(0,alongZ?-90:0,0);var doorScript=hinge.AddComponent<VillaDoor>();doorScript.angle=(alongZ&&center.x<0)||Mathf.Abs(center.z-4)<.1f?-95:95;
 var leaf=Box("Türblatt",Vector3.zero,new Vector3(width-.04f,2.45f,.075f),wood);leaf.transform.SetParent(hinge.transform,false);leaf.transform.localPosition=new Vector3(width/2,1.225f,.0f);
 // Beide Griffe bewegen sich mit dem Türscharnier. Die Blattoberkante liegt bündig am Rahmen bei 2,45 m.
 foreach (float side in new[] { -1f, 1f }) {
     var handle = Box(side < 0 ? "Türgriff" : "Türgriff_Rueckseite", Vector3.zero, new Vector3(.16f, .04f, .16f), metal, false);
     handle.transform.SetParent(hinge.transform, false);
     handle.transform.localPosition = new Vector3(width - .2f, 1.05f, side * .1f);
 }
 }
 static void Partition(string n,float y,Vector2 a,Vector2 b,float door){
 Wall(n,new Vector3(a.x,y,a.y),new Vector3(b.x,y,b.y),3,.16f,inside,new Opening(door,1.4f,0,2.45f,true));
 Vector2 d=(b-a).normalized;Door(new Vector3(a.x+d.x*door,y,a.y+d.y*door),Mathf.Abs(d.y)>.5f);
 }
 static void Solid(string n,float y,Vector2 a,Vector2 b){Wall(n,new Vector3(a.x,y,a.y),new Vector3(b.x,y,b.y),3,.16f,inside);}
 static void Slab(float y,bool hole){
 if(!hole){Box("Bodenplatte",new Vector3(0,y-.14f,0),new Vector3(20,.28f,16),floor);return;}
 Box("Boden_Links",new Vector3(-6.6f,y-.14f,0),new Vector3(6.8f,.28f,16),floor);
 Box("Boden_Rechts",new Vector3(6.6f,y-.14f,0),new Vector3(6.8f,.28f,16),floor);
 Box("Boden_Vorne",new Vector3(0,y-.14f,-5.6f),new Vector3(6.4f,.28f,4.8f),floor);
 Box("Boden_Hinten",new Vector3(0,y-.14f,5.5f),new Vector3(6.4f,.28f,5),floor);
 }
 static void Run(string name,Vector3 a,Vector3 b,float width,int count,bool rails=true){
 var dir=new Vector3(b.x-a.x,0,b.z-a.z).normalized;float length=Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));Vector3 side=Vector3.Cross(Vector3.up,dir);
 for(int i=0;i<count;i++){float top=Mathf.Lerp(a.y,b.y,(i+1f)/count);var p=a+dir*(length*(i+.5f)/count);p.y=top-.12f;
 var step=Box(name+"_Stufe_"+(i+1),p,new Vector3(width,.24f,length/count+.015f),stone);step.transform.rotation=Quaternion.LookRotation(dir);
 }
 // Closed sloped underside supports the individual treads.
 Vector3 ac=a+Vector3.down*.20f,bc=b+Vector3.down*.20f;
 var ramp=Box(name+"_Unterbau",(ac+bc)/2,new Vector3(width,.18f,Vector3.Distance(ac,bc)),stone);
 ramp.transform.rotation=Quaternion.LookRotation((bc-ac).normalized,Vector3.up);
 if(rails)for(int s=-1;s<=1;s+=2){Vector3 off=side*(width/2-.05f)*s;
 Beam(name+"_Handlauf",a+off+Vector3.up*.95f,b+off+Vector3.up*.95f,.055f,wood);
 for(int i=0;i<=count;i+=2){Vector3 p=Vector3.Lerp(a,b,i/(float)count)+off;Beam(name+"_Stab",p,p+Vector3.up*.92f,.035f,metal);}}
 }
 static void Stairs(float y,float width,string name){
 Group(name);
 float rise=3.2f/3;
 Run("Lauf_1",new Vector3(-2,y,-2.9f),new Vector3(-2,y+rise,1.05f),width,6);
 Box("Eckpodest_1",new Vector3(-2,y+rise-.12f,1.8f),new Vector3(1.5f,.24f,1.5f),stone);
 Run("Lauf_2",new Vector3(-1.25f,y+rise,1.8f),new Vector3(1.25f,y+2*rise,1.8f),width,6);
 Box("Eckpodest_2",new Vector3(2,y+2*rise-.12f,1.8f),new Vector3(1.5f,.24f,1.5f),stone);
 Run("Lauf_3",new Vector3(2,y+2*rise,1.05f),new Vector3(2,y+3.2f,-2.9f),width,6);
 // Connect both ends to the hall without bridging the stairwell.
 Box("Antritt",new Vector3(-2,y-.12f,-3.05f),new Vector3(1.5f,.24f,.3f),stone);
 Box("Austritt",new Vector3(2,y+3.2f-.12f,-3.05f),new Vector3(1.5f,.24f,.3f),stone);
 }

 [MenuItem("Familiar Dread/Villa/Nebentreppen begradigen")]
 public static void AdjustSecondaryStairs(){
 if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Bitte zuerst den Spielmodus beenden.");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 if(scene.path!="Assets/Scenes/VillaPrototype.unity")throw new System.InvalidOperationException("Bitte VillaPrototype öffnen.");
 var villa=GameObject.Find("VILLA_PROTOTYPE");
 if(villa==null)throw new System.InvalidOperationException("Villa fehlt.");
 root=villa.transform;
 stone=AssetDatabase.LoadAssetAtPath<Material>(basePath+"/Materials/Stein.mat");
 wood=AssetDatabase.LoadAssetAtPath<Material>(basePath+"/Materials/Dunkles_Holz.mat");
 metal=AssetDatabase.LoadAssetAtPath<Material>(basePath+"/Materials/Eisen.mat");
 floor=AssetDatabase.LoadAssetAtPath<Material>(basePath+"/Materials/Boden_Holz.mat");
 if(!stone||!wood||!metal||!floor)throw new System.InvalidOperationException("Treppenmaterialien fehlen.");
 Undo.IncrementCurrentGroup();int undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Gerade Keller- und Dachbodentreppe");
 foreach(string n in new[]{"Kellertreppe","Dachbodentreppe","Nebentreppen_Anpassung"}){
 var old=root.Find(n);if(old)Undo.DestroyObjectImmediate(old.gameObject);
 }
 var guards=root.Find("Treppenauge_Gelaender");
 if(guards){
 var remove=new List<GameObject>();
 foreach(Transform t in guards){
 float y=t.position.y;
 bool outerLevel=y>.1f&&y<1.1f||y>9.7f&&y<10.7f;
 bool atticEntrance=y>6.5f&&y<7.5f&&Mathf.Abs(t.position.z+3.2f)<.03f&&Mathf.Abs(t.position.x)<1.2f;
 if(outerLevel||atticEntrance)remove.Add(t.gameObject);
 }
 foreach(var g in remove)Undo.DestroyObjectImmediate(g);
 }
 Group("Nebentreppen_Anpassung");var patch=group;
 foreach(float y in new[]{0f,9.6f}){
 Box("Boden_Ergaenzung_Links",new Vector3(-2,y-.14f,-.1f),new Vector3(2.4f,.28f,6.2f),floor);
 Box("Boden_Ergaenzung_Rechts",new Vector3(2,y-.14f,-.1f),new Vector3(2.4f,.28f,6.2f),floor);
 Box("Boden_Ergaenzung_Hinten",new Vector3(0,y-.14f,2.9f),new Vector3(1.6f,.28f,.2f),floor);
 foreach(int side in new[]{-1,1}){
 Beam("Nebentreppe_Bruestung_Seite",new Vector3(side*.8f,y+1,-3.2f),new Vector3(side*.8f,y+1,2.8f),.065f,wood);
 for(float z=-3.2f;z<2.9f;z+=.5f)Beam("Nebentreppe_Bruestung_Stab",new Vector3(side*.8f,y,z),new Vector3(side*.8f,y+1,z),.04f,metal);
 }
 float endZ=y<1?2.8f:-3.2f;
 Beam("Nebentreppe_Bruestung_Stirn",new Vector3(-.8f,y+1,endZ),new Vector3(.8f,y+1,endZ),.065f,wood);
 for(float x=-.8f;x<.81f;x+=.4f)Beam("Nebentreppe_Bruestung_Stab",new Vector3(x,y,endZ),new Vector3(x,y+1,endZ),.04f,metal);
 }
 // Guard the remaining edges of the 2. OG stairwell beside the new entrance.
 foreach(int side in new[]{-1,1}){
 Beam("Dachbodenzugang_Bruestung",new Vector3(side*.65f,7.4f,-3.2f),new Vector3(side*1.15f,7.4f,-3.2f),.065f,wood);
 Beam("Dachbodenzugang_Stab",new Vector3(side*.70f,6.4f,-3.2f),new Vector3(side*.70f,7.4f,-3.2f),.04f,metal);
 }
 foreach(bool attic in new[]{false,true}){
 var stair=new GameObject(attic?"Dachbodentreppe":"Kellertreppe");stair.transform.SetParent(patch,false);group=stair.transform;
 float y=attic?6.4f:-3.2f;
 Run("Gerader_Lauf",new Vector3(0,y,attic?-2.9f:2.6f),new Vector3(0,y+3.2f,attic?2.6f:-2.9f),1.2f,18);
 Box("Austritt",new Vector3(0,y+3.2f-.12f,attic?2.7f:-3.05f),new Vector3(1.2f,.24f,attic?.2f:.3f),stone);
 if(attic)Box("Antritt",new Vector3(0,y-.12f,-3.05f),new Vector3(1.2f,.24f,.3f),stone);
 }
 group=patch;
 Undo.RegisterCreatedObjectUndo(patch.gameObject,"Gerade Nebentreppen");
 Undo.CollapseUndoOperations(undoGroup);
 EditorSceneManager.MarkSceneDirty(scene);
 EditorSceneManager.SaveScene(scene);
 Selection.activeGameObject=patch.gameObject;
 Debug.Log("Gerade Nebentreppen: 1,20 m breit, 18 Stufen, 5,50 m Lauf; Deckenöffnungen und Geländer angepasst.");
 }


 [MenuItem("Familiar Dread/Villa/Haupttreppe kompakt gestalten")]
 public static void AdjustMainStairs(){
 if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Bitte Spielmodus beenden.");
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
 if(scene.path!="Assets/Scenes/VillaPrototype.unity")throw new System.InvalidOperationException("Bitte VillaPrototype öffnen.");
 var villa=GameObject.Find("VILLA_PROTOTYPE");if(!villa)throw new System.InvalidOperationException("Villa fehlt.");root=villa.transform;
 stone=AssetDatabase.LoadAssetAtPath<Material>(basePath+"/Materials/Stein.mat");
 wood=AssetDatabase.LoadAssetAtPath<Material>(basePath+"/Materials/Dunkles_Holz.mat");
 metal=AssetDatabase.LoadAssetAtPath<Material>(basePath+"/Materials/Eisen.mat");
 floor=AssetDatabase.LoadAssetAtPath<Material>(basePath+"/Materials/Boden_Holz.mat");
 if(!stone||!wood||!metal||!floor)throw new System.InvalidOperationException("Materialien fehlen.");
 Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Kompakte L-Haupttreppe");
 foreach(string n in new[]{"Haupttreppe_0","Haupttreppe_1","Treppenauge_Gelaender","Haupttreppe_Kompakt"}){
 var old=root.Find(n);if(old)Undo.DestroyObjectImmediate(old.gameObject);
 }
 var secondary=root.Find("Nebentreppen_Anpassung");
 if(secondary){var remove=new List<GameObject>();foreach(Transform t in secondary)if(t.name.StartsWith("Dachbodenzugang_"))remove.Add(t.gameObject);foreach(var g in remove)Undo.DestroyObjectImmediate(g);}
 Group("Haupttreppe_Kompakt");var patch=group;
 for(int level=0;level<2;level++){
 var flight=new GameObject("Haupttreppe_L_"+level).transform;flight.SetParent(patch,false);group=flight;float y=level*3.2f;
 Run("Lauf_1",new Vector3(-2.4f,y,-2.9f),new Vector3(-2.4f,y+1.6f,-.2f),1.2f,9);
 Box("Eckpodest",new Vector3(-2.4f,y+1.48f,.4f),new Vector3(1.2f,.24f,1.2f),stone);
 Run("Lauf_2",new Vector3(-1.8f,y+1.6f,.4f),new Vector3(.9f,y+3.2f,.4f),1.2f,9);
 Box("Antritt",new Vector3(-2.4f,y-.12f,-3.05f),new Vector3(1.2f,.24f,.3f),stone);
 Box("Austritt",new Vector3(1,y+3.2f-.12f,.4f),new Vector3(.2f,.24f,1.2f),stone);
 // Back and left edges of the corner landing.
 Beam("Podest_Handlauf_Links",new Vector3(-2.95f,y+2.55f,-.2f),new Vector3(-2.95f,y+2.55f,.95f),.055f,wood);
 Beam("Podest_Handlauf_Hinten",new Vector3(-2.95f,y+2.55f,.95f),new Vector3(-1.8f,y+2.55f,.95f),.055f,wood);
 foreach(var p in new[]{new Vector3(-2.95f,y+1.6f,.95f),new Vector3(-2.95f,y+1.6f,.4f),new Vector3(-2.4f,y+1.6f,.95f)})Beam("Podest_Stab",p,p+Vector3.up*.92f,.035f,metal);
 }
 group=patch;
 float[] xs={-3.2f,-3.1f,-1.7f,1.1f,3.2f};float[] zs={-3.2f,-.3f,1.1f,3};
 for(int level=1;level<=2;level++){
 float y=level*3.2f;
 for(int x=0;x<xs.Length-1;x++)for(int z=0;z<zs.Length-1;z++){
 float cx=(xs[x]+xs[x+1])/2,cz=(zs[z]+zs[z+1])/2;
 bool opening=(cx>-3.1f&&cx< -1.7f&&cz<1.1f)||(cx>=-1.7f&&cx<1.1f&&cz>-.3f&&cz<1.1f);
 if(!opening)Box("Boden_Ergaenzung_"+level,new Vector3(cx,y-.14f,cz),new Vector3(xs[x+1]-xs[x],.28f,zs[z+1]-zs[z]),floor);
 }
 CompactGuard(new Vector3(-3.1f,y,-3.2f),new Vector3(-3.1f,y,1.1f));
 CompactGuard(new Vector3(-3.1f,y,1.1f),new Vector3(1.1f,y,1.1f));
 CompactGuard(new Vector3(-1.7f,y,-3.2f),new Vector3(-1.7f,y,-.3f));
 CompactGuard(new Vector3(-1.7f,y,-.3f),new Vector3(1.1f,y,-.3f));
 if(level==2)CompactGuard(new Vector3(-3.1f,y,-3.2f),new Vector3(-1.7f,y,-3.2f));
 }
 Undo.RegisterCreatedObjectUndo(patch.gameObject,"L-Haupttreppe erstellen");Undo.CollapseUndoOperations(undo);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=patch.gameObject;
 Debug.Log("Haupttreppe: 1 x 90 Grad, 1,20 m Laufbreite, 2 x 9 Stufen, L-förmige Deckenöffnung 9,94 m².");
 }
 static void CompactGuard(Vector3 a,Vector3 b){
 Beam("L_Treppenauge_Handlauf",a+Vector3.up,b+Vector3.up,.065f,wood);
 int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.45f);
 for(int i=0;i<=count;i++){var p=Vector3.Lerp(a,b,i/(float)count);Beam("L_Treppenauge_Stab",p,p+Vector3.up,.04f,metal);}
 }

 static void LightAt(Vector3 p){
 var ceiling=Box("Deckenhalterung",p+Vector3.up*.06f,new Vector3(.10f,.12f,.10f),metal,false);
 Box("Deckenleuchte",p,new Vector3(.5f,.06f,.5f),white,false);
 var g=new GameObject("Raumlicht");g.transform.SetParent(group,false);g.transform.position=p+Vector3.down*.22f;var l=g.AddComponent<Light>();l.type=LightType.Point;l.range=8;l.intensity=2.2f;l.color=new Color(1,.90f,.76f);l.shadows=LightShadows.None;
 }
 static void Hip(string n,Vector3 center,float sx,float sz,float eave,float peak){
 Vector3[] v={new Vector3(-sx/2,eave,-sz/2),new Vector3(sx/2,eave,-sz/2),new Vector3(sx/2,eave,sz/2),new Vector3(-sx/2,eave,sz/2),new Vector3(-sx*.17f,peak,0),new Vector3(sx*.17f,peak,0)};
 for(int i=0;i<v.Length;i++)v[i]+=center;
 int[] t={0,4,5,0,5,1,1,5,2,2,5,4,2,4,3,3,4,0};var both=new List<int>(t);int original=v.Length;var doubled=new Vector3[original*2];System.Array.Copy(v,0,doubled,0,original);System.Array.Copy(v,0,doubled,original,original);v=doubled;for(int i=0;i<t.Length;i+=3){both.Add(t[i+2]+original);both.Add(t[i+1]+original);both.Add(t[i]+original);}
 var mesh=new Mesh();mesh.name=n;mesh.vertices=v;mesh.triangles=both.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,basePath+"/"+n+".asset");
 var g=new GameObject(n);g.transform.SetParent(group,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=roof;g.AddComponent<MeshCollider>().sharedMesh=mesh;
 }
 [MenuItem("Familiar Dread/Villa/Grundaufbau erstellen")]
 public static void Build(){
 if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/VillaPrototype.unity")!=null)throw new System.Exception("VillaPrototype existiert bereits. Kein automatisches Überschreiben.");
 EditorSceneManager.SaveOpenScenes();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 System.IO.Directory.CreateDirectory(basePath+"/Materials");AssetDatabase.Refresh();
 plaster=Mat("Fassade_Kalk",new Color(.68f,.65f,.54f));inside=Mat("Innenwand",new Color(.72f,.69f,.59f));wood=Mat("Dunkles_Holz",new Color(.16f,.095f,.05f));stone=Mat("Stein",new Color(.43f,.44f,.40f));roof=Mat("Dach_Schiefer",new Color(.095f,.12f,.14f));metal=Mat("Eisen",new Color(.07f,.08f,.075f),.5f);glass=Mat("Fensterglas",new Color(.40f,.57f,.63f,.18f),.8f);ground=Mat("Garten",new Color(.22f,.28f,.18f));floor=Mat("Boden_Holz",new Color(.38f,.29f,.19f));white=Mat("Leuchte",new Color(.95f,.88f,.68f));
 glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
 root=new GameObject("VILLA_PROTOTYPE").transform;
 Group("Garten_und_Zufahrt");foreach(int side in new[]{-1,1}){Box("Garten_Seite",new Vector3(side*22.6f,-.225f,0),new Vector3(24.8f,.35f,60),ground);Box("Garten_Vorne_Hinten",new Vector3(0,-.225f,side*19.1f),new Vector3(20.4f,.35f,21.8f),ground);}Box("Zufahrt",new Vector3(-13.6f,-.025f,-15),new Vector3(7.4f,.08f,15),stone);Box("Eingangsweg",new Vector3(0,-.025f,-16),new Vector3(3,.08f,16),stone);
 for(int level=-1;level<=3;level++){
 float y=level*3.2f;Group(level==-1?"Keller":level==0?"Erdgeschoss":level==1?"1_OG_Wohnen":level==2?"2_OG_Schlafen":"Dachboden");Slab(y,level!=-1);
 if(level==-1){foreach(int z in new[]{-8,8})Wall("Kellerwand",new Vector3(-10,y,z),new Vector3(10,y,z),3,.35f,stone);foreach(int x in new[]{-10,10})Wall("Kellerwand",new Vector3(x,y,-8),new Vector3(x,y,8),3,.35f,stone);LightAt(new Vector3(-6,y+2.86f,2));LightAt(new Vector3(6,y+2.86f,-2));Label("KELLER",new Vector3(0,y+1.8f,7.8f),180);continue;}
 float h=level==3?1.7f:3;
 var fronts=new List<Opening>();foreach(float x in level==0?new[]{-8.37f,-6.05f,6.05f,8.37f}:level==1?new[]{-7.46f,-2.9225f,3.613333f,7.151667f}:level==2?new[]{-7.21f,-1.756667f,1.756667f,7.21f}:new[]{-7.5f,-4.5f,4.5f,7.5f})fronts.Add(new Opening(x+10,1.45f,level==3?.45f:.8f,level==3?.85f:1.8f));
 if(level<2)fronts.Add(new Opening(10,1.7f,0,2.45f,true));
 if(level==3)fronts.Clear();
 Wall("Front",new Vector3(-10,y,-8),new Vector3(10,y,-8),h,.32f,plaster,fronts.ToArray());if(level<2)Door(new Vector3(0,y,-8),false,1.66f);
 foreach(int z in new[]{8}){var wins=new List<Opening>();foreach(float x in level==0?new[]{-7.21f,-2.6475f,0,2.6475f,7.21f}:level==1?new[]{-7.143333f,-3.571667f,0,3.571667f,7.143333f}:level==2?new[]{-6.878333f,-3.041667f,3.041667f,6.878333f}:new[]{-7.5f,-4.5f,0,4.5f,7.5f})wins.Add(new Opening(x+10,1.5f,level==3?.45f:.8f,level==3?.85f:1.8f));if(level==3)wins.Clear();Wall("Gartenseite",new Vector3(-10,y,z),new Vector3(10,y,z),h,.32f,plaster,wins.ToArray());}
 foreach(int x in new[]{-10,10}){var wins=new List<Opening>();foreach(float z in level==0?(x==-10?new[]{2.605f,5.635f}:new[]{-5.96f,.8575f,3.46f,6.0625f}):level==1?(x==-10?new[]{-5.96f,-1.3925f,1.96f,5.3125f}:new[]{-5.199f,-1.733f,1.733f,5.199f}):level==2?new[]{-4.928333f,-1.191667f,2.9f,5.96f}:new[]{-5.5f,-1f,3.5f,6}){if(level==0&&x==-10&&z<0)continue;wins.Add(new Opening(z+8,1.4f,level==3?.45f:.8f,level==3?.85f:1.8f));}if(level==0&&x==-10)wins.Add(new Opening(4,1.4f,0,2.45f,true));if(level==3)wins.Clear();Wall("Seitenwand",new Vector3(x,y,-8),new Vector3(x,y,8),h,.32f,plaster,wins.ToArray());if(level==0&&x==-10)Door(new Vector3(-10,0,-4),true);}
 if(level<3){foreach(float x in new[]{-6.5f,6.5f})foreach(float z in new[]{-5,4.5f})LightAt(new Vector3(x,y+2.88f,z));LightAt(new Vector3(0,y+2.88f,-4.3f));}else{LightAt(new Vector3(-6,y+1.58f,0));LightAt(new Vector3(6,y+1.58f,0));Label("DACHBODEN / OFFENE LAGERFLÄCHE",new Vector3(0,y+1.05f,7.8f),180);}
 }
 Group("EG_Raumaufteilung");
 Partition("HWR_Flur",0,new Vector2(-4.5f,-8),new Vector2(-4.5f,-2),3.5f);
 Partition("HWR_Werkstatt",0,new Vector2(-10,-2),new Vector2(-4.5f,-2),2.7f);
 Partition("Werkstatt_Flur",0,new Vector2(-4.5f,-2),new Vector2(-4.5f,8),2);
 Partition("Hobbyraum",0,new Vector2(-4.5f,4),new Vector2(4.5f,4),4.5f);
 
 Solid("Gaeste_WC",0,new Vector2(4.5f,-4),new Vector2(10,-4));
 Partition("Gaestebad",0,new Vector2(4.5f,-1),new Vector2(10,-1),3.7f);
 // Separate WC access from the hall.
 // Extra opening in the existing right partition is formed below by replacing its front portion.
 var walls=new List<GameObject>();foreach(Transform t in group)if(t.name.StartsWith("Gaeste_Flur"))walls.Add(t.gameObject);
 foreach(var g in walls)Object.DestroyImmediate(g);
 Wall("Gaeste_Flur",new Vector3(4.5f,0,-8),new Vector3(4.5f,0,8),3,.16f,inside,new Opening(2,1.4f,0,2.45f,true),new Opening(9,1.4f,0,2.45f,true));Door(new Vector3(4.5f,0,-6),true);Door(new Vector3(4.5f,0,1),true);
 Label("HAUSWIRTSCHAFT",new Vector3(-7,1.8f,-7.8f));Label("WERKSTATT",new Vector3(-7,1.8f,7.8f),180);Label("HOBBYRAUM",new Vector3(0,1.8f,7.8f),180);Label("GÄSTEZIMMER",new Vector3(7,1.8f,7.8f),180);Label("GÄSTEBAD",new Vector3(7,1.8f,-3.85f));Label("GÄSTE-WC",new Vector3(7,1.8f,-7.8f));
 Group("1OG_Raumaufteilung");Partition("WC",3.2f,new Vector2(-10,-4),new Vector2(-5,-4),3);Solid("WC_Seite",3.2f,new Vector2(-5,-8),new Vector2(-5,-4));
 Label("KÜCHE",new Vector3(-7,5,7.8f),180);Label("ESSEN",new Vector3(0,5,7.8f),180);Label("WOHNEN",new Vector3(7,5,7.8f),180);Label("WC",new Vector3(-7,5,-7.8f));
 Group("2OG_Raumaufteilung");
 Partition("Buero",6.4f,new Vector2(-4.5f,-8),new Vector2(-4.5f,1.8f),6);
 Partition("Drittes_Schlafzimmer",6.4f,new Vector2(4.5f,-8),new Vector2(4.5f,1.8f),6);
 Solid("Buero_Rueckwand",6.4f,new Vector2(-10,1.8f),new Vector2(-4.5f,1.8f));Solid("Schlafzimmer3_Rueckwand",6.4f,new Vector2(4.5f,1.8f),new Vector2(10,1.8f));
 Wall("Schlafzimmer_Flur",new Vector3(-10,6.4f,4),new Vector3(10,6.4f,4),3,.16f,inside,new Opening(3,1.4f,0,2.45f,true),new Opening(17,1.4f,0,2.45f,true));Door(new Vector3(-7,6.4f,4));Door(new Vector3(7,6.4f,4));
 Solid("Schlafzimmer_Trennwand",6.4f,new Vector2(0,4),new Vector2(0,8));
 Partition("Familienbad",6.4f,new Vector2(-4.5f,-5),new Vector2(4.5f,-5),4.5f);
 Label("ELTERN",new Vector3(-5,8.2f,7.8f),180);Label("KINDERZIMMER / TUTORIAL",new Vector3(5,8.2f,7.8f),180);Label("BÜRO",new Vector3(-7,8.2f,-7.8f));Label("SCHLAFZIMMER 3",new Vector3(7,8.2f,-7.8f));Label("FAMILIENBAD",new Vector3(0,8.2f,-7.8f));
 for(int i=-1;i<3;i++)Stairs(i*3.2f,i==-1||i==2?1.15f:1.5f,i==-1?"Kellertreppe":i==2?"Dachbodentreppe":"Haupttreppe_"+i);
 Group("Treppenauge_Gelaender"); for(int level=0;level<=3;level++){float y=level*3.2f;foreach(int side in new[]{-1,1}){Beam("Brüstung_Seite",new Vector3(side*3.15f,y+1, -3.1f),new Vector3(side*3.15f,y+1,3),.07f,wood);for(float z=-3;z<=3;z+=.5f)Beam("Brüstung_Stab",new Vector3(side*3.15f,y,z),new Vector3(side*3.15f,y+1,z),.04f,metal);}Beam("Brüstung_Hinten",new Vector3(-3.15f,y+1,3),new Vector3(3.15f,y+1,3),.07f,wood);for(float x=-3;x<=3;x+=.5f)Beam("Brüstung_Stab",new Vector3(x,y,3),new Vector3(x,y+1,3),.04f,metal);Beam("Brüstung_Mitte",new Vector3(-1.15f,y+1,-3.2f),new Vector3(1.15f,y+1,-3.2f),.07f,wood);for(float x=-1;x<=1;x+=.4f)Beam("Brüstung_Stab",new Vector3(x,y,-3.2f),new Vector3(x,y+1,-3.2f),.04f,metal);} Group("Dach_und_Fassade");Hip("Villa_Walmdach",Vector3.zero,21,17,11.3f,14);foreach(int side in new[]{-1,1})Beam("Dachboden_Lampenhalter",new Vector3(side*6,11.22f,0),new Vector3(side*6,13.04f,0),.07f,metal);
 for(int i=0;i<4;i++){float y=i*3.2f-.13f;Box("Gesims_Vorne",new Vector3(0,y,-8.18f),new Vector3(20.5f,.18f,.22f),stone);Box("Gesims_Hinten",new Vector3(0,y,8.18f),new Vector3(20.5f,.18f,.22f),stone);foreach(int x in new[]{-1,1})Box("Gesims_Seite",new Vector3(x*10.18f,y,0),new Vector3(.22f,.18f,16.5f),stone);}
 foreach(float x in new[]{-9.6f,-2.6f,2.6f,9.6f})Box("Fassadenpilaster",new Vector3(x,4.75f,-8.2f),new Vector3(.30f,9.5f,.20f),stone);
 Group("Doppelgarage");
 Box("Garage_Boden",new Vector3(-13.6f,-.14f,-3.5f),new Vector3(7.2f,.28f,7.5f),stone);
 Wall("Garage_Front",new Vector3(-17.2f,0,-7.25f),new Vector3(-10,0,-7.25f),3.4f,.3f,plaster,new Opening(1.85f,2.9f,0,2.6f,true),new Opening(5.35f,2.9f,0,2.6f,true));
 // Garage doors are visibly open in this exploration blockout.
 Wall("Garage_Aussen",new Vector3(-17.2f,0,-7.25f),new Vector3(-17.2f,0,.25f),3.4f,.3f,plaster,new Opening(4,1.6f,1,1.3f));
 Wall("Garage_Hinten",new Vector3(-17.2f,0,.25f),new Vector3(-10,0,.25f),3.4f,.3f,plaster);
 Box("Garagendecke",new Vector3(-13.6f,3.42f,-3.5f),new Vector3(7.5f,.18f,7.8f),inside);Hip("Garage_Walmdach",new Vector3(-13.6f,0,-3.5f),7.9f,8.2f,3.6f,5.3f);
 LightAt(new Vector3(-13.6f,3.25f,-3.5f));
 Group("Aussentreppe");
 Box("Eingangspodest",new Vector3(0,3.05f,-9.1f),new Vector3(4.2f,.3f,2.2f),stone);
 Run("Aussentreppe_Links",new Vector3(-7,0,-9.6f),new Vector3(-2.1f,3.2f,-9.6f),1.7f,18);
 Run("Aussentreppe_Rechts",new Vector3(7,0,-9.6f),new Vector3(2.1f,3.2f,-9.6f),1.7f,18);
 foreach(float x in new[]{-1.7f,1.7f})Box("Podeststuetze",new Vector3(x,1.45f,-9.7f),new Vector3(.25f,2.9f,.3f),stone);
 Beam("Podest_Gelaender",new Vector3(-2,4.15f,-10.2f),new Vector3(2,4.15f,-10.2f),.08f,stone);
 for(float x=-2;x<=2;x+=.4f)Beam("Baluster",new Vector3(x,3.2f,-10.2f),new Vector3(x,4.1f,-10.2f),.065f,stone);
 Furnish();
 Group("Beleuchtung_und_Spieler");
 RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.5f,.56f,.64f);RenderSettings.ambientEquatorColor=new Color(.34f,.36f,.4f);RenderSettings.ambientGroundColor=new Color(.20f,.22f,.24f);RenderSettings.fog=false;
 var sun=new GameObject("Directional Light");sun.transform.SetParent(group);sun.transform.rotation=Quaternion.Euler(40,-25,0);var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.color=new Color(.88f,.93f,1);light.shadows=LightShadows.Soft;RenderSettings.sun=light;
 var player=new GameObject("Player_Kind");player.transform.SetParent(group);player.transform.position=new Vector3(0,.05f,-14);var cc=player.AddComponent<CharacterController>();cc.height=1.4f;cc.radius=.25f;cc.center=new Vector3(0,.7f,0);cc.stepOffset=.25f;cc.skinWidth=.025f;cc.slopeLimit=50;
 var cam=new GameObject("Main Camera");cam.tag="MainCamera";cam.transform.SetParent(player.transform,false);cam.transform.localPosition=new Vector3(0,1.25f,0);var c=cam.AddComponent<Camera>();c.nearClipPlane=.05f;c.farClipPlane=150;c.fieldOfView=75;c.clearFlags=CameraClearFlags.Skybox;cam.AddComponent<AudioListener>();var explorer=player.AddComponent<VillaExplorer>();explorer.view=c;
 var torch=new GameObject("Taschenlampe_F");torch.transform.SetParent(cam.transform,false);var tl=torch.AddComponent<Light>();tl.type=LightType.Spot;tl.range=18;tl.spotAngle=65;tl.intensity=4;tl.shadows=LightShadows.Soft;tl.enabled=false;
 EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/VillaPrototype.unity");
 AdjustSecondaryStairs();
 AdjustMainStairs();
 VillaPolish.Apply();
 VillaEntrance.Apply();
 VillaAccess.Apply();
 VillaRailingJoints.Apply();
 EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/VillaPrototype.unity",true)};
 AssetDatabase.SaveAssets();Selection.activeGameObject=root.gameObject;
 if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(-2,4,0),Quaternion.Euler(22,145,0),35);
 Debug.Log("VillaPrototype erstellt: Keller, EG, 1. OG, 2. OG, Dachboden, Doppelgarage und begehbare Treppen.");
 }
 static void Furnish(){
 Group("Orientierungsmoebel");
 // Kitchen, dining and lounge remain one connected living area.
 for(int i=0;i<6;i++)Box("Kuechenunterschrank",new Vector3(-9.1f,3.65f,1+i),new Vector3(1.2f,.9f,.95f),white);
 Box("Arbeitsplatte",new Vector3(-9.1f,4.15f,3.5f),new Vector3(1.3f,.1f,6),stone);
 Box("Kochinsel",new Vector3(-6.2f,3.7f,4.4f),new Vector3(1.4f,1,3),wood);
 Table(new Vector3(-.1f,3.2f,5.5f),new Vector2(3,1.3f),"Esstisch");
 Box("Sofa",new Vector3(7.8f,3.65f,5.3f),new Vector3(1, .9f,3.5f),wood);Box("Sofalehne",new Vector3(8.3f,4.05f,5.3f),new Vector3(.22f,1,3.5f),wood);Table(new Vector3(5.8f,3.2f,5.3f),new Vector2(1.2f,2),"Couchtisch");
 Bed(new Vector3(7,0,5.8f),1.6f);Bed(new Vector3(-5,6.4f,6),2);Bed(new Vector3(8.5f,6.4f,6),1.1f);Bed(new Vector3(8,6.4f,-5),1.1f);
 Table(new Vector3(-8,6.4f,-5.5f),new Vector2(2.4f,1),"Schreibtisch");Table(new Vector3(-8,0,6),new Vector2(3,1),"Werkbank");Table(new Vector3(0,0,6),new Vector2(2.5f,1.5f),"Hobbytisch");
 Box("Waschmaschine",new Vector3(-8.5f,.5f,-6.8f),Vector3.one,white);Box("Trockner",new Vector3(-7.2f,.5f,-6.8f),Vector3.one,white);
 foreach(var p in new[]{new Vector3(8,0,-2.7f),new Vector3(-7,3.2f,-6),new Vector3(-3.2f,6.4f,-6.8f),new Vector3(8,0,-6)}){Box("WC_Block",p+Vector3.up*.25f+(Mathf.Abs(p.z+2.7f)<.05f?Vector3.left*2:Vector3.zero),new Vector3(.45f,.5f,.7f),white);Box("Waschbecken",p.y>6?new Vector3(-1.6f,7.2f,-7.3f):p+Vector3.left*1.2f+Vector3.up*.8f,new Vector3(.7f,.15f,.5f),white);}
 Box("Gaestedusche",new Vector3(9.1f,.04f,-2.6f),new Vector3(1.2f,.08f,1.2f),white);
 Box("Badewanne",new Vector3(2.8f,6.7f,-6.7f),new Vector3(.9f,.6f,1.9f),white);
 }
 static void Table(Vector3 p,Vector2 size,string n){Box(n,p+Vector3.up*.76f,new Vector3(size.x,.1f,size.y),wood);foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(n+"_Bein",p+new Vector3(x*(size.x/2-.1f),.36f,z*(size.y/2-.1f)),new Vector3(.08f,.72f,.08f),wood);}
 static void Bed(Vector3 p,float width){Box("Bettgestell",p+Vector3.up*.22f,new Vector3(width,.44f,2.1f),wood);Box("Matratze",p+Vector3.up*.52f,new Vector3(width-.06f,.18f,2),white);Box("Kissen",p+new Vector3(0,.66f,.7f),new Vector3(width*.75f,.12f,.45f),inside);}
}
