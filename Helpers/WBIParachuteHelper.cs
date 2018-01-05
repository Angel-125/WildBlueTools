using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using RUI.Icons.Selectable;

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
    public class WBIParachuteHelper : PartModule
    {
        [KSPField(guiName = "Auto Cut", isPersistant = true, guiActiveEditor = true, guiActive = true)]
        [UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool enableAutoCut;

        [KSPField(guiName = "Cut Altitude", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(minValue = 50, stepIncrement = 50, maxValue = 5000)]
        public float cutAltitude;

        ModuleParachute parachute;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            parachute = this.part.FindModuleImplementing<ModuleParachute>();
        }

        public void Update()
        {
            Fields["cutAltitude"].guiActive = enableAutoCut;
            Fields["cutAltitude"].guiActiveEditor = enableAutoCut;
        }

        public void FixedUpdate()
        {
            if (enableAutoCut && HighLogic.LoadedSceneIsFlight)
            {
                if (parachute.deploymentState == ModuleParachute.deploymentStates.DEPLOYED)
                {
                    //Check cut altitude
                    if (this.part.vessel.altitude <= cutAltitude)
                    {
                        parachute.CutParachute();
                    }
                }
            }
        }
    }
}
