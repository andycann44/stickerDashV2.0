#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public class NLCheatSheetWindow : EditorWindow {
        const string SheetPath = "StickerDash_Status/NL_CheatSheet.md";
        Vector2 _scroll;
        string _text="";

        [MenuItem("Window/Aim2Pro/Track Creator/NL/Open Cheat Sheet")]
        public static void OpenMenu(){ Open(); }

        public static void Open(){
            Directory.CreateDirectory("StickerDash_Status");
            if (!File.Exists(SheetPath)) File.WriteAllText(SheetPath, DefaultSheet());
            var w = GetWindow<NLCheatSheetWindow>("NL Cheat Sheet");
            w.minSize = new Vector2(540, 420);
            w.Load();
        }

        void Load(){
            try{ _text = File.ReadAllText(SheetPath); }
            catch{ _text = "(Could not read NL_CheatSheet.md)"; }
        }

        void OnGUI(){
            using (new EditorGUILayout.HorizontalScope()){
                GUILayout.Label("Cheat Sheet (read-only)", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.Height(22))) Load();
                if (GUILayout.Button("Open Externally", GUILayout.Height(22))) {
                    EditorUtility.OpenWithDefaultApp(Path.GetFullPath(SheetPath));
                }
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUI.DisabledScope(true)){
                EditorGUILayout.TextArea(_text ?? "", GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();
        }

        static string DefaultSheet(){
            return string.Join(System.Environment.NewLine, new[]{
                "# NL Cheat Sheet",
                "",
                "Rebuild:",
                "  build 300 by 6",
                "  safe start 10 end 10",
                "",
                "Amend:",
                "  remove rows 80 to 120",
                "  remove tiles 2,4,7 in row 95",
                "  curve rows 40 to 60 left 20 degrees",
                "  s bend 60 to 120 at 25 gain 2",
                "  random slopes 2 to 4 degrees, segment 2",
                "  random holes 5%",
                "  add 2 jump gaps",
                "  protect start 10 end 10",
                "",
                "Fork / Rejoin:",
                "  fork same width 140 to 200 gap 1",
                "  rejoin 200 to 240"
            });
        }
    }
}
#endif
