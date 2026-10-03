using System.Collections.Generic;
using UnityEngine;
namespace ContraptionMine
{
    // Shared runtime art keeps the workshop icons and actual physical parts identical.
    public static class MineArt
    {
        static readonly Dictionary<PartType, Sprite> Icons = new();
        static Sprite panel, slot;
        public static Sprite Panel => panel ??= MakePanel(false);
        public static Sprite Slot => slot ??= MakePanel(true);
        public static Sprite Icon(PartType type)
        {
            if (Icons.TryGetValue(type, out var icon)) return icon;
            var p = new Painter(256);
            Color ink = new(.10f, .13f, .19f), steel = new(.55f, .67f, .8f), gold = new(1, .65f, .12f), wood = new(.8f, .36f, .08f), red = new(.88f, .12f, .14f);
            switch (type)
            {
                case PartType.Frame:
                    p.Round(25,25,206,206,18,ink);p.Round(34,34,188,188,12,wood);p.Round(45,45,166,166,7,gold);
                    p.Round(69,69,118,118,5,Color.clear,true);p.Line(53,55,53,200,7,new Color(1,.82f,.37f));p.Line(68,209,199,209,6,new Color(1,.82f,.37f));
                    foreach(var x in new[]{49,207})foreach(var y in new[]{49,207}){p.Circle(x,y,12,ink);p.Circle(x,y,8,steel);p.Circle(x-2,y+2,3,Color.white);}break;
                case PartType.Wheel: case PartType.HeavyWheel:
                    p.Circle(128,126,113,ink);p.Circle(128,126,103,new Color(.22f,.25f,.31f));
                    for(int i=0;i<14;i++){float a=i*Mathf.PI*2/14;p.Line(128+Mathf.Cos(a)*85,126+Mathf.Sin(a)*85,128+Mathf.Cos(a+.09f)*104,126+Mathf.Sin(a+.09f)*104,12,type==PartType.HeavyWheel?new Color(.5f,.56f,.63f):new Color(.38f,.43f,.5f));}
                    p.Circle(128,126,74,ink);p.Circle(128,126,64,steel);p.Circle(128,126,45,new Color(.31f,.41f,.56f));p.Circle(128,126,29,ink);p.Circle(128,126,23,gold);p.Circle(121,136,8,new Color(1,.85f,.4f));
                    for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;p.Circle(128+Mathf.Cos(a)*51,126+Mathf.Sin(a)*51,5,new Color(.8f,.87f,.92f));}break;
                case PartType.Engine: case PartType.PowerfulEngine:
                    p.Round(47,26,168,149,20,ink);p.Round(54,33,153,134,14,type==PartType.Engine?red:new Color(1,.37f,.08f));p.Round(65,137,130,19,5,new Color(1,.52f,.29f));
                    p.Round(150,165,39,63,5,ink);p.Round(157,171,24,54,2,steel);p.Round(143,215,52,13,4,ink);p.Round(147,219,44,7,2,new Color(.8f,.86f,.9f));
                    p.Round(28,64,71,86,10,ink);p.Round(35,72,57,68,5,steel);p.Round(49,85,28,43,3,new Color(.29f,.39f,.55f));
                    p.Round(117,75,66,43,7,ink);p.Round(123,82,54,28,3,steel);p.Line(132,89,167,89,4,ink);p.Line(132,101,167,101,4,ink);p.Circle(208,74,15,ink);p.Circle(210,75,10,gold);break;
                case PartType.Cargo:
                    p.Poly(new[]{new Vector2(30,144),new Vector2(224,144),new Vector2(203,30),new Vector2(54,30)},ink);p.Poly(new[]{new Vector2(41,134),new Vector2(212,134),new Vector2(194,40),new Vector2(64,40)},steel);p.Line(69,48,188,48,6,new Color(.3f,.41f,.57f));
                    p.Diamond(75,159,28,42,new Color(.02f,.85f,1));p.Diamond(128,177,32,61,new Color(.04f,.75f,.96f));p.Diamond(176,153,28,45,new Color(.03f,.91f,1));p.Line(40,138,214,138,13,ink);p.Line(45,144,210,144,7,new Color(.82f,.91f,.96f));p.Circle(80,66,6,Color.white);p.Circle(177,66,6,Color.white);break;
                case PartType.Spring:
                    p.Line(86,30,166,225,18,ink);for(int i=0;i<6;i++){float y=51+i*29;p.Line(78,y,177,y+18,22,ink);p.Line(78,y+3,177,y+21,14,red);p.Line(85,y+7,170,y+23,4,new Color(1,.54f,.44f));}p.Line(57,27,153,27,19,ink);p.Line(63,31,147,31,10,steel);p.Line(106,228,203,228,19,ink);p.Line(113,232,197,232,10,steel);break;
                case PartType.Balloon:
                    p.Line(129,52,119,15,5,new Color(.77f,.49f,.22f));p.Circle(128,151,83,ink,1,1.1f);p.Circle(128,154,75,new Color(.96f,.13f,.24f),1,1.08f);p.Circle(107,183,24,new Color(1,.5f,.58f),1,.7f);p.Circle(96,190,10,new Color(1,.78f,.78f));p.Poly(new[]{new Vector2(119,72),new Vector2(140,72),new Vector2(147,53),new Vector2(111,53)},red);break;
            }
            icon=p.Sprite();Icons.Add(type,icon);return icon;
        }
        static Sprite MakePanel(bool empty)
        {
            var p=new Painter(128);p.Round(0,0,128,128,25,new Color(.02f,.08f,.15f));p.Round(3,6,122,121,22,new Color(.35f,.75f,.95f));p.Round(8,10,112,110,17,new Color(.62f,.87f,1));p.Round(11,12,106,103,15,new Color(.68f,.77f,.83f));p.Round(13,16,102,96,13,empty?new Color(.24f,.38f,.53f):new Color(.72f,.79f,.87f));
            if(!empty)p.Round(18,96,92,11,5,new Color(1,1,1,.44f));
            var t=p.Texture();return Sprite.Create(t,new Rect(0,0,128,128),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(27,27,27,27));
        }
        sealed class Painter
        {
            readonly int n;readonly Color[] pixels;
            public Painter(int size){n=size;pixels=new Color[n*n];}
            void Dot(int x,int y,Color c,float a=1,bool erase=false){if(x<0||y<0||x>=n||y>=n)return;int k=y*n+x;if(erase){pixels[k]=Color.clear;return;}a*=c.a;var d=pixels[k];float alpha=a+d.a*(1-a);pixels[k]=alpha<=0?Color.clear:new Color((c.r*a+d.r*d.a*(1-a))/alpha,(c.g*a+d.g*d.a*(1-a))/alpha,(c.b*a+d.b*d.a*(1-a))/alpha,alpha);}
            public void Round(float x,float y,float w,float h,float radius,Color c,bool erase=false){for(int py=(int)y;py<y+h;py++)for(int px=(int)x;px<x+w;px++){float dx=Mathf.Max(Mathf.Abs(px+.5f-x-w/2)-w/2+radius,0),dy=Mathf.Max(Mathf.Abs(py+.5f-y-h/2)-h/2+radius,0);float a=Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f);if(a>0)Dot(px,py,c,a,erase);}}
            public void Circle(float x,float y,float r,Color c,float sx=1,float sy=1){for(int py=(int)(y-r*sy-1);py<=y+r*sy+1;py++)for(int px=(int)(x-r*sx-1);px<=x+r*sx+1;px++){float d=Vector2.Distance(new Vector2((px+.5f-x)/sx,(py+.5f-y)/sy),Vector2.zero);Dot(px,py,c,Mathf.Clamp01(r-d+.5f));}}
            public void Line(float ax,float ay,float bx,float by,float width,Color c){var a=new Vector2(ax,ay);var b=new Vector2(bx,by);for(int y=(int)(Mathf.Min(ay,by)-width);y<=Mathf.Max(ay,by)+width;y++)for(int x=(int)(Mathf.Min(ax,bx)-width);x<=Mathf.Max(ax,bx)+width;x++){var q=new Vector2(x+.5f,y+.5f);float t=Mathf.Clamp01(Vector2.Dot(q-a,b-a)/(b-a).sqrMagnitude);Dot(x,y,c,Mathf.Clamp01(width/2-Vector2.Distance(q,a+(b-a)*t)+.5f));}}
            public void Poly(Vector2[] points,Color c){for(int y=0;y<n;y++)for(int x=0;x<n;x++){bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++){var a=points[i];var b=points[j];if((a.y>y)!=(b.y>y)&&x<(b.x-a.x)*(y-a.y)/(b.y-a.y)+a.x)inside=!inside;}if(inside)Dot(x,y,c);}}
            public void Diamond(float x,float y,float w,float h,Color c){var points=new[]{new Vector2(x,y+h),new Vector2(x+w,y),new Vector2(x,y-h*.6f),new Vector2(x-w,y)};Poly(points,new Color(.01f,.32f,.48f));Poly(new[]{new Vector2(x,y+h-6),new Vector2(x+w-5,y),new Vector2(x,y-h*.6f+4),new Vector2(x-w+5,y)},c);Poly(new[]{new Vector2(x,y+h-6),new Vector2(x,y-h*.6f+4),new Vector2(x-w+5,y)},new Color(.42f,.97f,1));Line(x,y+h-3,x+w-4,y,3,Color.white);}
            public Texture2D Texture(){var t=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};t.SetPixels(pixels);t.Apply();return t;}
            public Sprite Sprite()=>UnityEngine.Sprite.Create(Texture(),new Rect(0,0,n,n),Vector2.one*.5f,n);
        }
    }
}
