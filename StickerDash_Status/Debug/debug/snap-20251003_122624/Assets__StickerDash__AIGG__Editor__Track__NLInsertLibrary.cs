#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;

namespace Aim2Pro.AIGG {
    public static class NLInsertLibrary {
        public static void ShowInsertMenu(Action<string> onInsert){
            var gm = new GenericMenu();

            void add(string label, string text){ gm.AddItem(new GUIContent(label), false, () => onInsert(text)); }

            // Basics
            add("Build/build 300 by 6", "build 300 by 6");
            add("Build/safe start 10 end 10", "safe start 10 end 10");

            // Randomizers
            add("Random/random holes 5%", "random holes 5%");
            add("Random/random slopes 2 to 4 degrees, segment 2", "random slopes 2 to 4 degrees, segment 2");
            add("Random/auto s-bends 2 at 25", "auto s-bends 2 at 25");

            // Curves/S-bends
            add("Curves/curve rows 80 to 120 left 20 degrees", "curve rows 80 to 120 left 20 degrees");
            add("Curves/curve rows 120 to 160 right 20 degrees", "curve rows 120 to 160 right 20 degrees");
            add("Curves/s bend 60 to 120 at 25 gain 2", "s bend 60 to 120 at 25 gain 2");

            // Chicanes (examples)
            add("Chicane/left → right (examples)",
                "curve rows 80 to 110 left 18 degrees\ncurve rows 110 to 140 right 18 degrees");
            add("Chicane/right → left (examples)",
                "curve rows 80 to 110 right 18 degrees\ncurve rows 110 to 140 left 18 degrees");

            // Forks / Rejoin
            add("Fork/fork same width 140 to 200 gap 1", "fork same width 140 to 200 gap 1");
            add("Fork/rejoin 200 to 240", "rejoin 200 to 240");

            // Edits
            add("Edit/delete row 15", "remove rows 15 to 15");
            add("Edit/delete tiles 2,4,7 in row 95", "remove tiles 2,4,7 in row 95");

            gm.ShowAsContext();
        }

        public static string AppendWithNewline(string dst, string snippet){
            if (string.IsNullOrEmpty(dst)) return snippet;
            if (!dst.EndsWith("\n")) dst += "\n";
            return dst + snippet;
        }
    }
}
#endif
