using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIModuleDisableDecoupler : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool decouplerEnabled = true;

        [KSPEvent(guiName = "Decoupler Enabled", guiActiveEditor = true, guiActive = false)]
        public void ToggleDecouplerEnabled()
        {
            decouplerEnabled = !decouplerEnabled;

            setupDecoupler();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (decouplerEnabled)
                Events["ToggleDecouplerEnabled"].guiName = "Disable Decoupler";
            else
                Events["ToggleDecouplerEnabled"].guiName = "Enable Decoupler";

            ModuleDecouple decoupler = this.part.FindModuleImplementing<ModuleDecouple>();
            if (!decoupler)
                return;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!decouplerEnabled)
                {
                    decoupler.enabled = false;
                    decoupler.isEnabled = false;
                }
            }

            else if (HighLogic.LoadedSceneIsEditor)
            {
                if (decouplerEnabled)
                {
                    Events["ToggleDecouplerEnabled"].guiName = "Disable Decoupler";
                    decoupler.enabled = true;
                    decoupler.isEnabled = true;
                }

                else
                {
                    Events["ToggleDecouplerEnabled"].guiName = "Enable Decoupler";
                    decoupler.enabled = false;
                    decoupler.isEnabled = false;
                }
            }
        }

        protected void setupDecoupler()
        {
            ModuleDecouple decoupler = this.part.FindModuleImplementing<ModuleDecouple>();
            if (!decoupler)
                return;

            if (decouplerEnabled)
            {
                Events["ToggleDecouplerEnabled"].guiName = "Disable Decoupler";
                this.part.stagingOn = true;
                decoupler.enabled = true;
                decoupler.isEnabled = true;
                decoupler.Events["ToggleStaging"].Invoke();
                decoupler.stagingEnabled = true;
            }

            else
            {
                Events["ToggleDecouplerEnabled"].guiName = "Enable Decoupler";
                decoupler.Events["ToggleStaging"].Invoke();
                decoupler.stagingEnabled = false;
                decoupler.enabled = false;
                decoupler.isEnabled = false;
            }
        }
    }
}
