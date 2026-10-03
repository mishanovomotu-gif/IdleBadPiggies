using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ContraptionMine
{
    public sealed partial class MineGame
    {
        int floorPage;
        static readonly Color Cyan=new(.04f,.76f,1),Wood=new(.9f,.49f,.13f);
        void BuildInterface()
        {
            Flat("Sky blue",0,0,1080,567,new Color(.055f,.51f,.75f));
            Box("Timber logo",24,22,405,140,Wood);
            Flat("Wood grain",51,85,350,5,new Color(.64f,.31f,.1f));
            var logo=Label("CONTRAPTION",40,26,372,57,41,new Color(1,.9f,.32f));logo.alignment=TextAlignmentOptions.Center;
            var name=Label("MINE",43,76,365,74,63,Color.white);name.alignment=TextAlignmentOptions.Center;
            foreach(float x in new[]{45f,400f})foreach(float y in new[]{40f,139f})RoundDot("Bolt",x,y,14,new Color(.75f,.88f,.95f));
            Box("Wallet",449,28,470,127,Blue);RoundDot("Coin",470,67,48,Gold);RoundDot("Coin shine",482,76,14,new Color(1,.96f,.64f));wallet=Label("",532,39,365,101,29,Color.white);
            Button("DEV",941,39,110,97,()=>{debug=!debug;Refresh();},Blue,24);
            Box("Course heading",18,508,1044,62,Blue);hud=Label("",44,511,998,50,29,Color.white);
            Box("Cargo dashboard",18,1057,1044,96,Blue);telemetry=Label("",42,1064,996,81,24,Color.white);
            Box("Workshop",8,1157,1064,759,Blue);Box("Workshop inset",23,1234,1034,602,Panel);
            Label("BUILD YOUR HAULER",113,1168,894,60,44,Color.white);Icon(PartType.Frame,38,1174,59,55);
            notice=Label("",43,1231,994,61,23,Gold);
        }
        RectTransform Rect(GameObject o,float x,float y,float w,float h,Transform parent=null)
        {
            var r=o.GetComponent<RectTransform>();r.SetParent(parent?parent:canvas.transform,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        GameObject Flat(string n,float x,float y,float w,float h,Color c,bool dyn=false)
        {
            var o=new GameObject(n,typeof(RectTransform),typeof(Image));Rect(o,x,y,w,h);o.GetComponent<Image>().color=c;o.GetComponent<Image>().raycastTarget=false;if(dyn)dynamicUI.Add(o);return o;
        }
        GameObject Box(string n,float x,float y,float w,float h,Color c,bool dyn=false)
        {
            var o=Flat(n,x,y,w,h,c,dyn);var im=o.GetComponent<Image>();im.sprite=MineArt.Panel;im.type=Image.Type.Sliced;return o;
        }
        void RoundDot(string n,float x,float y,float diameter,Color color,bool dyn=false)
        {
            VehiclePhysics.InitSprites();var o=Flat(n,x,y,diameter,diameter,color,dyn);o.GetComponent<Image>().sprite=VehiclePhysics.Circle;
        }
        TMP_Text Label(string s,float x,float y,float w,float h,int size,Color c,bool dyn=false)
        {
            var o=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));Rect(o,x,y,w,h);var t=o.GetComponent<TextMeshProUGUI>();t.text=s;t.fontSize=size;t.fontStyle=FontStyles.Bold;t.color=c;t.raycastTarget=false;t.verticalAlignment=VerticalAlignmentOptions.Middle;t.font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");t.enableWordWrapping=true;
            var shadow=o.AddComponent<Shadow>();shadow.effectColor=new Color(.015f,.06f,.12f,.75f);shadow.effectDistance=new Vector2(1.5f,-2.5f);if(dyn)dynamicUI.Add(o);return t;
        }
        Button Button(string s,float x,float y,float w,float h,Action action,Color color,int size=26,bool dyn=false)
        {
            var o=Box(s,x,y,w,h,color,dyn);o.GetComponent<Image>().raycastTarget=true;var b=o.AddComponent<Button>();b.onClick.AddListener(()=>action());var colors=b.colors;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.7f,.8f,.9f);colors.disabledColor=new Color(.42f,.5f,.59f,.85f);b.colors=colors;
            var text=Label(s,0,0,w,h,size,Color.white);text.rectTransform.SetParent(o.transform,false);text.rectTransform.anchoredPosition=Vector2.zero;text.alignment=TextAlignmentOptions.Center;return b;
        }
        void Icon(PartType type,float x,float y,float w,float h,bool dyn=false,Transform parent=null)
        {
            var o=Flat(type+" icon",x,y,w,h,Color.white,dyn);var im=o.GetComponent<Image>();im.sprite=MineArt.Icon(type);im.preserveAspect=true;if(parent!=null){o.transform.SetParent(parent,false);o.GetComponent<RectTransform>().anchoredPosition=new Vector2(x,-y);}
        }
        void Refresh()
        {
            foreach(var o in dynamicUI)Destroy(o);dynamicUI.Clear();notice.text=message;
            int pageCount=(floors.Length+2)/3;floorPage=Mathf.Clamp(floorPage,0,pageCount-1);
            Button("<",30,173,72,42,()=>{if(!running){floorPage=(floorPage+pageCount-1)%pageCount;Refresh();}},Blue,26,true).interactable=!running;
            var title=Label($"MINE FLOORS {floorPage*3+1}–{Mathf.Min(floors.Length,floorPage*3+3)} / {floors.Length}",118,168,845,43,24,Color.white,true);title.alignment=TextAlignmentOptions.Center;
            Button(">",978,173,72,42,()=>{if(!running){floorPage=(floorPage+1)%pageCount;Refresh();}},Blue,26,true).interactable=!running;
            for(int rowIndex=0;rowIndex<3;rowIndex++)
            {
                int i=floorPage*3+rowIndex;if(i>=floors.Length)break;int fi=i;float y=220+rowIndex*94;var f=state.floors[i];var d=floors[i];
                var row=Button("",30,y,1020,87,()=>SelectFloor(fi),f.unlocked?(i==Active?Cyan:Blue):new Color(.18f,.35f,.48f),29,true);row.interactable=!running;
                Box("Floor number",43,y+9,69,67,f.unlocked?Blue:Panel,true);var number=Label((i+1).ToString(),43,y+9,69,67,42,Color.white,true);number.alignment=TextAlignmentOptions.Center;
                Label($"FLOOR {i+1} · {d.distance:0} m",130,y+8,625,37,30,Color.white,true);
                Label(f.unlocked?$"{f.automated?.rate??0:0}/min  ·  BEST {f.best:0}/min":$"{d.unlockPrice:N0} coins · automate Floor {i}",130,y+45,690,31,21,f.unlocked?new Color(1,.9f,.34f):new Color(.75f,.85f,.95f),true);
                Box("State",820,y+12,210,57,f.automated!=null?new Color(.12f,.89f,.18f):Panel,true);var badge=Label(f.unlocked?(f.automated!=null?"AUTO":"BUILD"):"LOCKED",820,y+12,210,57,27,Color.white,true);badge.alignment=TextAlignmentOptions.Center;
                var mini=f.automated?.vehicle??f.design;if(mini!=null&&f.unlocked)foreach(var p in mini.parts)Icon(p.type,654+p.x*18,y+10+(4-p.y)*13,20,18,true);
            }
            Label($"{Floor.challenge}  ·  DELIVER {Floor.requiredOre} ORE",40,576,996,40,26,Gold,true);
            for(int y=4;y>=0;y--)for(int x=0;x<8;x++)
            {
                int gx=x,gy=y;var p=Current.design.parts.Find(q=>q.x==gx&&q.y==gy);bool marked=moving&&gx==moveX&&gy==moveY;
                var cell=Button("",35+x*127,1301+(4-y)*63,119,58,()=>Cell(gx,gy),marked?Gold:new Color(.05f,.42f,.6f),21,true);cell.GetComponent<Image>().sprite=MineArt.Slot;cell.interactable=!running;
                if(p!=null)Icon(p.type,31,1,57,55,false,cell.transform);else{var dot=Label("·",0,0,119,58,26,new Color(.21f,.65f,.78f));dot.rectTransform.SetParent(cell.transform,false);dot.rectTransform.anchoredPosition=Vector2.zero;dot.alignment=TextAlignmentOptions.Center;}
            }
            for(int i=0;i<8;i++)
            {
                int pi=i;var d=parts[i];int count=Current.design.parts.FindAll(p=>p.type==d.type).Count;bool unlocked=state.partsUnlocked[i];float x=30+(i%4)*257,y=1622+(i/4)*76;
                var b=Button("",x,y,248,70,()=>Pick(pi),selected==d.type&&!delete&&!moving?Cyan:Blue,22,true);b.interactable=!running;
                Icon(d.type,7,5,61,58,false,b.transform);var label=Label(unlocked?$"{d.label}\n<size=19>Lv {state.levels[i]} · {d.stock-count} left</size>":$"{d.label}\n<size=19>{d.unlockCost} coins</size>",74,4,165,62,23,Color.white);label.rectTransform.SetParent(b.transform,false);label.rectTransform.anchoredPosition=new Vector2(74,-4);
            }
            Button(delete?"DELETE ✓":"DELETE",30,1780,160,43,()=>{delete=!delete;moving=false;Refresh();},delete?Cyan:Blue,20,true).interactable=!running;
            Button(moving?"MOVE ✓":"MOVE",200,1780,150,43,()=>{moving=!moving;delete=false;moveX=-1;Refresh();},moving?Cyan:Blue,20,true).interactable=!running;
            Button("UPGRADE",360,1780,180,43,Upgrade,Blue,20,true).interactable=!running;
            Button("BLUEPRINT",550,1780,230,43,()=>{Current.design=Floor.blueprint.Copy();message="Starter blueprint loaded. Test it, then improve it.";Save();Refresh();Preview();},Blue,20,true).interactable=!running;
            Button("RESET",790,1780,260,43,()=>{Current.design=new VehicleDefinition();message="Empty workshop. Place connected parts or use BLUEPRINT.";Save();Refresh();Preview();},Blue,20,true).interactable=!running;
            Button(running?"STOP / MODIFY":"▶  TEST RUN",30,1840,505,69,()=>{if(running)EndRun(false,"Run stopped. Modify your hauler and try again.");else StartRun();},new Color(.16f,.97f,.15f),34,true);
            Button(Current.automated!=null?"REPLACE AUTO":"AUTOMATE",550,1840,500,69,Automate,Cyan,30,true).interactable=!running&&Current.lastSuccess!=null&&Current.lastSuccess.ore>=Floor.requiredOre;
            if(!running&&Current.lastSuccess!=null){var r=Current.lastSuccess;Box("Run result",28,1005,1024,45,Blue,true);Label($"LAST SUCCESS   {r.ore} ore · {r.seconds:0.0}s · {r.rate:0}/min",46,1007,986,39,25,Gold,true);}
            if(pendingOffline>0)OfflinePanel();if(debug)DebugPanel();
        }
    }
}
