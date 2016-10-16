using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    [KSPModule("Convertible MPL")]
    public class WBIConvertibleMPL : WBIMultiConverter
    {
        [KSPField]
        public string opsViewTitle = string.Empty;

        private float originalCrewsRequired;

        public override void OnStart(StartState state)
        {
            ModuleScienceLab sciLab = this.part.FindModuleImplementing<ModuleScienceLab>();
            if (sciLab != null)
                originalCrewsRequired = sciLab.crewsRequired;

            base.OnStart(state);

            if (HighLogic.LoadedSceneIsEditor == false && fieldReconfigurable == false)
                ShowGUI = false;

            if (string.IsNullOrEmpty(opsViewTitle) == false)
                opsManagerView.WindowTitle = opsViewTitle;

            if (string.IsNullOrEmpty(resourcesToKeep))
                resourcesToKeep = "ElectricCharge";
        }

        public override void RedecorateModule(bool loadTemplateResources = true)
        {
            base.RedecorateModule(loadTemplateResources);
            updateScienceLab();
            updateWorkshop();
        }

        protected void updateScienceLab()
        {
            bool enableMPLModules = false;

            if (CurrentTemplate.HasValue("enableMPLModules"))
                enableMPLModules = bool.Parse(CurrentTemplate.GetValue("enableMPLModules"));

            ModuleScienceLab sciLab = this.part.FindModuleImplementing<ModuleScienceLab>();
            if (sciLab != null)
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    sciLab.isEnabled = false;
                    sciLab.enabled = false;
                }

                else if (enableMPLModules)
                {
                    sciLab.isEnabled = true;
                    sciLab.enabled = true;
                    sciLab.crewsRequired = originalCrewsRequired;
                }
                else
                {
                    sciLab.crewsRequired = 2000.0f;
                    sciLab.isEnabled = false;
                    sciLab.enabled = false;
                }
            }
        }

        protected void updateWorkshop()
        {
            PartModule oseWorkshop = null;
            PartModule oseRecycler = null;
            bool enableWorkshop = false;

            //See if the workshop is enabled.
            if (CurrentTemplate.HasValue("enableWorkshop"))
                enableWorkshop = bool.Parse(CurrentTemplate.GetValue("enableWorkshop"));

            //Find the workshop modules
            foreach (PartModule pm in this.part.Modules)
            {
                if (pm.moduleName == "OseModuleWorkshop")
                    oseWorkshop = pm;
                else if (pm.moduleName == "OseModuleRecycler")
                    oseRecycler = pm;
            }

            if (oseWorkshop != null)
            {
                oseWorkshop.enabled = enableWorkshop;
                oseWorkshop.isEnabled = enableWorkshop;
            }

            if (oseRecycler != null)
            {
                oseRecycler.enabled = enableWorkshop;
                oseRecycler.isEnabled = enableWorkshop;
            }
        }
    }
}
