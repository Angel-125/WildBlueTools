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
    public class WBISolarPanelHelper : PartModule
    {
        [KSPField(guiActive = false, guiName = "Status")]
        public string status = "Blocked by attached parts";

        protected ModuleDeployableSolarPanel solarPanel;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            solarPanel = this.part.FindModuleImplementing<ModuleDeployableSolarPanel>();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (solarPanel == null)
                return;

            if (this.part.children.Count > 0 && solarPanel.enabled)
            {
                Fields["status"].guiActive = true;
                solarPanel.enabled = false;
                solarPanel.isEnabled = false;
            }

            else if (this.part.children.Count == 0 && solarPanel.enabled == false)
            {
                Fields["status"].guiActive = false;
                solarPanel.enabled = true;
                solarPanel.isEnabled = true;
            }
        }
    }
}
