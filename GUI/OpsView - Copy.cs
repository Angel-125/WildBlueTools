using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: CC BY-NC-SA 4.0
License URL: https://creativecommons.org/licenses/by-nc-sa/4.0/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public delegate bool TemplateHasOpsWindow();
    public delegate void DrawTemplateOps();
    public delegate void NextModule();
    public delegate void PrevModule();
    public delegate void NextPreviewModule(string templateName);
    public delegate void PrevPreviewModule(string templateName);
    public delegate void ChangeModuleType(string templateName);
    public delegate string GetModuleInfo(string templateName);
    public delegate Texture GetModuleLogo(string templateName);

    public class OpsView : Window<OpsView>
    {
        public List<ModuleResourceConverter> converters = null;
        public Part part;
        public PartResourceList resources;
        public bool techResearched;
        public bool fieldReconfigurable;
        public string nextName;
        public string prevName;
        public string previewName;
        public string cost;
        public string requiredResource;
        public NextModule nextModuleDelegate = null;
        public PrevModule prevModuleDelegate = null;
        public NextPreviewModule nextPreviewDelegate = null;
        public PrevPreviewModule prevPreviewDelegate = null;
        public ChangeModuleType changeModuleTypeDelegate = null;
        public GetModuleInfo getModuleInfoDelegate = null;
        public GetModuleLogo getModuleLogoDelegate = null;
        public TemplateHasOpsWindow teplateHasOpsWindowDelegate = null;
        public DrawTemplateOps drawTemplateOpsDelegate = null;
        public int templateCount = 0;
        public InfoView modSummary = new InfoView();

        private Vector2 _scrollPosConverters;
        private Vector2 _scrollPosResources;
        private string moduleInfo;
        ModuleCommand commandModule;
        WBIResourceSwitcher switcher;
        WBILight lightModule;
        protected bool drawTemplateOps;
        string[] tabs = new string[] { "Info", "Resources" };
        int selectedTab = 0;
        private string[] managementTabs = new string[] { "Processors", "Command & Control" };
        private int managementTab = 0;
        private string _shortName;
        public Texture moduleLabel;

        public OpsView() :
        base("<color=white>Operations Manager</color>", 600, 330)
        {
            Resizable = false;
            _scrollPosConverters = new Vector2(0, 0);
            _scrollPosResources = new Vector2(0, 0);
        }

        public string shortName
        {
            get
            {
                return _shortName;
            }

            set
            {
                _shortName = value;
                moduleInfo = getModuleInfoDelegate(_shortName).Replace("<br>", "\r\n");
                moduleLabel = getModuleLogoDelegate(shortName);
            }
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            if (newValue)
                UpdateConverters();
        }

        public void UpdateConverters()
        {
            List<ModuleResourceConverter> doomedConverters = new List<ModuleResourceConverter>();

            converters = this.part.FindModulesImplementing<ModuleResourceConverter>();

            //Now get rid of anything that is a basic science lab
            foreach (ModuleResourceConverter converter in converters)
            {
                if (converter is WBIBasicScienceLab)
                    doomedConverters.Add(converter);
            }

            foreach (ModuleResourceConverter doomed in doomedConverters)
                converters.Remove(doomed);
        }

        public override void DrawWindow()
        {
            base.DrawWindow();
            if (modSummary.IsVisible())
                modSummary.DrawWindow();
        }

        public void GetPartModules()
        {
            commandModule = this.part.FindModuleImplementing<ModuleCommand>();
            if (commandModule != null)
                foreach (BaseEvent cmdEvent in commandModule.Events)
                {
                    cmdEvent.guiActive = false;
                    cmdEvent.guiActiveUnfocused = false;
                }

            switcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();
            if (switcher != null)
            {
                switcher.Events["ToggleDecals"].guiActive = false;
                switcher.Events["ToggleDecals"].guiActiveUnfocused = false;
            }

            lightModule = this.part.FindModuleImplementing<WBILight>();
            if (lightModule != null)
                lightModule.showGui(false);

        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            GUILayout.Label("<color=white>Current: " + shortName +"</color>");

            if (teplateHasOpsWindowDelegate != null)
            {
                bool hasOpsWindow = teplateHasOpsWindowDelegate();
                if (hasOpsWindow && drawTemplateOpsDelegate != null)
                {
                    string buttonTitle = drawTemplateOps == true ? "Hide" : "Show";

                    if (GUILayout.Button(buttonTitle, GUILayout.Width(50)))
                        drawTemplateOps = !drawTemplateOps;

                    if (drawTemplateOps)
                    {
                        GUILayout.EndHorizontal();
                        drawTemplateOpsDelegate();
                        GUILayout.EndVertical();
                        return;
                    }
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            //Left pane : Module management controls
            drawModuleManagementPane();

            //Right pane: info/resource pane
            GUILayout.BeginVertical();
            if (!HighLogic.LoadedSceneIsEditor)
            {
                selectedTab = GUILayout.SelectionGrid(selectedTab, tabs, tabs.Length);
                if (selectedTab == 0)
                    drawInfoPane();
                else
                    drawResourcePane();
            }

            else
            {
                drawInfoPane();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        protected void drawInfoPane()
        {
            _scrollPosResources = GUILayout.BeginScrollView(_scrollPosResources, new GUIStyle(GUI.skin.textArea));
            if (moduleLabel != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(moduleLabel, new GUILayoutOption[] { GUILayout.Width(128), GUILayout.Height(128) });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Label(moduleInfo, new GUILayoutOption[] { GUILayout.Width(190)});
            GUILayout.EndScrollView();
        }

        protected void drawResourcePane()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Resources");

            _scrollPosResources = GUILayout.BeginScrollView(_scrollPosResources, new GUIStyle(GUI.skin.textArea));
            foreach (PartResource resource in this.part.Resources)
            {
                GUILayout.Label(resource.resourceName);
                GUILayout.Label(String.Format("<color=white>{0:#,##0.00}/{1:#,##0.00}</color>", resource.amount, resource.maxAmount));
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        protected virtual void drawModuleManagementPane()
        {
            GUILayout.BeginVertical(GUILayout.MaxWidth(350f));
            GUILayout.Space(4);
            bool cncControlsRendered = false;

            managementTab = GUILayout.SelectionGrid(managementTab, managementTabs, tabs.Length);
            if (managementTab == 0)
            {
                //Draw converters
                drawConverters();

                //Depending upon loaded scene, we'll either show the module next/prev buttons and labels
                //or we'll show the module preview buttons.
                if (!HighLogic.LoadedSceneIsEditor)
                    drawPreviewGUI();
                else
                    drawEditorGUI();
            }

            else //C&C tab
            {
                //Control From Here
                if (commandModule != null)
                {
                    cncControlsRendered = true;

                    if (GUILayout.Button("Control From Here"))
                        commandModule.MakeReference();

                    //Rename Vessel
                    if (GUILayout.Button("Rename Base"))
                        commandModule.RenameVessel();
                }

                //Toggle Decals
                if (switcher != null && switcher.decalsVisible)
                {
                    cncControlsRendered = true;

                    if (GUILayout.Button("Toggle Decals"))
                        switcher.ToggleDecals();
                }

                //Toggle Lights
                if (lightModule != null)
                {
                    cncControlsRendered = true;

                    if (GUILayout.Button("Toggle Lights"))
                        lightModule.ToggleAnimation();
                }

                //No controls?
                if (!cncControlsRendered)
                    GUILayout.Label("No C&C controls available");
            }

            GUILayout.EndVertical();
        }

        protected void drawEditorGUI()
        {
            if (templateCount == 1)
            {
                GUILayout.Label("<color=yellow>There is only one template, no other options.</color>");
                return;
            }

            //Next/Prev buttons
            if (GUILayout.Button("Next: " + nextName))
                if (nextModuleDelegate != null)
                    nextModuleDelegate();

            if (templateCount >= 4)
            {
                if (GUILayout.Button("Prev: " + prevName))
                    if (prevModuleDelegate != null)
                        prevModuleDelegate();
            }

            if (!fieldReconfigurable)
                GUILayout.Label("<color=yellow>NOTE: Cannot be reconfigured after launch.</color>");
        }

        protected void drawPreviewGUI()
        {
            //Only allow reconfiguring of the module if it allows field reconfiguration.
            if (fieldReconfigurable == false)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=yellow>This module cannot be reconfigured in the field.</color>");
                GUILayout.FlexibleSpace();
                return;
            }

            //Only allow reconfiguring of the module if enough tech has been researched.
            else if (techResearched == false)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=yellow>This module cannot be reconfigured. Research more technology.</color>");
                GUILayout.FlexibleSpace();
                return;
            }

            string moduleInfo;

            GUILayout.Label("<color=white>Current Preview: " + previewName + "</color>");
            GUILayout.Label("<color=white>Reconfiguration Cost: " + cost + " " + requiredResource + "</color>");

            //Make sure we have something to display
            if (string.IsNullOrEmpty(previewName))
                previewName = nextName;

            if (converters.Count > 2)
            {
                //Next preview button
                if (GUILayout.Button("Next: " + nextName))
                {
                    if (nextPreviewDelegate != null)
                        nextPreviewDelegate(previewName);
                }

                //Prev preview button
                if (GUILayout.Button("Prev: " + prevName))
                {
                    if (prevPreviewDelegate != null)
                        prevPreviewDelegate(previewName);
                }
            }

            else
            {
                //Next preview button
                if (GUILayout.Button("Next: " + nextName))
                {
                    if (nextPreviewDelegate != null)
                        nextPreviewDelegate(previewName);
                }
            }

            //More info button
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("More Info"))
            {
                if (getModuleInfoDelegate != null)
                {
                    moduleInfo = getModuleInfoDelegate(previewName);
                    Texture moduleLabel;

                    modSummary.ModuleInfo = moduleInfo;

                    if (this.getModuleLogoDelegate != null)
                    {
                        moduleLabel = getModuleLogoDelegate(previewName);
                        modSummary.moduleLabel = moduleLabel;
                    }
                    modSummary.ToggleVisible();

                }
            }

            if (GUILayout.Button("Reconfigure"))
                changeModuleTypeDelegate(previewName);

            GUILayout.EndHorizontal();
        }

        protected void drawConverters()
        {
            GUILayout.BeginVertical(GUILayout.MinHeight(110));
            string converterName = "??";
            string converterStatus = "??";
            bool isActivated;

            _scrollPosConverters = GUILayout.BeginScrollView(_scrollPosConverters, new GUIStyle(GUI.skin.textArea));

            foreach (ModuleResourceConverter converter in converters)
            {
                converterName = converter.ConverterName;
                converterStatus = converter.status;
                isActivated = converter.IsActivated;

                GUILayout.BeginVertical();

                //Toggle, name and status message
                if (!HighLogic.LoadedSceneIsEditor)
                    isActivated = GUILayout.Toggle(isActivated, string.Format(converterName + "({0:f1}%): ", converter.Efficiency * converter.EfficiencyBonus * 100f) + converterStatus);
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

            if (converters.Count == 0)
                GUILayout.Label("<color=yellow>No processors are present in this configuration.</color>");

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}