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
    public delegate bool RequirementsDelegate(WBIAquaticEngine aquaticEngine);

    /// <summary>
    /// This class is an engine that only runs underwater. It needs no resource intake; if underwater then it'll auto-replenish the part's resource reserves.
    /// </summary>
    public class WBIAquaticEngine: ModuleEnginesFX
    {
        /// <summary>
        /// Flag to indicate whether or not the engine is in reverse-thrust mode.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool isReverseThrust;

        #region Housekeeping
        public RequirementsDelegate checkRequirements;
        public bool isUnderwater;
        #endregion

        #region Events
        [KSPEvent(guiActive = true, guiName = "Reverse Thrust")]
        public void ToggleReverseThrust()
        {
            isReverseThrust = !isReverseThrust;
            reverseThrustTransform();
            updateGUI();
        }

        [KSPAction("Toggle forward/reverse thrust")]
        public void ToggleReverseThrustAction(KSPActionParam param)
        {
            ToggleReverseThrust();
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (isReverseThrust)
                reverseThrustTransform();
            updateGUI();
        }

        public override bool CheckDeprived(double requiredPropellant, out string propName)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return base.CheckDeprived(requiredPropellant, out propName);

            //Check external requirements to see if the engine can run.
            //Example: when the boat's supercavitation effect is on, aquatic engines can't run- they're in a buble of air and the water intakes are dry!
            isUnderwater = checkUnderwater();
            bool requirementsMet = true;
            if (checkRequirements != null)
                requirementsMet = checkRequirements(this);

            //If we're underwater, then let the engine decide if we're deprived of propellants.
            if (isUnderwater && requirementsMet)
            {
                return base.CheckDeprived(requiredPropellant, out propName);
            }

            //Clear our resource reserves, then let the engine decide what to do.
            //This will force the engine to flame out because we don't have any IntakeLqd.
            else
            {
                int count = this.part.Resources.Count;
                for (int index = 0; index < count; index++)
                    this.part.Resources[index].amount = 0.0f;
            }

            return base.CheckDeprived(requiredPropellant, out propName);
        }

        public override void FXUpdate()
        {
            base.FXUpdate();

            //Make sure we're underwater
            if (!checkUnderwater())
                return;

            //Refresh our reserves. This is primarily to simulate intake of IntakeLqd.
            //Why do this? Because ModuleResourceIntake will fill all resource containers on the vessel.
            //So what we do is have the part contain a small amount of IntakeLqd, and make flow for it NO_FLOW.
            int count = this.part.Resources.Count;
            PartResource partResource;
            for (int index = 0; index < count; index++)
            {
                partResource = this.part.Resources[index];

                if (partResource.resourceName == "ElectricCharge")
                    continue;

                partResource.amount = partResource.maxAmount;
            }
        }
        #endregion

        #region Helpers
        protected bool checkUnderwater()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return false;
            if (!this.part.vessel.mainBody.ocean)
                return false;
            if (!this.part.vessel.Splashed)
                return false;

            int count = thrustTransforms.Count;
            for (int index = 0; index < count; index++)
            {
                if (FlightGlobals.getAltitudeAtPos((Vector3d)thrustTransforms[index].position, this.part.vessel.mainBody) <= 0.0f)
                    return true;
            }
            return true;
        }

        protected void updateGUI()
        {
            Events["ToggleReverseThrust"].guiName = isReverseThrust ? "Set Forward Thrust" : "Set Reverse Thrust";
        }

        protected void reverseThrustTransform()
        {
            int count = thrustTransforms.Count;
            Transform transform;
            for (int index = 0; index < count; index++)
            {
                transform = thrustTransforms[index];
                transform.Rotate(0, 180.0f, 0);
            }
        }
        #endregion
    }
}
