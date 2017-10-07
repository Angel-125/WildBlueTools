using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.IO;

namespace WildBlueIndustries
{
    public delegate void ChangePlayMode(string modeName);

    public class PlayModesWindow : Dialog<PlayModesWindow>
    {
        public static int selectedIndex = -1;
        public static ConfigNode[] playModeNodes;

        public ChangePlayMode changePlayModeDelegate;

        public string playModesPath;

        public string currentPlayMode;
        public string currentPlayModeFile;

        public bool payToRemodel;
        public bool requireSkillCheck;
        public bool repairsRequireResources;
        public bool partsCanBreak;

        protected string[] playModeFiles;
        protected string[] playModeNames;
        protected string description;
        protected ConfigNode nodePlayMode;
        internal WBIPlayModeHelper playModeHelper = new WBIPlayModeHelper();

        private Vector2 _scrollPos, _scrollPos2;

        public PlayModesWindow() :
        base("Select A Play Mode", 800, 600)
        {
            Resizable = false;
            _scrollPos = new Vector2(0, 0);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                //Get the play modes
                playModeNodes = playModeHelper.GetModes();

                //Get the button labels
                List<string> modeNames = new List<string>();
                foreach (ConfigNode node in playModeNodes)
                {
                    if (node.HasValue("name"))
                    {
                        modeNames.Add(node.GetValue("name"));
                    }
                }
                playModeNames = modeNames.ToArray();

                //Get the current play mode.
                selectedIndex = playModeHelper.GetCurrentModeIndex();
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("<color=white>Do you want to use a different set of resources? How about less complicated converters for colonization? With Play Modes, now you can! Play Modes give you the ability to significantly alter the game functionality of your installed Wild Blue Industries mods to suit your play styles. It's your game, your choice.</color>\r\n<color=yellow><b>You will need to restart Kerbal Space Program for these changes to take effect.</b></color>");

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, new GUILayoutOption[] { GUILayout.Width(375) });
            selectedIndex = GUILayout.SelectionGrid(selectedIndex, playModeNames, 1);
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            _scrollPos2 = GUILayout.BeginScrollView(_scrollPos2, new GUILayoutOption[] { GUILayout.Width(425) });

            nodePlayMode = playModeNodes[selectedIndex];
            loadConfig();

            GUILayout.Label(description);

            GUILayout.EndScrollView();

            drawOkCancelButtons();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        protected void loadConfig()
        {
            StringBuilder desc = new StringBuilder();

            //Name
            if (nodePlayMode.HasValue("name"))
                desc.AppendLine("<color=LightBlue><b>" + nodePlayMode.GetValue("name") + "</b></color>");

            //Description
            if (nodePlayMode.HasValue("description"))
                desc.AppendLine("\r\n<color=white>" + nodePlayMode.GetValue("description") + "</color>");

            //Settings
            if (nodePlayMode.HasValue("payToRemodel"))
                payToRemodel = bool.Parse(nodePlayMode.GetValue("payToRemodel"));
            else
                payToRemodel = true;

            if (nodePlayMode.HasValue("requireSkillCheck"))
                requireSkillCheck = bool.Parse(nodePlayMode.GetValue("requireSkillCheck"));
            else
                requireSkillCheck = true;

            if (nodePlayMode.HasValue("repairsRequireResources"))
                repairsRequireResources = bool.Parse(nodePlayMode.GetValue("repairsRequireResources"));
            else
                repairsRequireResources = true;

            if (nodePlayMode.HasValue("partsCanBreak"))
                partsCanBreak = bool.Parse(nodePlayMode.GetValue("partsCanBreak"));
            else
                partsCanBreak = true;

            desc.AppendLine(" ");
            desc.AppendLine("<color=white><b>Parts can break: </b>" + partsCanBreak + "</color>");
            desc.AppendLine("<color=white><b>Repairs require resources: </b>" + repairsRequireResources + "</color>");
            desc.AppendLine("<color=white><b>Pay to reconfigure/assemble modules: </b>" + payToRemodel + "</color>");
            desc.AppendLine("<color=white><b>Reconfiguration/assembly requires skill: </b>" + requireSkillCheck + "</color>");

            description = desc.ToString();
        }

        protected void drawOkCancelButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                //Set the play mode
                playModeHelper.SetPlayMode(playModeNames[selectedIndex]);

                //Fire the delegate
                if (changePlayModeDelegate != null)
                    changePlayModeDelegate(playModeNames[selectedIndex]);

                SetVisible(false);
            }

            if (GUILayout.Button("Cancel"))
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();
        }


    }
}
