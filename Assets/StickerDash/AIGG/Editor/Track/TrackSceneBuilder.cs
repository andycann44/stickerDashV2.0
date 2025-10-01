#if UNITY_EDITOR
using UnityEditor; using UnityEditor.SceneManagement;
using UnityEngine; using System; using System.Linq; using System.Text.RegularExpressions; using System.Collections.Generic;

namespace Aim2Pro.AIGG.Track {
  public static class TrackSceneBuilder {
    const string RootName = "A2P_Track";
    static int lastRows=0,lastCols=0;

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
      float h=0.2f;
      for(int r=0;r<rows;r++){
        for(int c=0;c<cols;c++){
          var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
          cube.name = $"tile_r{r}_c{c}";
          cube.transform.SetParent(root,false);
          cube.transform.localScale = new Vector3(1f,h,1f);
          cube.transform.localPosition = new Vector3(c,h/2f,r);
          Undo.RegisterCreatedObjectUndo(cube,"Create Tile");
        }
      }
      lastRows=rows; lastCols=cols;
      MarkDirty(); return (rows,cols);
    }

    public static int DeleteRows(int start,int end){
      var root=Root(); int count=0;
      for(int i=root.childCount-1;i>=0;--i){
        var t=root.GetChild(i);
        if(TryRowCol(t.name,out int r,out _)){
          if(r>=start && r<=end){ Undo.DestroyObjectImmediate(t.gameObject); count++; }
        }
      }
      MarkDirty(); return count;
    }

    public static int DeleteTilesInRow(int row, int[] cols1Based){
      var root=Root(); int count=0;
      var set = new HashSet<int>(cols1Based.Select(v=>Mathf.Max(1,v)));
      for(int i=root.childCount-1;i>=0;--i){
        var t=root.GetChild(i);
        if(TryRowCol(t.name,out int r,out int c)){
          if(r==row && set.Contains(c+1)){ Undo.DestroyObjectImmediate(t.gameObject); count++; }
        }
      }
      MarkDirty(); return count;
    }

    public static int RandomHoles(float percent){
      percent = Mathf.Clamp(percent,0f,100f);
      float p = percent/100f; int removed=0;
      var root=Root();
      for(int i=root.childCount-1;i>=0;--i){
        var t=root.GetChild(i);
        if(UnityEngine.Random.value < p){ Undo.DestroyObjectImmediate(t.gameObject); removed++; }
      }
      MarkDirty(); return removed;
    }

    public static int InsertJumpGaps(int gaps){
      if(lastRows<=0) return 0;
      gaps = Mathf.Max(0,gaps);
      if(gaps==0) return 0;
      int removed=0;
      // even spacing across rows
      for(int i=1;i<=gaps;i++){
        int row = Mathf.Clamp(Mathf.RoundToInt(i*(lastRows/(float)(gaps+1))),0,lastRows-1);
        removed += DeleteRows(row,row);
      }
      MarkDirty(); return removed;
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
