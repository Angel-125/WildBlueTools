using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
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

    public class WBIOpsManager : WBIConvertibleStorage
    {
        [KSPField(isPersistant = true)]
        public int activeConverters; 

        [KSPField(isPersistant = true)]
        public bool isBroken;

        [KSPField(isPersistant = true)]
        public bool isMothballed;

        [KSPField]
        public bool canConfigureWhenDeflated = false;

        protected OpsManagerView opsManagerView;

        public override void OnStart(StartState state)
        {
            opsManagerView = new OpsManagerView();
            if (logoPanelTransforms != null)
                opsManagerView.hasDecals = true;
            opsManagerView.part = this.part;
            opsManagerView.storageView = this.storageView;
            opsManagerView.setActiveConverterCount = setActiveConverterCount;
            opsManagerView.isAssembled = (this.isInflatable && this.isDeployed) || !this.isInflatable;
            opsManagerView.fieldReconfigurable = this.fieldReconfigurable;

            base.OnStart(state);
            Events["ReconfigureStorage"].guiName = "Manage Operations";
            Events["ReconfigureStorage"].active = getAssembledState();
        }

        public void OnDestroy()
        {
        }

        protected void setActiveConverterCount(int count)
        {
            //Record the new count.
            activeConverters = count;
        }

        public override void ReconfigureStorage()
        {
            setupStorageView(CurrentTemplateIndex);
            setupOpsView();
            opsManagerView.SetVisible(true);
        }

        public override void ToggleInflation()
        {
            base.ToggleInflation();
            Events["ReconfigureStorage"].active = getAssembledState();
            opsManagerView.isAssembled = getAssembledState();
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
            setupOpsView();
            opsManagerView.DrawOpsWindow(buttonLabel);
        }
        #endregion

        protected void setupOpsView()
        {
            if (opsManagerView != null)
                return;
            opsManagerView = new OpsManagerView();
            if (logoPanelTransforms != null)
                opsManagerView.hasDecals = true;
            opsManagerView.part = this.part;
            opsManagerView.storageView = this.storageView;
            opsManagerView.setActiveConverterCount = setActiveConverterCount;
            opsManagerView.fieldReconfigurable = this.fieldReconfigurable;
            opsManagerView.isAssembled = getAssembledState();
        }

        protected bool getAssembledState()
        {
            return (this.isInflatable && this.isDeployed) || !this.isInflatable || canConfigureWhenDeflated;
        }
    }
}
