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
    [KSPModule("Active Radiator")]
    class WBIActiveRadiatorToggle : PartModule
    {
        ModuleActiveRadiator radiator;

        [KSPField(guiActive = true)]
        public string Status;

        [KSPField(isPersistant = true)]
        public bool isCooling;

        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ToggleCooling()
        {
            isCooling = !isCooling;

            radiator.isEnabled = isCooling;
            radiator.enabled = isCooling;

            UpdateStatus();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            radiator = this.part.FindModuleImplementing<ModuleActiveRadiator>();
            radiator.isEnabled = isCooling;
            radiator.enabled = isCooling;

            UpdateStatus();
        }

        public void UpdateStatus()
        {
            if (isCooling)
            {
                Status = "Cooling";
                Events["ToggleCooling"].guiName = "Turn cooling off";
            }

            else
            {
                Status = "Off";
                Events["ToggleCooling"].guiName = "Turn cooling on";
            }
        }
    }
}
