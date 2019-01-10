using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.IO;
using UnityEngine;

namespace WildBlueIndustries
{
    public delegate bool PerformAnalysisDelegate();
    public delegate void DrawViewDelegate();

    public class GeoLabView : Dialog<GeoLabView>
    {
        public Part part;
        public ModuleGPS gps;
        public Dictionary<string, float> abundanceSummary = new Dictionary<string, float>();
        public PerformAnalysisDelegate performBiomAnalysisDelegate;
        public DrawViewDelegate drawView;

        Vector2 scrollPosResources = new Vector2(0, 0);

        public GeoLabView() :
        base("<color=white>Geology Lab</color>", 300, 330)
        {
            Resizable = false;
        }

        public void DrawView()
        {
            if (this.part != null && this.gps == null)
                this.gps = this.part.FindModuleImplementing<ModuleGPS>();

            if (HighLogic.LoadedSceneIsFlight == false)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("<color=yellow>This view is unavailable in the VAB/SPH.</color>");
                GUILayout.EndVertical();
            }
            bool biomeUnlocked = Utils.IsBiomeUnlocked(this.part.vessel);
            GUILayout.BeginVertical();

            //Location
            GUILayout.BeginScrollView(new Vector2(0, 0), new GUIStyle(GUI.skin.textArea), GUILayout.Height(110));
            if (gps != null)
            {
                GUILayout.Label("<color=white><b>Location:</b> " + gps.body + " " + gps.bioName + "</color>");
                GUILayout.Label("<color=white><b>Lon:</b> " + gps.lon + "</color>");
                GUILayout.Label("<color=white><b>Lat:</b> " + gps.lat + "</color>");
            }
            else
            {
                GUILayout.Label("<color=yellow>Unable to determine current location.</color>");
            }
            GUILayout.EndScrollView();

            //Abundance
            drawAbundanceGUI(biomeUnlocked);

            //Extra stuff
            if (drawView != null)
                drawView();

            GUILayout.EndVertical();
        }

        protected override void DrawWindowContents(int windowId)
        {
            DrawView();
        }

        protected void drawAbundanceGUI(bool biomeUnlocked)
        {
            GUILayout.BeginVertical();
            if (biomeUnlocked && abundanceSummary != null)
            {
                int count = abundanceSummary.Keys.Count;
                if (count > 0)
                {
                    string[] keys = abundanceSummary.Keys.ToArray();
                    scrollPosResources = GUILayout.BeginScrollView(scrollPosResources, new GUIStyle(GUI.skin.textArea));
                    for (int index = 0; index < count; index++)
                    {
                        GUILayout.Label("<color=white>" + keys[index] + " abundance: " + getAbundance(abundanceSummary[keys[index]]) + "</color>");
                    }
                }
                else
                {
                    scrollPosResources = GUILayout.BeginScrollView(scrollPosResources, new GUIStyle(GUI.skin.textArea));
                    GUILayout.Label("<color=yellow>No detectable resources in this area.</color>");
                }
            }

            else
            {
                scrollPosResources = GUILayout.BeginScrollView(scrollPosResources, new GUIStyle(GUI.skin.textArea));
                GUILayout.Label("<color=yellow>Unlock the biome to get the resource composition.</color>");
                if (GUILayout.Button("Perform biome analysis") && performBiomAnalysisDelegate != null)
                    performBiomAnalysisDelegate();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        protected string getAbundance(float abundance)
        {
            float displayAbundance = abundance * 100.0f;

            if (displayAbundance > 0.001)
                return string.Format("{0:f2}%", displayAbundance);
            else
                return "None present.";
        }
    }

}
