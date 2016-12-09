using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public struct SDrawbleView
    {
        public string buttonLabel;
        public IOpsView view;
        public Vessel vessel;
        public string partTitle;
    }

    public class OpsManagerView : Window<OpsManagerView>, IOpsView, IParentView
    {
        public bool hasDecals;
        public Part part;
        public ConvertibleStorageView storageView;
        public List<ModuleResourceConverter> converters = new List<ModuleResourceConverter>();

        private Vector2 _scrollPosViews, _scrollPosResources, _scrollPosConverters;
        List<SDrawbleView> views = new List<SDrawbleView>();
        SDrawbleView currentDrawableView;
        ModuleCommand commandModule;
        WBIResourceSwitcher switcher;
        WBILight lightModule = null;

        public OpsManagerView() :
        base("Manage Operations", 800, 480)
        {
            Resizable = false;
        }        

        #region IParentView
        public void ConfigChanged(IOpsView opsView)
        {
        }

        public void SetParentVisible(bool isVisible)
        {
            SetVisible(isVisible);
        }
        #endregion

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                UpdateButtonTabs();

                //Set initial view
                currentDrawableView = views[0];
            }
        }

        public void UpdateConverters()
        {
            converters.Clear();
            List<ModuleResourceConverter> possibleConverters = this.part.FindModulesImplementing<ModuleResourceConverter>();
            int totalCount = possibleConverters.Count;
            ModuleResourceConverter converter;

            //Now get rid of anything that is a basic science lab
            for (int index = 0; index < totalCount; index++)
            {
                converter = possibleConverters[index];

                if (!(converter is WBIBasicScienceLab))
                    converters.Add(converter);
            }
        }

        public void UpdateButtonTabs()
        {
            SDrawbleView drawableView;

            //Get our part modules
            UpdateConverters();
            GetPartModules();

            views.Clear();

            //Custom views from other PartModules
            List<IOpsView> templateOpsViews = this.part.FindModulesImplementing<IOpsView>();
            int totalCount = templateOpsViews.Count;
            int labelCount;
            string label;
            IOpsView templateOps;
            for (int index = 0; index < totalCount; index++)
            {
                templateOps = templateOpsViews[index];
                List<string> labels = templateOps.GetButtonLabels();
                labelCount = labels.Count;
                for (int labelIndex = 0; labelIndex < labelCount; labelIndex++)
                {
                    label = labels[labelIndex];
                    drawableView = new SDrawbleView();
                    drawableView.buttonLabel = label;
                    drawableView.view = templateOps;
                    templateOps.SetParentView(this);
                    templateOps.SetContextGUIVisible(false);
                    views.Add(drawableView);
                }
            }
        }

        #region IOpsView
        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public void DrawOpsWindow(string buttonLabel)
        {
            switch (buttonLabel)
            {
                case "Config":
                    storageView.DrawView();
                    break;

                case "Command":
                    drawCommandView();
                    break;

                case "Resources":
                    drawResourceView();
                    break;

                case "Converters":
                    drawConvertersView();
                    break;

                default:
                    if (currentDrawableView.vessel != null)
                        currentDrawableView.view.DrawOpsWindow(buttonLabel);
                    else
                        Debug.Log("No current view to show!");
                    break;
            }
        }

        public List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();
            int resourceCount = this.part.Resources.Count;

            //Get our part modules
            UpdateConverters();
            GetPartModules();

            buttonLabels.Add("Config");

            if (commandModule != null || lightModule != null || switcher.decalsVisible)
                buttonLabels.Add("Command");

            if (resourceCount > 0)
                buttonLabels.Add("Resources");

            if (converters.Count > 0)
                buttonLabels.Add("Converters");

            return buttonLabels;
        }

        public void SetContextGUIVisible(bool isVisible)
        {
        }

        #endregion

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();

            //View buttons
            _scrollPosViews = GUILayout.BeginScrollView(_scrollPosViews, new GUILayoutOption[] { GUILayout.Width(160) });
            int totalViews = views.Count;
            SDrawbleView drawableView;
            for (int index = 0; index < totalViews; index++)
            {
                drawableView = views[index];

                if (GUILayout.Button(drawableView.buttonLabel))
                {
                    _scrollPosViews = new Vector2();
                    currentDrawableView = drawableView;
                }
            }
            GUILayout.EndScrollView();

            //CurrentView
            currentDrawableView.view.DrawOpsWindow(currentDrawableView.buttonLabel);

            GUILayout.EndHorizontal();
        }

        public void GetPartModules()
        {
            BaseEvent cmdEvent;
            int totalEvents;

            commandModule = this.part.FindModuleImplementing<ModuleCommand>();
            if (commandModule != null)
            {
                totalEvents = commandModule.Events.Count;
                for (int index = 0; index < totalEvents; index++)
                {
                    cmdEvent = commandModule.Events.GetByIndex(index);
                    cmdEvent.guiActive = false;
                    cmdEvent.guiActiveUnfocused = false;
                }
            }

            switcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();
            if (switcher != null)
            {
                switcher.Events["ToggleDecals"].guiActive = false;
                switcher.Events["ToggleDecals"].guiActiveUnfocused = false;
                switcher.Events["DumpResources"].guiActive = false;
                switcher.Events["DumpResources"].guiActiveUnfocused = false;
            }

            lightModule = this.part.FindModuleImplementing<WBILight>();
        }

        protected void drawCommandView()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(new Vector2(), new GUIStyle(GUI.skin.textArea), new GUILayoutOption[] { GUILayout.Height(480) });
            if (!HighLogic.LoadedSceneIsFlight)
            {
                GUILayout.Label("This configuration is working, but the contents can only be accessed in flight.");
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            //Control From Here
            if (commandModule != null)
            {
                if (GUILayout.Button("Control From Here"))
                    commandModule.MakeReference();

                //Rename Vessel
                if (GUILayout.Button("Rename Base"))
                    commandModule.RenameVessel();
            }

            //Toggle Decals
            if (switcher != null)
            {
                if (GUILayout.Button("Toggle Decals"))
                    switcher.ToggleDecals();
            }

            //Dump Resources
            if (switcher != null)
            {
                if (GUILayout.Button("Dump Resources"))
                    switcher.DumpResources();
            }

            //Toggle Lights
            if (lightModule != null)
            {
                if (lightModule.isDeployed)
                {
                    if (GUILayout.Button(lightModule.endEventGUIName))
                        lightModule.TurnOffLights();
                }

                else
                {
                    if (GUILayout.Button(lightModule.startEventGUIName))
                        lightModule.TurnOnLights();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        protected void drawResourceView()
        {
            PartResource resource;
            int TotalCount;

            GUILayout.BeginVertical();
            _scrollPosResources = GUILayout.BeginScrollView(_scrollPosResources, new GUIStyle(GUI.skin.textArea), new GUILayoutOption[] { GUILayout.Height(480) });

            if (this.part.Resources.Count == 0)
            {
                GUILayout.Label("<color=yellow>This configuration does not have any resources in it.</color>");
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            TotalCount = this.part.Resources.Count;
            for (int index = 0; index < TotalCount; index++)
            {
                resource = this.part.Resources[index];
                if (resource.isVisible)
                {
                    GUILayout.Label(resource.resourceName);
                    GUILayout.Label(String.Format("<color=white>{0:#,##0.00}/{1:#,##0.00}</color>", resource.amount, resource.maxAmount));
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        protected void drawConvertersView()
        {
            GUILayout.BeginVertical();
            string converterName = "??";
            string converterStatus = "??";
            bool isActivated;
            ModuleResourceConverter converter;
            int totalCount;

            _scrollPosConverters = GUILayout.BeginScrollView(_scrollPosConverters, new GUIStyle(GUI.skin.textArea), new GUILayoutOption[] { GUILayout.Height(480) });
            if (converters.Count == 0)
            {
                GUILayout.Label("<color=yellow>This configuration does not have any resource converters in it.</color>");
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            totalCount = converters.Count;
            for (int index = 0; index < totalCount; index++)
            {
                converter = converters[index];
                converterName = converter.ConverterName;
                converterStatus = converter.status;
                isActivated = converter.IsActivated;

                GUILayout.BeginVertical();

                GUILayout.Label("EfficiencyModifiers");
                GUILayout.Label("Count: " + converter.EfficiencyModifiers.Count);
                foreach (string key in converter.EfficiencyModifiers.Keys)
                {
                    GUILayout.Label(key + ": " + converter.EfficiencyModifiers[key]);
                }

                //Toggle, name and status message
                if (!HighLogic.LoadedSceneIsEditor)
                    isActivated = GUILayout.Toggle(isActivated, string.Format(converterName + " ({0:f1}%): ", converter.EfficiencyBonus * 100f) + converterStatus);
                else
                    isActivated = GUILayout.Toggle(isActivated, converterName);

                if (converter.IsActivated != isActivated)
                {
                    if (isActivated)
                        converter.StartResourceConverter();
                    else
                        converter.StopResourceConverter();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

    }
}
