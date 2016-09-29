using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace WildBlueIndustries
{
    public class InfoView : Window<InfoView>
    {
        public string ModuleInfo;
        public Texture moduleLabel;

        private Vector2 _scrollPos;
        private string _info;

        public InfoView() :
        base("Module Info", 320, 400)
        {
            Resizable = false;
            _scrollPos = new Vector2(0, 0);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            _info = ModuleInfo.Replace("<br>", "\r\n");
        }

        protected override void DrawWindowContents(int windowId)
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            if (moduleLabel != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(moduleLabel, new GUILayoutOption[] { GUILayout.Width(128), GUILayout.Height(128) });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(_info);
            GUILayout.EndScrollView();
        }

    }
}
