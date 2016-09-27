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
    public class WBIDualAxisSolarArray : ModuleDeployableSolarPanel
    {
        [KSPField()]
        public int rotationModuleIndex;

        ModuleDeployableSolarPanel rotationModule;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            List<ModuleDeployableSolarPanel> solarModules = this.part.FindModulesImplementing<ModuleDeployableSolarPanel>();

            if (solarModules.Count > 0)
            {
                if (rotationModuleIndex >= 0 && rotationModuleIndex < solarModules.Count - 1)
                    rotationModule = solarModules[rotationModuleIndex];
            }

            if (rotationModule != null)
            {
                foreach (BaseField field in rotationModule.Fields)
                {
                    field.guiActive = false;
                    field.guiActiveEditor = false;
                }

                foreach (BaseEvent baseEvent in rotationModule.Events)
                {
                    baseEvent.guiActive = false;
                    baseEvent.guiActiveEditor = false;
                    baseEvent.guiActiveUnfocused = false;
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (rotationModule == null)
                return;

            if (deployState == DeployState.RETRACTED && rotationModule.isEnabled)
            {
                rotationModule.enabled = false;
                rotationModule.isEnabled = false;
            }

            else if (deployState == DeployState.EXTENDED && rotationModule.isEnabled == false)
            {
                rotationModule.enabled = true;
                rotationModule.isEnabled = true;
            }
        }
    }
}