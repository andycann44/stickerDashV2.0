#if UNITY_EDITOR
using UnityEditor; using UnityEditor.SceneManagement;
using UnityEngine; using System.Linq; using System;
namespace Aim2Pro.AIGG.Track {
  public static class TrackSceneBuilder {
    const string RootName = "A2P_Track";
    static Transform Root() {
      var go = GameObject.Find(RootName);
      if (!go) { go = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(go, "Create Track Root"); }
      return go.transform;
    }
    public static (int rows,int cols) BuildAbs(int lengthM, int width) {
      var root = Root();
      Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Rebuild Track");
      for (int i=root.childCount-1;i>=0;--i) Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
      int rows = Mathf.Max(1, lengthM); int cols = Mathf.Max(1, width);
      float h = 0.2f;
      for (int r=0; r<rows; r++){
        for (int c=0; c<cols; c++){
          var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
          cube.name = $"tile_r{r}_c{c}";
          cube.transform.SetParent(root, false);
          cube.transform.localScale = new Vector3(1f, h, 1f);
          cube.transform.localPosition = new Vector3(c, h/2f, r);
          Undo.RegisterCreatedObjectUndo(cube, "Create Tile");
        }
      }
      EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
      return (rows, cols);
    }
    public static int DeleteRows(int start, int end){
      var root = Root(); int count=0;
      for (int i=root.childCount-1;i>=0;--i){
        var t=root.GetChild(i); if (TryRowCol(t.name, out int r, out _)){
          if (r>=start && r<=end){ Undo.DestroyObjectImmediate(t.gameObject); count++; }
        }
      } return count;
    }
    public static int DeleteTilesInRow(int row, int[] cols1Based){
      var root=Root(); int count=0;
      var cols = cols1Based.Select(v=>Mathf.Max(0,v-1)).ToArray();
      for (int i=root.childCount-1;i>=0;--i){
        var t=root.GetChild(i); if (TryRowCol(t.name, out int r, out int c)){
          if (r==row && cols.Contains(c)){ Undo.DestroyObjectImmediate(t.gameObject); count++; }
        }
      } return count;
    }
    static bool TryRowCol(string name, out int r, out int c){
      r=c=-1; if (string.IsNullOrEmpty(name)) return false;
      // expects tile_r{r}_c{c}
      var parts=name.Split(new[]{\'_\',\'r\',\'c\'}, StringSplitOptions.RemoveEmptyEntries);
      // crude but safe: find last two integers
      var ints = parts.Where(p=>int.TryParse(p,out _)).Select(int.Parse).ToArray();
      if (ints.Length<2) return false; r=ints[ints.Length-2]; c=ints[ints.Length-1]; return true;
    }
  }
}
#endif
