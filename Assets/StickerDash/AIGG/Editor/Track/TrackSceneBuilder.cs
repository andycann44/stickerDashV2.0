#if UNITY_EDITOR
using UnityEditor; using UnityEditor.SceneManagement;
using UnityEngine; using System; using System.Linq; using System.Text.RegularExpressions; using System.Collections.Generic;

namespace Aim2Pro.AIGG.Track {
  public static class TrackSceneBuilder {
    const string RootName = "A2P_Track";
    static int lastRows=0,lastCols=0;
    static float tileH = 0.2f;

    static Transform Root(){
      var go = GameObject.Find(RootName);
      if(!go){ go = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(go,"Create Track Root"); }
      return go.transform;
    }
    static void MarkDirty(){ EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); }
    public static void Seed(int s){ UnityEngine.Random.InitState(s); }

    public static (int rows,int cols) BuildAbs(int lengthM, int width){
      var root = Root();
      Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Rebuild Track");
      for(int i=root.childCount-1;i>=0;--i) Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
      int rows = Mathf.Max(1,lengthM), cols = Mathf.Max(1,width);
      for(int r=0;r<rows;r++){
        for(int c=0;c<cols;c++){
          var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
          cube.name = $"tile_r{r}_c{c}";
          cube.transform.SetParent(root,false);
          cube.transform.localScale = new Vector3(1f,tileH,1f);
          cube.transform.localPosition = new Vector3(c,tileH/2f,r);
          Undo.RegisterCreatedObjectUndo(cube,"Create Tile");
        }
      }
      lastRows=rows; lastCols=cols; MarkDirty(); return (rows,cols);
    }

    public static int DeleteRows(int start,int end){
      var root=Root(); int count=0; NormalizeRange(ref start, ref end, lastRows);
      for(int i=root.childCount-1;i>=0;--i){
        var t=root.GetChild(i);
        if(TryRowCol(t.name,out int r,out _)){
          if(r>=start && r<=end){ Undo.DestroyObjectImmediate(t.gameObject); count++; }
        }
      }
      MarkDirty(); return count;
    }

    public static int DeleteTilesInRow(int row, int[] cols1Based){
      var root=Root(); int count=0; var set = new HashSet<int>(cols1Based.Select(v=>Mathf.Max(1,v)));
      for(int i=root.childCount-1;i>=0;--i){
        var t=root.GetChild(i);
        if(TryRowCol(t.name,out int r,out int c)){
          if(r==row && set.Contains(c+1)){ Undo.DestroyObjectImmediate(t.gameObject); count++; }
        }
      }
      MarkDirty(); return count;
    }

    public static int RandomHoles(float percent){
      percent = Mathf.Clamp(percent,0f,100f); float p = percent/100f; int removed=0;
      var root=Root();
      for(int i=root.childCount-1;i>=0;--i){ var t=root.GetChild(i); if(UnityEngine.Random.value < p){ Undo.DestroyObjectImmediate(t.gameObject); removed++; } }
      MarkDirty(); return removed;
    }

    public static int InsertJumpGaps(int gaps){
      if(lastRows<=0) return 0; gaps = Mathf.Max(0,gaps); if(gaps==0) return 0;
      int removed=0;
      for(int i=1;i<=gaps;i++){
        int row = Mathf.Clamp(Mathf.RoundToInt(i*(lastRows/(float)(gaps+1))),0,lastRows-1);
        removed += DeleteRows(row,row);
      }
      MarkDirty(); return removed;
    }

    // ---- New: Curves & S-bends & Slopes ----
    public static int CurveRows(int start, int end, string dir, float degrees){
      if(lastRows<=0) return 0; NormalizeRange(ref start, ref end, lastRows);
      float sign = (dir.ToLowerInvariant().StartsWith("r")) ? 1f : -1f;
      float t2m = Mathf.Tan(degrees * Mathf.Deg2Rad);           // convert deg to lateral factor
      float maxOffset = Mathf.Clamp(t2m * 5f, -20f, 20f);       // soft cap offsets
      int changed=0;
      var root=Root();
      for(int r=start; r<=end; r++){
        float t = (end==start)?1f:((r-start)/(float)(end-start));
        float off = sign * maxOffset * Mathf.SmoothStep(0f,1f,t);
        for(int i=0;i<root.childCount;i++){
          var tr = root.GetChild(i);
          if(TryRowCol(tr.name, out int rr, out _ ) && rr==r){
            var p = tr.localPosition; p.x = Mathf.Round((p.x + off)*100f)/100f; tr.localPosition = p; changed++;
          }
        }
      }
      MarkDirty(); return changed;
    }

    public static int SBend(int start, int end, float degrees){
      if(lastRows<=0) return 0; NormalizeRange(ref start, ref end, lastRows);
      float amp = Mathf.Clamp(Mathf.Tan(degrees*Mathf.Deg2Rad) * 5f, -20f, 20f);
      int changed=0; var root=Root();
      for(int r=start; r<=end; r++){
        float t = (end==start)?0f:((r-start)/(float)(end-start));
        float off = amp * Mathf.Sin(2f*Mathf.PI*t);
        for(int i=0;i<root.childCount;i++){
          var tr=root.GetChild(i); if(TryRowCol(tr.name,out int rr,out _ ) && rr==r){
            var p=tr.localPosition; p.x = Mathf.Round((p.x + off)*100f)/100f; tr.localPosition=p; changed++;
          }
        }
      }
      MarkDirty(); return changed;
    }

    public static int SBendAuto(int count, float degrees){
      if(lastRows<=0 || count<=0) return 0;
      int per = Mathf.Max(3, lastRows / count);
      int changed=0;
      for(int k=0; k<count; k++){
        int s = Mathf.Clamp(k*per, 0, lastRows-1);
        int e = Mathf.Clamp((k+1)*per-1, 0, lastRows-1);
        changed += SBend(s,e,degrees);
      }
      return changed;
    }

    public static int SlopesRandomAuto(float minDeg, float maxDeg, int segmentLen){
      if(lastRows<=0) return 0;
      segmentLen = Mathf.Max(3, segmentLen);
      float height = 0f; int changed=0; var root=Root();
      int r=0;
      while(r<lastRows){
        float deg = UnityEngine.Random.Range(minDeg, maxDeg);
        float dhPerRow = Mathf.Tan(deg*Mathf.Deg2Rad) * 0.5f; // gentle slope per row
        int segEnd = Mathf.Min(lastRows-1, r+segmentLen-1);
        for(; r<=segEnd; r++){
          for(int i=0;i<root.childCount;i++){
            var tr=root.GetChild(i);
            if(TryRowCol(tr.name,out int rr,out _ ) && rr==r){
              var p=tr.localPosition; p.y = tileH/2f + height; tr.localPosition=p; changed++;
            }
          }
          height += dhPerRow;
        }
      }
      MarkDirty(); return changed;
    }

    static void NormalizeRange(ref int a, ref int b, int max){
      a=Mathf.Clamp(a,0,Mathf.Max(0,max-1)); b=Mathf.Clamp(b,0,Mathf.Max(0,max-1)); if(a>b){ int t=a;a=b;b=t; }
    }

    static bool TryRowCol(string name, out int r, out int c){
      r=c=-1; if(string.IsNullOrEmpty(name)) return false;
      var m = Regex.Match(name, @"tile_r(\d+)_c(\d+)");
      if(!m.Success) return false;
      r=int.Parse(m.Groups[1].Value); c=int.Parse(m.Groups[2].Value); return true;
    }
  }
}
#endif
