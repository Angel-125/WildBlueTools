using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2018, by Michael Billard (Angel-125)
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
    public class WBIAquaticRCS: ModuleRCSFX
    {
        /// <summary>
        /// Name of the part's intake transform.
        /// </summary>
        [KSPField]
        public string intakeTransformName = "intakeTransform";

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            originalThrustPower = thrusterPower;

            //Get the intake transforms
            if (!string.IsNullOrEmpty(intakeTransformName))
                intakeTransforms = this.part.FindModelTransforms(intakeTransformName).ToArray();
        }

        protected Transform[] intakeTransforms;
        protected float originalThrustPower;

        protected override void UpdatePowerFX(bool running, int idx, float power)
        {
            //Make sure at least one of our intake transforms is underwater.
            if (intakeTransforms == null)
                return;
            if (!this.part.vessel.mainBody.ocean)
            {
                thrusterPower = 0.0f;
                base.UpdatePowerFX(false, idx, power);
                return;
            }
            if (!this.part.vessel.Splashed)
                return;

            bool intakeIsUnderwater = false;
            for (int index = 0; index < intakeTransforms.Length; index++)
            {
                if (FlightGlobals.getAltitudeAtPos((Vector3d)intakeTransforms[index].position, this.part.vessel.mainBody) <= 0.0f)
                {
                    intakeIsUnderwater = true;
                    break;
                }
            }
            if (!intakeIsUnderwater)
            {
                thrusterPower = 0.0f;
                base.UpdatePowerFX(false, idx, power);
                return;
            }

            //Update the FX
            thrusterPower = originalThrustPower;
            base.UpdatePowerFX(running, idx, power);

            //Refresh our reserves. This is primarily to simulate intake of IntakeLqd.
            //Why do this? Because ModuleResourceIntake will fill all resource containers on the vessel.
            //So what we do is have the part contain a small amount of IntakeLqd, and make flow for it NO_FLOW.
            int count = propellants.Count;
            Propellant propellant;
            for (int index = 0; index < count; index++)
            {
                propellant = propellants[index];
                if (this.part.Resources.Contains(propellant.name))
                {
                    this.part.Resources[propellant.name].amount = this.part.Resources[propellant.name].maxAmount;
                }
            }
        }
    }
}
