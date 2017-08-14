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
    public interface IParentView
    {
        void SetParentVisible(bool isVisible);
    }

    public interface IOpsView
    {
        List<string> GetButtonLabels();
        void DrawOpsWindow(string buttonLabel);
        void SetParentView(IParentView parentView);
        void SetContextGUIVisible(bool isVisible);
        string GetPartTitle();
    }

    public class WBIOpsManager : WBIConvertibleStorage, ICanBreak
    {
        [KSPField(isPersistant = true)]
        public int activeConverters; 

        [KSPField(isPersistant = true)]
        public bool isBroken;

        protected OpsManagerView opsManagerView = new OpsManagerView();
        protected ModuleQualityControl qualityControl = null;

        public override void OnStart(StartState state)
        {
            if (logoPanelTransforms != null)
                opsManagerView.hasDecals = true;
            opsManagerView.part = this.part;
            opsManagerView.storageView = this.storageView;
            opsManagerView.setActiveConverterCount = setActiveConverterCount;

            base.OnStart(state);
            Events["ReconfigureStorage"].guiName = "Manage Operations";
        }

        public void Destroy()
        {
            qualityControl.onPartBroken -= OnPartBroken;
            qualityControl.onPartFixed -= OnPartFixed;
        }

        protected void setActiveConverterCount(int count)
        {
            //If the active converter count != the current count and
            //at least one converter is active, then perform a quality check
            if (activeConverters != count && count > 0)
                qualityControl.PerformQualityCheck();

            //Record the new count.
            activeConverters = count;

            //Wake up the qualityControl
            qualityControl.UpdateActivationState();
        }

        #region ICanBreak
        public string GetCheckSkill()
        {
            if (CurrentTemplate.HasValue("reconfigureSkill"))
                return CurrentTemplate.GetValue("reconfigureSkill");
            else
                return "RepairSkill";
        }

        public bool ModuleIsActivated()
        {
            if (activeConverters > 0)
                return true;
            else
                return false;
        }

        public void SubscribeToEvents(ModuleQualityControl moduleQualityControl)
        {
            qualityControl = this.part.FindModuleImplementing<ModuleQualityControl>();
            qualityControl.onPartBroken += OnPartBroken;
            qualityControl.onPartFixed += OnPartFixed;
        }

        public void OnPartFixed(ModuleQualityControl qualityControl)
        {
            isBroken = false;
            opsManagerView.isBroken = isBroken;
        }

        public void OnPartBroken(ModuleQualityControl qualityControl)
        {
            isBroken = true;
            opsManagerView.isBroken = isBroken;

            List<ModuleResourceConverter> converters = this.part.FindModulesImplementing<ModuleResourceConverter>();
            foreach (ModuleResourceConverter converter in converters)
                converter.StopResourceConverter();
        }
        #endregion

        public override void ReconfigureStorage()
        {
            setupStorageView(CurrentTemplateIndex);
            opsManagerView.SetVisible(true);
        }

        public override void OnGUI()
        {
            if (opsManagerView.IsVisible())
            {
                opsManagerView.DrawWindow();
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            //Show/hide the inflate/deflate button depending upon whether or not crew is aboard
            if (isInflatable && HighLogic.LoadedSceneIsFlight)
            {
                if (this.part.protoModuleCrew.Count() > 0)
                {
                    Events["ToggleInflation"].guiActive = false;
                    Events["ToggleInflation"].guiActiveUnfocused = false;
                }

                else
                {
                    Events["ToggleInflation"].guiActive = true;
                    Events["ToggleInflation"].guiActiveUnfocused = true;
                }
            }
        }

        protected override void loadModulesFromTemplate(ConfigNode templateNode)
        {
            base.loadModulesFromTemplate(templateNode);
            opsManagerView.UpdateButtonTabs();
        }

        #region IOpsView
        public override List<string> GetButtonLabels()
        {
            return opsManagerView.GetButtonLabels();
        }

        public override void DrawOpsWindow(string buttonLabel)
        {
            opsManagerView.DrawOpsWindow(buttonLabel);
        }
        #endregion
    }
}
