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
  Screen.SetResolution(540,960,false);yield return null;
  for(int floor=0;floor<g.floors.Length;floor++){
   s.selectedFloor=floor;s.floors[floor].unlocked=true;s.floors[floor].design=g.floors[floor].blueprint.Copy();Call(g,"BuildTrack");Call(g,"Refresh");yield return null;
   Call(g,"StartRun");Time.timeScale=3;float waited=0;while((bool)Get(g,"running")&&waited<35){waited+=Time.deltaTime;yield return null;}
   var r=s.floors[floor].lastSuccess;if(r==null||r.ore<g.floors[floor].requiredOre){Fail("Runtime floor "+(floor+1)+" failed: "+Get(g,"message"));yield break;}
   Debug.Log($"RUNTIME FLOOR {floor+1} PASSED: {r.ore} ore in {r.seconds:0.00}s, {r.rate:0.0}/min");Call(g,"Automate");float rate=s.floors[floor].automated.rate;s.floors[floor].design.parts.Clear();if(s.floors[floor].automated.rate!=rate){Fail("Editing changed automated income");yield break;}s.floors[floor].design=r.vehicle.Copy();
  }
  s.selectedFloor=0;Call(g,"BuildTrack");Call(g,"Refresh");yield return null;
  // A missing engine must block the run without spending coins.
  var original=s.floors[0].design.Copy();s.floors[0].design.parts.RemoveAll(p=>g.parts[(int)p.type].IsEngine);double before=s.coins;Call(g,"StartRun");if((bool)Get(g,"running")||s.coins<before){Fail("Invalid construction was accepted or charged");yield break;}s.floors[0].design=original;Call(g,"BuildTrack");Call(g,"Refresh");yield return null;
  Call(g,"Save");var loaded=SaveManager.Load();if(loaded.floors[g.floors.Length-1].automated==null||loaded.floors[0].design.parts.Count!=original.parts.Count){Fail("Save/load lost configuration");yield break;}
  typeof(MineGame).GetField("message",Flags).SetValue(g,"Ready to build. Change cargo, power or wheel placement, then retest.");Call(g,"Refresh");Time.timeScale=1;yield return null;yield return null;yield return null;Capture(g);
  Debug.Log("CONTRAPTION MINE RUNTIME SMOKE PASSED");EditorApplication.Exit(0);
 }
 static void Capture(MineGame game){
  var world=(Camera)Get(game,"cam");var canvas=FindFirstObjectByType<Canvas>();var rt=new RenderTexture(540,960,24);rt.Create();RenderTexture.active=rt;GL.Clear(true,true,new Color(.025f,.08f,.15f));world.targetTexture=rt;world.Render();RenderTexture.active=rt;var worldShot=new Texture2D(540,960,TextureFormat.RGBA32,false);worldShot.ReadPixels(new Rect(0,0,540,960),0,0);worldShot.Apply();var worldPixels=worldShot.GetPixels();
  var ui=new GameObject("UI capture").AddComponent<Camera>();ui.transform.position=new Vector3(0,0,-100);ui.orthographic=true;ui.clearFlags=CameraClearFlags.SolidColor;ui.backgroundColor=Color.clear;ui.cullingMask=1<<5;ui.targetTexture=rt;
  foreach(var tr in canvas.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=5;
  canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=ui;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();ui.Render();
  RenderTexture.active=rt;var shot=new Texture2D(540,960,TextureFormat.RGBA32,false);shot.ReadPixels(new Rect(0,0,540,960),0,0);shot.Apply();var pixels=shot.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=Color.Lerp(worldPixels[i],pixels[i],pixels[i].a);shot.SetPixels(pixels);shot.Apply();Destroy(worldShot);System.IO.File.WriteAllBytes("/tmp/contraption-preview.png",shot.EncodeToPNG());RenderTexture.active=null;world.targetTexture=null;rt.Release();Destroy(rt);Destroy(shot);
 }
 void OnLog(string condition,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)Fail(condition);}
 void Fail(string reason){Application.logMessageReceived-=OnLog;Debug.LogError("SMOKE FAILED: "+reason);EditorApplication.Exit(1);}
}
}
#endif
