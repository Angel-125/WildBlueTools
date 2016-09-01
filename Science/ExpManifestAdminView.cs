using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015 - 2016, by Michael Billard (Angel-125)
License: GPLV3

If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    internal class ExpManifestAdminView : Window<ExpManifestAdminView>
    {
        static Texture loadIcon;
        static Texture transferIcon;
        static Texture experimentIcon;
        static Texture infoIcon;
        static Texture completedIcon;
        static Texture trashcanIcon;
        static Texture playIcon;
        static Texture pauseIcon;

        public WBIModuleScienceExperiment[] experimentSlots = null;
        public Part part = null;
        public WBIExperimentLab experimentLab = null;
        public LoadExperimentView loadExperimentView = new LoadExperimentView();

        private WBIModuleScienceExperiment experimentToTransfer = null;
        private Vector2 scrollPos = new Vector2(0, 0);
        private Vector2 scrollPosPanel = new Vector2(0, 0);
        private Vector2 scrollPosSynopsis = new Vector2(0, 0);
        private GUILayoutOption[] iconOptions = new GUILayoutOption[] { GUILayout.Width(24), GUILayout.Height(24) };
        private GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Width(64), GUILayout.Height(64) };
        private GUILayoutOption[] experimentPanelOptions = new GUILayoutOption[] { GUILayout.Height(85) };
        private GUILayoutOption[] synopsisOptions = new GUILayoutOption[] { GUILayout.Width(175) };
        private string experimentSynopsis;
        private bool experimentStatusVisible;
        private bool labGUIVisible;
        private bool clearExperimentConfirmed;
        private bool confirmFinalTransfer;
        private WBIModuleScienceExperiment lastExperimentSlot = null;

        public ExpManifestAdminView() :
            base("Experiment Manifest", 600, 330)
        {
            Resizable = false;
        }

        public void SetupView(Part parentPart, bool showExperimentStatus, bool showLabGUI, WBIExperimentLab lab = null)
        {
            setupIcons();

            experimentStatusVisible = showExperimentStatus;
            labGUIVisible = showLabGUI;
            part = parentPart;
            experimentLab = lab;
            WindowTitle = this.part.partInfo.title + ": Experiment Manifest";
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
        }

        protected override void DrawWindowContents(int windowId)
        {
            DrawGUIControls();
        }

        public void DrawGUIControls()
        {
            setupIcons();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

            //Lab Status & controls
            if (experimentLab != null && labGUIVisible)
                drawLabGUI();

            //Experiments
            drawExperiments();

            GUILayout.EndVertical();

            //Synopsis
            drawSynopsis();

            GUILayout.EndHorizontal();
        }

        protected void drawSynopsis()
        {
            scrollPosSynopsis = GUILayout.BeginScrollView(scrollPosSynopsis, synopsisOptions);
            if (experimentLab.debugMode)
                drawDebugInfo();
            if (string.IsNullOrEmpty(experimentSynopsis) == false)
                GUILayout.Label(experimentSynopsis);
            else
                GUILayout.Label(" ");
            GUILayout.EndScrollView();
        }

        protected void drawDebugInfo()
        {
            int totalResources = this.part.Resources.Count;
            PartResource resource;
            WBIModuleScienceExperiment experimentSlot;

            GUILayout.Label("Part Resources");
            for (int index = 0; index < totalResources; index++)
            {
                resource = this.part.Resources[index];
                GUILayout.Label(resource.resourceName);

                if (experimentLab.currentAmounts.ContainsKey(resource.resourceName))
                    GUILayout.Label(string.Format("Made: {0:f5}", experimentLab.currentAmounts[resource.resourceName]));

                if (experimentLab.shareAmounts.ContainsKey(resource.resourceName))
                    GUILayout.Label(string.Format("Per Exp: {0:f5}", experimentLab.shareAmounts[resource.resourceName]));

                GUILayout.Label(string.Format("Extra: {0:f3}/{1:f3}", resource.amount, resource.maxAmount));
            }

            if (experimentSlots == null)
                return;

            GUILayout.Label("Accumulated resources");
            for (int index = 0; index < experimentSlots.Length; index++)
            {
                experimentSlot = experimentSlots[index];
                if (string.IsNullOrEmpty(experimentSlot.accumulatedResources) == false)
                    GUILayout.Label(experimentSlot.experimentID + "\r\n" + experimentSlot.accumulatedResources);
            }
            GUILayout.Label("-------");
        }

        protected void drawExperiments()
        {
            int index;
            WBIModuleScienceExperiment experimentSlot;

            if (experimentSlots == null)
            {
                GUILayout.Label("No experiment slots available");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            for (index = 0; index < experimentSlots.Length; index++)
            {
                experimentSlot = experimentSlots[index];

                GUILayout.BeginVertical();

                GUILayout.BeginScrollView(scrollPosPanel, experimentPanelOptions);

                GUILayout.BeginHorizontal();

                //Transfer button
                Texture xFerIcon;
                if (HighLogic.LoadedSceneIsFlight)
                    xFerIcon = transferIcon;
                else
                    xFerIcon = loadIcon;
                if (GUILayout.Button(xFerIcon, buttonOptions))
                    transferExperiment(experimentSlot);
                GUILayout.BeginVertical();

                //Title
                GUILayout.BeginHorizontal();
                GUILayout.Label(experimentIcon, iconOptions);
                GUILayout.Label(experimentSlot.title);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                //Run/Pause (flight only, Experiment Lab only)
                if (experimentLab != null && labGUIVisible)
                {
                    if (HighLogic.LoadedSceneIsFlight && experimentSlot.isCompleted == false)
                    {
                        if (experimentSlot.isRunning)
                        {
                            if (GUILayout.Button(pauseIcon, iconOptions))
                                experimentSlot.isRunning = false;
                        }

                        else
                        {
                            if (GUILayout.Button(playIcon, iconOptions))
                                experimentSlot.isRunning = true;
                        }
                    }

                    if (experimentSlot.isRunning && experimentLab.IsActivated == false)
                        experimentLab.StartConverter();
                }

                //Trashcan
                if (GUILayout.Button(trashcanIcon, iconOptions))
                {
                    //Verify that user wants to clear the experiment.
                    //If so, then clear the experiment (load the default).
                    if (HighLogic.LoadedSceneIsFlight && !clearExperimentConfirmed)
                    {
                        clearExperimentConfirmed = true;
                        ScreenMessages.PostScreenMessage("Existing experiment will be removed. Click a second time to confirm.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }

                    else
                    {
                        clearExperimentConfirmed = false;
                        experimentSlot.ClearExperiment();
                    }
                }

                //Status
                if (experimentStatusVisible)
                {
                    if (experimentSlot.isCompleted)
                        GUILayout.Label(completedIcon, iconOptions);
                    else
                        GUILayout.Label(infoIcon, iconOptions);
                    GUILayout.Label(experimentSlot.status);
                }

                else if (experimentLab == null && experimentSlot.isCompleted)
                {
                    GUILayout.Label("Ready to be recovered");
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();

                //Hit test for the experiment panel
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    if (lastExperimentSlot != experimentSlot)
                    {
                        lastExperimentSlot = experimentSlot;
                        experimentSynopsis = experimentSlot.GetInfo();
                        scrollPosSynopsis = new Vector2(0, 0);
                    }
                }

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        protected void drawLabGUI()
        {
            StringBuilder builder = new StringBuilder();

            GUILayout.Label("<color=white><b>Status: </b>" + experimentLab.status + "</color>");
            GUILayout.Label("<color=white><b>Bonus Science Research: </b>" + experimentLab.lastAttempt + "</color>");
            GUILayout.Label(string.Format("<color=white><b>Bonus Science Gained (requires transmission): </b>{0:f2}</color>", experimentLab.scienceAdded));

            int totalCount = experimentLab.outputList.Count;
            for (int index = 0; index < totalCount; index++)
                builder.Append(experimentLab.outputList[index].ResourceName + ",");
            string outputs = builder.ToString();
            outputs = outputs.Substring(0, outputs.Length - 1);
            GUILayout.Label("<color=yellow><b>NOTE:</b> Bonus science and " + outputs + " can be generated without running experiments.</color>");

            if (experimentLab.IsActivated)
            {
                if (GUILayout.Button("Stop"))
                    experimentLab.StopConverter();
            }
            else if (GUILayout.Button("Start"))
            {
                experimentLab.StartConverter();
            }
        }
        
        protected void setupIcons()
        {
            if (transferIcon == null)
                transferIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/TransferIcon", false);
            if (loadIcon == null)
                loadIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/SelectExpIcon", false);
            if (experimentIcon == null)
                experimentIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/ExperimentIcon", false);
            if (infoIcon == null)
                infoIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/infoIcon", false);
            if (completedIcon == null)
                completedIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/CompletedIcon", false);
            if (trashcanIcon == null)
                trashcanIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/TrashCan", false);
            if (playIcon == null)
                playIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/PlayIcon", false);
            if (pauseIcon == null)
                pauseIcon  = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/PauseIcon", false);
        }

        protected void transferExperiment(WBIModuleScienceExperiment experimentSlot)
        {
            //If we're in the editor then show the experiment to load screen
            if (HighLogic.LoadedSceneIsEditor)
            {
                loadExperimentView.defaultExperiment = experimentSlot.defaultExperiment;
                loadExperimentView.transferRecipient = experimentSlot;
                loadExperimentView.part = this.part;
                loadExperimentView.windowPos = this.windowPos;
                loadExperimentView.windowPos.position += new Vector2(40.0f, 40.0f);
                loadExperimentView.SetVisible(true);
            }

            //If we're in flight then highlight the available experiment containers.
            else if (HighLogic.LoadedSceneIsFlight)
            {
                //Make sure we have an experiment to transfer.
                if (experimentSlot.experimentID == experimentSlot.defaultExperiment)
                {
                    ScreenMessages.PostScreenMessage("No experiment to transfer, this slot is empty.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }

                /*
                //If the experiment has done its final transfer then we're done.
                if (experimentSlot.finalTransfer)
                {
                    ScreenMessages.PostScreenMessage("This experiment is complete and cannot be transfered.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }

                //If the experiment is complete then ask user to confirm the final transfer.
                if (experimentSlot.isCompleted && confirmFinalTransfer == false)
                {
                    confirmFinalTransfer = true;
                    ScreenMessages.PostScreenMessage("This experiment is complete. Once transfered it cannot be transfered again. Click to confirm transfer.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
                confirmFinalTransfer = false;
                 */

                List<WBIExperimentManifest> manifests = this.part.vessel.FindPartModulesImplementing<WBIExperimentManifest>();
                List<WBIExperimentLab> labs = this.part.vessel.FindPartModulesImplementing<WBIExperimentLab>();
                WBIExperimentLab thisLab = this.part.FindModuleImplementing<WBIExperimentLab>();
                WBIExperimentManifest thisManifest = this.part.FindModuleImplementing<WBIExperimentManifest>();
                Color sourceColor = new Color(1, 1, 0);
                Color destinationColor = new Color(0, 191, 243);
                bool foundAvailableSlot = false;
                int index;
                int totalCount;
                WBIExperimentManifest manifest;
                WBIExperimentLab lab;

                //Highlight the manifests
                totalCount = manifests.Count;
                for (index = 0; index < totalCount; index++)
                {
                    manifest = manifests[index];

                    //Part can accept the experiment if it is not full.
                    if (manifest != thisManifest && manifest.HasAvailableSlots())
                    {
                        foundAvailableSlot = true;
                        manifest.part.highlighter.ConstantOn(destinationColor);
                        manifest.part.AddOnMouseDown(onPartMouseDown);
                    }

                    else
                    {
                        manifest.part.highlighter.ConstantOn(sourceColor);
                    }
                }

                //Highlight labs
                totalCount = labs.Count;
                for (index = 0; index < totalCount; index++)
                {
                    lab = labs[index];

                    //Part can accept the experiment if it is not full.
                    if (lab != thisLab && lab.HasAvailableSlots())
                    {
                        foundAvailableSlot = true;
                        lab.part.highlighter.ConstantOn(destinationColor);
                        lab.part.AddOnMouseDown(onPartMouseDown);
                    }

                    else
                    {
                        lab.part.highlighter.ConstantOn(sourceColor);
                    }
                }

                //If we couldn't find an available slot then don't transfer the experiment.
                if (foundAvailableSlot)
                {
                    //Save the experiment to transfer
                    experimentToTransfer = experimentSlot;

                    //Tell user what to do
                    ScreenMessages.PostScreenMessage("Click a destination to receive the experiment. Press the ESCAPE key to cancel.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "ExperimentManifestLock");
                }

                else
                {
                    //Clear highlights
                    if (thisManifest != null)
                        thisManifest.part.highlighter.Off();
                    if (thisLab != null)
                        thisLab.part.highlighter.Off();
                    ScreenMessages.PostScreenMessage("You need more experiment space.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
            }

        }

        protected void onPartMouseDown(Part partClicked)
        {
            List<WBIExperimentManifest> manifests = this.part.vessel.FindPartModulesImplementing<WBIExperimentManifest>();
            List<WBIExperimentLab> labs = this.part.vessel.FindPartModulesImplementing<WBIExperimentLab>();
            WBIExperimentLab thisLab = this.part.FindModuleImplementing<WBIExperimentLab>();
            WBIExperimentManifest thisManifest = this.part.FindModuleImplementing<WBIExperimentManifest>();
            int index;
            int totalCount;
            WBIExperimentManifest manifest;
            WBIExperimentLab lab;

            //Clear all the highlights
            totalCount = manifests.Count;
            for (index = 0; index < totalCount; index++)
            {
                manifest = manifests[index];

                //Highlighter off
                manifest.part.highlighter.Off();

                //Remove mouse down handler
                if (manifest != thisManifest)
                    manifest.part.RemoveOnMouseDown(onPartMouseDown);

                //Transfer the experiment
                if (manifest.part == partClicked)
                    manifest.TransferExperiment(experimentToTransfer);
            }

            totalCount = labs.Count;
            for (index = 0; index < totalCount; index++)
            {
                lab = labs[index];

                //Highlighter off
                lab.part.highlighter.Off();

                //Remove mouse down handler
                if (lab != thisLab)
                    lab.part.RemoveOnMouseDown(onPartMouseDown);

                //Transfer the experiment
                if (lab.part == partClicked)
                    lab.TransferExperiment(experimentToTransfer);
            }

            //Clear the xperiment to transfer
            experimentToTransfer = null;

            //Remove input lock
            InputLockManager.RemoveControlLock("ExperimentManifestLock");
        }

        public void EscapeKeyPressed()
        {
            //Check to see if user canceled the transfer
            if (experimentToTransfer != null)
            {
                List<WBIExperimentManifest> manifests = this.part.vessel.FindPartModulesImplementing<WBIExperimentManifest>();
                List<WBIExperimentLab> labs = this.part.vessel.FindPartModulesImplementing<WBIExperimentLab>();
                WBIExperimentLab thisLab = this.part.FindModuleImplementing<WBIExperimentLab>();
                WBIExperimentManifest thisManifest = this.part.FindModuleImplementing<WBIExperimentManifest>();
                int index;
                int totalCount;
                WBIExperimentManifest manifest;
                WBIExperimentLab lab;

                //Clear all the highlights
                totalCount = manifests.Count;
                for (index = 0; index < totalCount; index++)
                {
                    manifest = manifests[index];

                    //Highlighter off
                    manifest.part.highlighter.Off();

                    //Remove mouse down handler
                    if (manifest != thisManifest)
                        manifest.part.RemoveOnMouseDown(onPartMouseDown);
                }

                totalCount = labs.Count;
                for (index = 0; index < totalCount; index++)
                {
                    lab = labs[index];

                    //Highlighter off
                    lab.part.highlighter.Off();

                    //Remove mouse down handler
                    if (lab != thisLab)
                        lab.part.RemoveOnMouseDown(onPartMouseDown);
                }

                //Clear the xperiment to transfer
                experimentToTransfer = null;
                confirmFinalTransfer = false;

                //Remove input lock
                InputLockManager.RemoveControlLock("ExperimentManifestLock");
            }
        }

    }
}
