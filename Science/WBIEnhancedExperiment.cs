using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2015-2017, by Michael Billard (Angel-125)
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
    [KSPModule("Science Experiment")]
    public class WBIEnhancedExperiment : WBIModuleScienceExperiment
    {
        [KSPField]
        public string decalTransform = string.Empty;

        [KSPField]
        public string decalPath = string.Empty;

        protected InfoView infoView;

        [KSPEvent(guiName = "Show Synopsis", guiActiveEditor = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ShowSynopsis()
        {
            //Set info text
            infoView.ModuleInfo = this.GetInfo();

            //Show view
            infoView.SetVisible(true);
        }

        [KSPEvent(guiName = "Start Experiment", guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void StartExperiment()
        {
            isRunning = true;
            isCompleted = false;
            Events["StopExperiment"].active = true;
            Events["StartExperiment"].active = false;
            rebuildResourceMap();
        }

        [KSPEvent(guiName = "Stop Experiment", guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void StopExperiment()
        {
            isRunning = false;
            status = "Ready to run";
            Events["StopExperiment"].active = false;
            Events["StartExperiment"].active = true;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            LoadFromDefinition(experimentID);
            setupDecal();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            infoView = new InfoView();
            setupGUI();

            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorPartPlaced.Add(onPartPlaced);
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorPartPlaced.Remove(onPartPlaced);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //Check auto restart field.
            if (!rerunnable && Fields["autoRestartExperiment"].guiActive && HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
            {
                Fields["autoRestartExperiment"].guiActive = false;
                Fields["autoRestartExperiment"].guiActiveEditor = false;
            }

            //If we're not in flight, then we're done.
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //Base class actions and events are always disabled.
            Events["DeployExperiment"].active = false;
            Events["DeployExperimentExternal"].active = false;
            Actions["DeployAction"].active = false;

            //If the experiment isn't running then we're done.
            if (!isRunning)
            {
                //Check for reset
                if (isCompleted && !rerunnable && (resettable || resettableOnEVA) && !Events["ResetExperiment"].active && !Events["ResetExperimentExternal"].active && !Events["StartExperiment"].active)
                {
                    Events["StartExperiment"].active = true;
                }
                return;
            }

            //If the experiment has been completed then stop the experiment from running.
            if (isCompleted)
            {
                StopExperiment();
                Events["ReviewDataEvent"].active = true;
                Events["TransferDataEvent"].active = true;
                if (!rerunnable)
                    Events["StartExperiment"].active = false;
                status = "Completed";

                if (autoRestartExperiment)
                {
                    ResetExperiment();
                    StartExperiment();
                }
                else
                {
                    ModuleResourceConverter converter = this.part.FindModuleImplementing<ModuleResourceConverter>();
                    if (converter != null && converter.IsActivated)
                        converter.StopResourceConverter();
                }
                return;
            }

            //Check for completion
            CheckCompletion();
        }

        protected void onPartPlaced(Part partPlaced)
        {
            if (partPlaced == this.part)
            {
                //Set the tooltip.
                if (ToolTipScenario.Instance.HasDisplayedToolTip(this.part.partInfo.name))
                    return;
                ToolTipScenario.Instance.AddToolTipDisplayedFlag(this.part.partInfo.name);

                LoadFromDefinition(experimentID);
                StringBuilder requirements = new StringBuilder();
                string requirementsText = string.Empty;

                //Mininum Crew
                if (minCrew > 0)
                    requirements.Append("<b>This part requires a minimum of </b>" + minCrew + " <b>crew.</b>\r\n");

                //Required parts
                if (requiredParts != null && requiredParts.Length > 0)
                {
                    //If our part is on the list, then we're done.
                    if (requiredParts.Contains(this.part.partInfo.title))
                        return;

                    //Build the list of required parts.
                    requirements.Append("<b>This part also requires at least one of: </b>\r\n");
                    for (int index = 0; index < requiredParts.Length; index++)
                        requirements.Append(requiredParts[index] + "\r\n");
                }

                requirementsText = requirements.ToString();
                if (string.IsNullOrEmpty(requirementsText) == false)
                {
                    infoView.ModuleInfo = requirementsText;
                    infoView.SetVisible(true);
                }
            }
        }

        protected void setupGUI()
        {
            //Base class actions and events are always disabled.
            Events["DeployExperiment"].active = false;
            Events["DeployExperimentExternal"].active = false;
            Actions["DeployAction"].active = false;

            //Setup experiment GUI
            Fields["status"].guiActive = true;
            Fields["autoRestartExperiment"].guiActive = true;
            Events["ReviewDataEvent"].active = isCompleted;
            Events["TransferDataEvent"].active = isCompleted;
            if (isRunning)
            {
                Events["StartExperiment"].active = false;
                Events["StopExperiment"].active = true;
            }

            else
            {
                status = "Ready to run";
                Events["StartExperiment"].active = true;
                Events["StopExperiment"].active = false;
            }
        }

        protected void setupDecal()
        {
            //Setup decal
            if (string.IsNullOrEmpty(decalPath) == false && string.IsNullOrEmpty(decalTransform) == false)
            {
                Transform decal = this.part.FindModelTransform(decalTransform);
                if (decal == null)
                    return;

                Renderer renderer = decal.GetComponent<Renderer>();
                if (renderer == null)
                    return;

                Texture decalTexture = GameDatabase.Instance.GetTexture(decalPath, false);
                if (decalTexture == null)
                    return;
                renderer.material.SetTexture("_MainTex", decalTexture);
            }
        }
    }
}
