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
    public class WBIFieldUpgrade : PartModule
    {
        private const string kInsufficientParts = "Insufficient resources to upgrade the {0:s}. You need a total of {1:f2} {2:s} to reconfigure.";
        private const string kInsufficientSkill = "Insufficient skill to upgrade the {0:s}. You need one of: ";
        private const string kInsufficientCrew = "Cannot upgrade. Either crew the vessel or perform an EVA.";

        [KSPField]
        public string upgradeResource = string.Empty;

        [KSPField]
        public float upgradeCost = 0f;

        [KSPField]
        public bool canEVAUpgrade;

        [KSPField]
        public bool canInShipUpgrade;

        [KSPField]
        public string upgradeSkill = string.Empty;

        [KSPEvent(unfocusedRange = 3.0f)]
        public virtual void Upgrade()
        {
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (canEVAUpgrade)
                Events["Upgrade"].guiActiveUnfocused = true;

            if (canInShipUpgrade)
                Events["Upgrade"].guiActive = true;
        }

        protected virtual bool shouldAutoUpgrade()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return true;

            if (!WBIMainSettings.PayToReconfigure && !WBIMainSettings.RequiresSkillCheck)
                return true;
            else
                return false;
        }

        protected virtual bool payUpgradeCost()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!WBIMainSettings.PayToReconfigure)
                return true;
            if (upgradeCost == 0f)
                return true;
            if (string.IsNullOrEmpty(upgradeResource))
                return true;

            PartResourceDefinition definition = ResourceHelper.DefinitionForResource(upgradeResource);
            double resourcePaid = FlightGlobals.ActiveVessel.rootPart.RequestResource(definition.id, upgradeCost, ResourceFlowMode.ALL_VESSEL); ;

            //Could we afford it?
            if (Math.Abs(resourcePaid) / Math.Abs(upgradeCost) < 0.999f)
            {
                //Put back what we took
                this.part.RequestResource(definition.id, -resourcePaid, ResourceFlowMode.ALL_VESSEL);
                return false;
            }

            return true;
        }

        protected virtual bool hasSufficientSkill()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return true;
            if (!WBIMainSettings.RequiresSkillCheck)
                return true;
            if (string.IsNullOrEmpty(upgradeSkill))
                return true;
            if (Utils.IsExperienceEnabled() == false)
                return true;
            bool hasAtLeastOneCrew = false;

            string[] skillTraits = Utils.GetTraitsWithEffect(upgradeSkill);
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < skillTraits.Length; index++)
            {
                builder.Append(skillTraits[index]);
                builder.Append(",");
            }

            string errorMessage = string.Format(kInsufficientSkill + builder.ToString(), this.part.partInfo.title);

            //Make sure we have an experienced person either out on EVA performing the reconfiguration, or inside the module.
            //Check EVA first
            if (FlightGlobals.ActiveVessel.isEVA)
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                ProtoCrewMember astronaut = vessel.GetVesselCrew()[0];

                if (astronaut.HasEffect(upgradeSkill) == false)
                {
                    ScreenMessages.PostScreenMessage(errorMessage, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
                return true;
            }

            //Now check the vessel
            foreach (ProtoCrewMember protoCrew in this.part.vessel.GetVesselCrew())
            {
                if (protoCrew.HasEffect(upgradeSkill))
                {
                    hasAtLeastOneCrew = true;
                    break;
                }
            }

            if (!hasAtLeastOneCrew)
            {
                ScreenMessages.PostScreenMessage(kInsufficientCrew, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            //Yup, we have sufficient skill.
            return true;
        }

        protected virtual bool canAffordUpgrade()
        {
            if (string.IsNullOrEmpty(upgradeResource))
                return true;

            //If the active vessel is a kerbal, see if the kerbal has the resources
            double totalResources;
            if (FlightGlobals.ActiveVessel.isEVA)
            {
                Vessel vessel = FlightGlobals.ActiveVessel;
                totalResources = ResourceHelper.GetTotalResourceAmount(upgradeResource, vessel);
                if (totalResources >= upgradeCost)
                    return true;
            }

            //Check the ship
            totalResources = ResourceHelper.GetTotalResourceAmount(upgradeResource, this.part.vessel);
            if (totalResources >= upgradeCost)
                return true;

            string errorMessage = string.Format(kInsufficientParts, this.part.partInfo.title, upgradeCost, upgradeResource);
            ScreenMessages.PostScreenMessage(errorMessage, 5.0f, ScreenMessageStyle.UPPER_CENTER);
            return false;
        }

    }
}
