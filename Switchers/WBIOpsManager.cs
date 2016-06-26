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

    public class WBIOpsManager : WBIConvertibleStorage
    {
        protected OpsManagerView opsManagerView = new OpsManagerView();

        public override void OnStart(StartState state)
        {
            if (logoPanelTransforms != null)
                opsManagerView.hasDecals = true;
            opsManagerView.part = this.part;
            opsManagerView.storageView = this.storageView;

            base.OnStart(state);
            Events["ReconfigureStorage"].guiName = "Manage Operations";
        }

        public override void ReconfigureStorage()
        {
            setupStorageView(CurrentTemplateIndex);
            opsManagerView.SetVisible(true);
        }

        public override void OnGUI()
        {
            if (opsManagerView.IsVisible())
                opsManagerView.DrawWindow();
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
            List<string> buttonLabels = new List<string>();

            //Get our part modules
            opsManagerView.UpdateConverters();
            opsManagerView.GetPartModules();

            buttonLabels.Add("Config");
            buttonLabels.Add("Command");
            buttonLabels.Add("Resources");
            buttonLabels.Add("Converters");

            return buttonLabels;
        }

        public override void DrawOpsWindow(string buttonLabel)
        {
            opsManagerView.DrawOpsWindow(buttonLabel);
        }
        #endregion
    }
}
