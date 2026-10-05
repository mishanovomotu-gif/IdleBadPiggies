#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
namespace ContraptionMine {
// Editor-only integration validation; excluded from player builds.
public sealed class PrototypeSmoke : MonoBehaviour {
 static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Spawn(){if(SessionState.GetBool("ContraptionMine.Smoke",false))new GameObject("Prototype validation").AddComponent<PrototypeSmoke>();}
 static object Get(MineGame g,string field)=>typeof(MineGame).GetField(field,Flags).GetValue(g);
 static void Call(MineGame g,string name,params object[] args)=>typeof(MineGame).GetMethod(name,Flags).Invoke(g,args);
 IEnumerator Start(){yield return null;yield return null;Application.logMessageReceived+=OnLog;var g=FindFirstObjectByType<MineGame>();var s=(MineState)Get(g,"state");
  Screen.SetResolution(540,SessionState.GetBool("ContraptionMine.SnapshotPreview",false)?1171:960,false);s.coins=1000000000;yield return null;
  if(SessionState.GetBool("ContraptionMine.SnapshotPreview",false)){
   FindFirstObjectByType<MineViewport>().SetValidationSize(540,1171);
   s=JsonUtility.FromJson<MineState>(System.IO.File.ReadAllText("/tmp/contraption-idle-tested-state.json"));typeof(MineGame).GetField("state",Flags).SetValue(g,s);typeof(MineGame).GetField("pendingOffline",Flags).SetValue(g,0d);
   int page=SessionState.GetInt("ContraptionMine.SnapshotScreen",0);Call(g,"SelectFloor",(page==1||page==6||page==7)?0:page==4?9:g.floors.Length-1);Call(g,"SetScreen",(page==6||page==7)?MineScreen.Workshop:page==4?MineScreen.Mine:page==5?MineScreen.Prestige:(MineScreen)page);
   if(page==6||page==7) {
    ((System.Collections.Generic.List<GameObject>)Get(g,"dynamicUI")).Clear();
    Call(g,"StartRun"); yield return new WaitForSeconds(2);
    ((System.Collections.Generic.List<GameObject>)Get(g,"dynamicUI")).Clear(); Call(g,"Refresh");
    int dashboards=0;foreach(Transform child in (RectTransform)Get(g,"designRoot")) if(child.gameObject.activeSelf) { if(child.name=="Workshop"||child.name=="Workshop inset"||child.name=="SAVE THIS HAULER"){Fail("Workshop leaked into run view after lost UI cache");yield break;} if(child.name=="Run dashboard")dashboards++; }
    if(dashboards!=1){Fail("Duplicate run dashboard");yield break;}
    if(page==7){typeof(MineGame).GetField("progress",Flags).SetValue(g,g.floors[s.selectedFloor].distance);typeof(MineGame).GetField("elapsed",Flags).SetValue(g,12f);Call(g,"EndRun",true,"UI result fixture."); ((System.Collections.Generic.List<GameObject>)Get(g,"dynamicUI")).Clear();Call(g,"Refresh");int results=0;foreach(Transform child in (RectTransform)Get(g,"designRoot"))if(child.gameObject.activeSelf){if(child.name=="Workshop"||child.name=="STOP & EDIT"){Fail("Old controls leaked into result view");yield break;}if(child.name=="Test result")results++;}if(results!=1){Fail("Duplicate result panel");yield break;}}
    Debug.Log("UI HIERARCHY CLEANUP PASSED: "+page);
   }
   if(page==1) {
    var content=(RectTransform)Get(g,"designRoot");var header=content.Find("Persistent header");
    while(header.childCount>0)header.GetChild(0).SetParent(content,false);Destroy(header.gameObject);yield return null;
    Call(g,"Refresh");if(content.Find("Persistent header")==null||((TMPro.TMP_Text)Get(g,"wallet")).transform.parent!=content.Find("Persistent header")){Fail("Legacy header migration failed");yield break;}
    Debug.Log("LEGACY UI HEADER MIGRATION PASSED");
    var beforeDrag=s.floors[s.selectedFloor].design.Copy();float extra=content.sizeDelta.y-1920;
    Vector2 TallPoint(float x,float y)=>RectTransformUtility.WorldToScreenPoint(null,content.TransformPoint(new Vector3(x-540,content.sizeDelta.y*.5f-y-(y>=900?extra:0),0)));
    var handleObject=new GameObject("Tall drag check",typeof(RectTransform));Call(g,"WireDrag",handleObject,PartType.Frame,-1,-1,true);var handle=handleObject.GetComponent<WorkshopDrag>();
    var e=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=-1,position=TallPoint(40,1640)};handle.OnBeginDrag(e);e.position=TallPoint(846,1219);handle.OnDrag(e);handle.OnEndDrag(e);
    if(!s.floors[s.selectedFloor].design.parts.Exists(p=>p.x==7&&p.y==4)){Fail("Tall portrait drag coordinates failed");yield break;}Destroy(handleObject);
    handleObject=new GameObject("Tall side deletion check",typeof(RectTransform));Call(g,"WireDrag",handleObject,PartType.Frame,7,4,true);handle=handleObject.GetComponent<WorkshopDrag>();e.position=TallPoint(846,1219);handle.OnBeginDrag(e);e.position=TallPoint(950,1350);handle.OnDrag(e);handle.OnEndDrag(e);
    if(s.floors[s.selectedFloor].design.parts.Exists(p=>p.x==7&&p.y==4)){Fail("Tall portrait side deletion failed");yield break;}Destroy(handleObject);s.floors[s.selectedFloor].design=beforeDrag;Call(g,"Refresh");Call(g,"Preview");Debug.Log("TALL PORTRAIT DRAG PASSED");
   }
   if(page==4){typeof(MineGame).GetField("floorDetails",Flags).SetValue(g,true);Call(g,"Refresh");}
   if(page==5){typeof(MineGame).GetField("prestigeConfirm",Flags).SetValue(g,true);Call(g,"Refresh");}
   yield return null;WarmWorld(g);yield return null;yield return null;Capture(g,"/tmp/contraption-idle-screen-"+page+".png");Debug.Log("IDLE UI PREVIEW PASSED: "+page);EditorApplication.Exit(0);yield break;
  }
  if(SessionState.GetBool("ContraptionMine.PreviewOnly",false)){
   for(int i=0;i<g.floors.Length;i++){s.floors[i].unlocked=true;float seconds=20;var record=new RunRecord{vehicle=g.floors[i].blueprint.Copy(),ore=g.floors[i].requiredOre,seconds=seconds,rate=g.floors[i].requiredOre/seconds*60*g.floors[i].multiplier*g.floors[i].OreValue};s.floors[i].automated=record;s.floors[i].lastSuccess=record;s.floors[i].best=record.rate;}
   Call(g,"SelectFloor",0);yield return null;WarmWorld(g);yield return null;yield return null;Capture(g);Debug.Log("CONTRAPTION MINE PREVIEW PASSED");EditorApplication.Exit(0);yield break;
  }
  Call(g,"SetScreen",MineScreen.Workshop); yield return null;
  var originalBuild=s.floors[0].design.Copy(); var root=(RectTransform)Get(g,"designRoot");
  var testTarget=new GameObject("Drag integration check",typeof(RectTransform));
  Call(g,"WireDrag",testTarget,PartType.Frame,-1,-1,true); var drag=testTarget.GetComponent<WorkshopDrag>();
  Vector2 Point(float x,float y)=>RectTransformUtility.WorldToScreenPoint(null,root.TransformPoint(new Vector3(x-540,960-y,0)));
  var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=-1,position=Point(40,1640)};
  drag.OnBeginDrag(pointer); pointer.position=Point(188+7*88+42,1177+42);drag.OnDrag(pointer);drag.OnEndDrag(pointer);
  if(!s.floors[0].design.parts.Exists(p=>p.type==PartType.Frame&&p.x==7&&p.y==4)){Fail("Palette drag failed");yield break;}
  Call(g,"RestoreBuild",false);if(JsonUtility.ToJson(s.floors[0].design)!=JsonUtility.ToJson(originalBuild)){Fail("Undo failed");yield break;}
  Call(g,"RestoreBuild",true);if(!s.floors[0].design.parts.Exists(p=>p.x==7&&p.y==4)){Fail("Redo failed");yield break;}
  Destroy(testTarget);testTarget=new GameObject("Move integration check",typeof(RectTransform));Call(g,"WireDrag",testTarget,PartType.Frame,7,4,true);drag=testTarget.GetComponent<WorkshopDrag>();
  pointer.position=Point(846,1219);drag.OnBeginDrag(pointer);pointer.position=Point(758,1219);drag.OnDrag(pointer);drag.OnEndDrag(pointer);
  if(!s.floors[0].design.parts.Exists(p=>p.x==6&&p.y==4)||s.floors[0].design.parts.Exists(p=>p.x==7&&p.y==4)){Fail("Grid move failed");yield break;}
  Destroy(testTarget);testTarget=new GameObject("Remove integration check",typeof(RectTransform));Call(g,"WireDrag",testTarget,PartType.Frame,6,4,true);drag=testTarget.GetComponent<WorkshopDrag>();
  pointer.position=Point(758,1219);drag.OnBeginDrag(pointer);pointer.position=Point(950,1350);drag.OnDrag(pointer);drag.OnEndDrag(pointer);
  if(s.floors[0].design.parts.Exists(p=>p.x==6&&p.y==4)){Fail("Side deletion failed");yield break;}
  Destroy(testTarget);s.floors[0].design=originalBuild;Call(g,"BuildTrack");Call(g,"Refresh");Debug.Log("WORKSHOP DRAG PASSED: palette, snap, move, side deletion, undo, redo");
  for(int floor=0;floor<g.floors.Length;floor++){
   if(floor>=9){
    double gateCoins=s.coins;int selected=s.selectedFloor;
    if(stateIncome(s)<g.floors[floor].outputGate){Call(g,"SelectFloor",floor);if(s.selectedFloor!=selected||s.coins<gateCoins){Fail("Deep floor output gate bypassed or charged");yield break;}}
    typeof(MineGame).GetField("buyMode",Flags).SetValue(g,10);Call(g,"SelectFloor",0);
    int guard=0;while(s.Income<g.floors[floor].outputGate&&guard++<10){Call(g,"BuyProduction",ProductionUpgrade.Crew);Call(g,"BuyProduction",ProductionUpgrade.Loading);}
    if(s.Income<g.floors[floor].outputGate){Fail("Upgrades did not reach output gate");yield break;}
   }
   double coins=s.coins;int unlock=s.floors[floor].unlocked?0:g.floors[floor].unlockPrice;Call(g,"SelectFloor",floor);if(s.selectedFloor!=floor||Math.Abs((coins-s.coins)-unlock)>.01){Fail("Paid floor unlocking failed");yield break;}s.floors[floor].design=g.floors[floor].blueprint.Copy();Call(g,"BuildTrack");Call(g,"Refresh");yield return null;
   Call(g,"StartRun");Time.timeScale=3;float waited=0;while((bool)Get(g,"running")&&waited<35){waited+=Time.deltaTime;yield return null;}
   var r=s.floors[floor].lastSuccess;if(r==null||r.ore<g.floors[floor].requiredOre){Fail("Runtime floor "+(floor+1)+" failed: "+Get(g,"message"));yield break;}
   float expected = r.ore / r.seconds * 60 * g.floors[floor].multiplier * g.floors[floor].OreValue;
   if(Mathf.Abs(expected-r.rate)>.1f||s.floors[floor].lastAttempt==null||!s.floors[floor].lastAttempt.success){Fail("Ore scoring or run feedback failed");yield break;}
   Debug.Log($"RUNTIME FLOOR {floor+1} PASSED: {r.ore} ore in {r.seconds:0.00}s, {r.rate:0.0}/min");Call(g,"Automate");
   double bank=s.floors[floor].stored;s.Tick(1);if(s.floors[floor].stored<=bank){Fail("New floor did not store manual production");yield break;}
   Call(g,"HireManager");if(!s.floors[floor].manager||s.floors[floor].stored!=0){Fail("Manager action did not automate collection");yield break;}
   float rate=s.floors[floor].automated.rate;s.floors[floor].design.parts.Clear();if(s.floors[floor].automated.rate!=rate){Fail("Editing changed automated income");yield break;}s.floors[floor].design=r.vehicle.Copy();
  }
  Call(g,"SelectFloor",0);Call(g,"BuildTrack");Call(g,"Refresh");yield return null;
  // A missing engine must block the run without spending coins.
  var original=s.floors[0].design.Copy();s.floors[0].design.parts.RemoveAll(p=>g.parts[(int)p.type].IsEngine);double before=s.coins;Call(g,"StartRun");if((bool)Get(g,"running")||s.coins<before){Fail("Invalid construction was accepted or charged");yield break;}s.floors[0].design=original;Call(g,"BuildTrack");Call(g,"Refresh");yield return null;
  float preserved=s.floors[0].automated.rate;
  string[] failures={"Fell", "Flipped", "Stalled", "Time", "disconnected"};
  for(int test=0;test<failures.Length;test++){
   Call(g,"StartRun");yield return null;var rig=(VehiclePhysics)Get(g,"vehicle");
   if(test==0)rig.body.position=new Vector2(5,-8);
   if(test==1){rig.body.rotation=180;typeof(MineGame).GetField("flipped",Flags).SetValue(g,3f);}
   if(test==2){rig.body.linearVelocity=Vector2.zero;typeof(MineGame).GetField("elapsed",Flags).SetValue(g,4f);typeof(MineGame).GetField("stalled",Flags).SetValue(g,3f);}
   if(test==3)typeof(MineGame).GetField("elapsed",Flags).SetValue(g,30f);
   if(test==4)rig.broken=true;
   yield return null;yield return null;
   var result=s.floors[0].lastAttempt;
   if((bool)Get(g,"running")||result==null||result.success||!result.reason.Contains(failures[test])||string.IsNullOrEmpty(result.hint)||s.floors[0].automated.rate!=preserved){Fail("Failure feedback or income preservation failed: "+failures[test]);yield break;}
  }
  Debug.Log("FAILURE FEEDBACK PASSED: fall, flip, stall, timeout, wheel break");
  Call(g,"SelectFloor",g.floors.Length-1);Call(g,"BuildTrack");Call(g,"Refresh");yield return null;
  Call(g,"Save");var loaded=SaveManager.Load();if(loaded.floors[g.floors.Length-1].automated==null||loaded.floors[0].design.parts.Count!=original.parts.Count||loaded.levels.Length!=g.parts.Length||loaded.floors[0].lastAttempt==null){Fail("Save/load lost configuration");yield break;}
  s.Tick(60);string snapshot=JsonUtility.ToJson(s);int partLevel=s.levels[3];string design=JsonUtility.ToJson(s.floors[0].design);
  Call(g,"SetScreen",MineScreen.Prestige);if(!MineEconomy.CanPrestige(s)){Fail("Prestige did not unlock after earning enough");yield break;}
  Call(g,"DoPrestige");if(s.coins!=100||s.floors[8].unlocked||s.floors[0].automated!=null||s.levels[3]!=partLevel||JsonUtility.ToJson(s.floors[0].design)!=design){Fail("Prestige game action lost progress or failed to reset");yield break;}
  Call(g,"Automate");if(s.floors[0].automated==null||(bool)Get(g,"running")){Fail("Saved run could not resume after prestige");yield break;}
  s=JsonUtility.FromJson<MineState>(snapshot);typeof(MineGame).GetField("state",Flags).SetValue(g,s);Call(g,"SelectFloor",g.floors.Length-1);Call(g,"Save");System.IO.File.WriteAllText("/tmp/contraption-idle-tested-state.json",JsonUtility.ToJson(s));
  Debug.Log("PRESTIGE FLOW PASSED: reset, creative progress retention, and saved-run reuse");
  typeof(MineGame).GetField("showResults",Flags).SetValue(g,true);Call(g,"SetScreen",MineScreen.Mine);typeof(MineGame).GetField("message",Flags).SetValue(g,"Idle mine running. Upgrade floors or collect stored earnings.");Call(g,"Refresh");Shader.WarmupAllShaders();Time.timeScale=1;yield return null;yield return null;yield return null;WarmWorld(g);yield return null;Capture(g);
  Debug.Log("IDLE PROGRESSION PASSED: deep floor gates and upgrades via game actions");Debug.Log("CONTRAPTION MINE RUNTIME SMOKE PASSED");EditorApplication.Exit(0);
 }
 static double stateIncome(MineState state)=>state.Income;
 static void WarmWorld(MineGame game){var camera=(Camera)Get(game,"cam");var texture=new RenderTexture(540,Mathf.RoundToInt(((RectTransform)Get(game,"designRoot")).sizeDelta.y*.5f),24);texture.Create();camera.targetTexture=texture;camera.Render();camera.targetTexture=null;texture.Release();Destroy(texture);}
 static void Capture(MineGame game,string path="/tmp/contraption-preview.png"){
  var world=(Camera)Get(game,"cam");var root=(RectTransform)Get(game,"designRoot");int captureHeight=Mathf.RoundToInt(root.sizeDelta.y*.5f);root.anchoredPosition=Vector2.zero;FindFirstObjectByType<MineViewport>().enabled=false;var canvas=FindFirstObjectByType<Canvas>();var rt=new RenderTexture(540,captureHeight,24);rt.Create();RenderTexture.active=rt;GL.Clear(true,true,new Color(.025f,.08f,.15f));world.targetTexture=rt;world.Render();RenderTexture.active=rt;var worldShot=new Texture2D(540,captureHeight,TextureFormat.RGBA32,false);worldShot.ReadPixels(new Rect(0,0,540,captureHeight),0,0);worldShot.Apply();var worldPixels=worldShot.GetPixels();
  var ui=new GameObject("UI capture").AddComponent<Camera>();ui.transform.position=new Vector3(0,0,-100);ui.orthographic=true;ui.clearFlags=CameraClearFlags.SolidColor;ui.backgroundColor=Color.clear;ui.cullingMask=1<<5;ui.targetTexture=rt;
  foreach(var tr in canvas.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=5;
  var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.enabled=false;canvas.scaleFactor=.5f;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=ui;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();foreach(var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())graphic.SetAllDirty();foreach(var label in canvas.GetComponentsInChildren<TMPro.TMP_Text>())label.ForceMeshUpdate(true);Canvas.ForceUpdateCanvases();ui.Render();
  RenderTexture.active=rt;var shot=new Texture2D(540,captureHeight,TextureFormat.RGBA32,false);shot.ReadPixels(new Rect(0,0,540,captureHeight),0,0);shot.Apply();var pixels=shot.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=Color.Lerp(worldPixels[i],pixels[i],pixels[i].a);shot.SetPixels(pixels);shot.Apply();Destroy(worldShot);System.IO.File.WriteAllBytes(path,shot.EncodeToPNG());RenderTexture.active=null;world.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=true;Destroy(ui.gameObject);rt.Release();Destroy(rt);Destroy(shot);
 }
 void OnLog(string condition,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)Fail(condition);}
 void Fail(string reason){Application.logMessageReceived-=OnLog;var rig=FindFirstObjectByType<VehiclePhysics>();if(rig!=null)Debug.Log($"FAILED RIG: x={rig.body.position.x:0.0}, y={rig.body.position.y:0.0}, angle={rig.body.rotation:0.0}, mass={rig.totalMass:0.0}");Debug.LogError("SMOKE FAILED: "+reason);EditorApplication.Exit(1);}
}
}
#endif
