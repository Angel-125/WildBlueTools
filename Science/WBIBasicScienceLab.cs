﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
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
    [KSPModule("Basic Science Lab")]
    public class WBIBasicScienceLab : WBIResourceConverter
    {
        private const float kBaseResearchDivisor = 1.75f;
        private const float kDefaultResearchTime = 7f;
        private const float kCriticalSuccessBonus = 1.5f;
        private const string kResearchCriticalFail = "Botched Results";
        private const string kNeedsRepairs = "Needs repairs";
        private const string kResearchFail = "Inconclusive";
        private const string kResearchCriticalSuccess = "Great Results";
        private const string kResearchSuccess = "Good Results";

        protected float kMessageDuration = 6.5f;
        protected string botchedResultsMsg = "Botched results! This may have consequences in the future...";
        protected string greatResultsMsg = "Analysis better than expected!";
        protected string goodResultsMsg = "Good results!";
        protected string noSuccessMsg = "Analysis inconclusive";
        protected string scienceAddedMsg = "<color=lightblue>Science added: {0:f2}</b></color>";
        protected string reputationAddedMsg = "<color=yellow>Reputation added: {0:f2}</b></color>";
        protected string fundsAddedMsg = "<color=lime>Funds added: {0:f2}</b></color>";
        protected string noResearchDataMsg = "No data to transmit yet, check back later.";
        protected string researchingMsg = "Researching";
        protected string readyMsg = "Ready";
        protected string notEnoughResourcesToRepair = "Unable to repair the {0} due to insufficient resources. You need {1:f1} {2}";
        protected string infoRepairSkill = "Repair Skill:";
        protected string infoRepairResource = "Repair Resource:";

        [KSPField]
        public float sciencePerCycle;

        [KSPField]
        public float reputationPerCycle;

        [KSPField]
        public float fundsPerCycle;

        [KSPField(isPersistant = true)]
        public float scienceAdded;

        [KSPField(isPersistant = true)]
        public float reputationAdded;

        [KSPField(isPersistant = true)]
        public float fundsAdded;

        [KSPField]
        public string experimentID;

        [KSPField]
        public string repairResource;

        [KSPField]
        public float repairAmount;

        [KSPField]
        public string repairSkill;

        protected bool failedLastAttempt;
        protected float successBonus;
        protected float dataAmount;
        protected TransmitHelper transmitHelper;
        protected ScienceLabResultsView scienceLabView;

        #region Actions And Events
        [KSPEvent(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3.0f, guiName = "Review Data")]
        public virtual void ReviewData()
        {
            if (scienceAdded < 0.001 && reputationAdded < 0.001 && fundsAdded < 0.001)
            {
                ScreenMessages.PostScreenMessage(noResearchDataMsg, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            scienceLabView.scienceLab = this;
            scienceLabView.SetVisible(true);
        }

        #endregion

        #region Overrides

        public override string GetInfo()
        {
            StringBuilder moduleInfo = new StringBuilder();

            moduleInfo.Append("Minimum Crew: " + crewsRequired.ToString() + "\r\n\r\n");

            moduleInfo.Append(base.GetInfo());

            if (sciencePerCycle > 0f)
                moduleInfo.Append(string.Format(" - Science: {0:f2}\r\n", sciencePerCycle));

            if (reputationPerCycle > 0f)
                moduleInfo.Append(string.Format(" - Reputation: {0:f2}\r\n", reputationPerCycle));

            if (fundsPerCycle > 0f)
                moduleInfo.Append(string.Format(" - Funds: {0:f2}\r\n", fundsPerCycle));

            moduleInfo.Append(string.Format("<color=#7FFF00><b>Research Cycle:</b></color> \r\n - {0:f2} hours\r\n", hoursPerCycle));

            if (string.IsNullOrEmpty(repairSkill) == false)
            {
                moduleInfo.Append("<color=#7FFF00><b>Repair Skill:</b></color>\r\n");
                moduleInfo.Append(" - " + repairSkill + "\r\n");
            }

            if (string.IsNullOrEmpty(repairResource) == false)
            {
                moduleInfo.Append("<color=#7FFF00><b>Repair Resource:</b></color>\r\n");
                moduleInfo.Append(" - " + repairResource + ": " + string.Format("{0:f2}\r\n", repairAmount));
            }

            return moduleInfo.ToString();
        }

        public override void OnStart(StartState state)
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            //Setup
            scienceLabView = new ScienceLabResultsView("Science Lab");
            transmitHelper = new TransmitHelper();
            transmitHelper.part = this.part;
            transmitHelper.transmitCompleteDelegate = TransmitComplete;
            scienceLabView.part = this.part;
            scienceLabView.scienceLab = this;

            //Feedback messages
            attemptCriticalFail = kResearchCriticalFail;
            attemptCriticalSuccess = kResearchCriticalSuccess;
            attemptFail = kResearchFail;
            attemptSuccess = kResearchSuccess;

            //Repairs
            if (isBroken)
            {
                StopResourceConverter();
                status = kNeedsRepairs;
            }

            else if (isMothballed)
            {
                StopResourceConverter();
            }

            else
            {
                status = "";
            }
        }
        #endregion

        #region Helpers
        protected virtual double calculateRepairCost()
        {
            if (BARISBridge.RepairsRequireResources == false)
                return 0f;
            if (!Utils.IsExperienceEnabled())
                return 0f;

            double repairUnits = repairAmount;
            double totalAmount;
            ProtoCrewMember[] crewMembers = FlightGlobals.ActiveVessel.GetVesselCrew().ToArray();
            ProtoCrewMember crew;
            bool repairSkillFound = false;

            //Make sure we have the right skill to repair the lab.
            if (string.IsNullOrEmpty(repairSkill) == false)
            {
                for (int index = 0; index < crewMembers.Length; index++)
                {
                    crew = crewMembers[index];
                    if (crew.HasEffect(repairSkill) || WBIMainSettings.RequiresSkillCheck == false)
                    {
                        repairUnits = repairUnits * (0.9f - (crew.experienceTrait.CrewMemberExperienceLevel() * 0.1f));
                        repairSkillFound = true;
                        break;
                    }
                }

                if (!repairSkillFound)
                    return -1.0f;
            }

            //make sure the ship has enough of the resource
            totalAmount = ResourceHelper.GetTotalResourceAmount(repairResource, this.part.vessel);
            if (totalAmount < repairUnits)
                return -1.0f;

            return repairUnits;
        }

        public override double GetSecondsPerCycle()
        {
            if (totalCrewSkill == 0)
                totalCrewSkill = GetTotalCrewSkill();
            double researchTime = hoursPerCycle - (totalCrewSkill / 15.0f);

            return researchTime * 3600;
        }

        public virtual void TransmitResults()
        {
            transmitHelper.TransmitToKSC(scienceAdded, reputationAdded, fundsAdded, -1.0f, experimentID);
        }

        public virtual void TransmitComplete()
        {
            scienceAdded = 0f;
            reputationAdded = 0f;
            fundsAdded = 0f;
        }

        public virtual void TransferToScienceLab()
        {
            ModuleScienceLab lab = this.part.FindModuleImplementing<ModuleScienceLab>();

            if (lab == null)
            {
                List<ModuleScienceLab> labs = this.part.vessel.FindPartModulesImplementing<ModuleScienceLab>();

                foreach (ModuleScienceLab scienceLab in labs)
                {
                    if (scienceLab.isEnabled && scienceLab.enabled && scienceLab.part.protoModuleCrew.Count >= scienceLab.crewsRequired)
                    {
                        lab = scienceLab;
                        break;
                    }
                }
            }

            //No lab on the vessel? Then we're done.
            if (lab == null)
            {
                ScreenMessages.PostScreenMessage("Can't find an available/crewed laboratory!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                Log("No ModuleScienceLab to transfer to!");
                return;
            }

            //Transfer as much as we can to the lab.
            float availableStorage = lab.dataStorage - lab.dataStored;
            if (scienceAdded < availableStorage)
            {
                lab.dataStored += scienceAdded;
                scienceAdded = 0f;
            }

            else
            {
                lab.dataStored = lab.dataStorage;
                scienceAdded -= availableStorage;
            }
        }

        protected override void onCriticalFailure()
        {
            base.onCriticalFailure();
        }

        protected override void onCriticalSuccess()
        {
            base.onCriticalSuccess();
            successBonus = kCriticalSuccessBonus;
            addCurrency();
        }

        protected override void onFailure()
        {
            base.onFailure();
        }

        protected override void onSuccess()
        {
            base.onSuccess();
            successBonus = 1.0f;
            addCurrency();
        }

        protected virtual void addCurrency()
        {
            float successFactor = successBonus * (1.0f + (totalCrewSkill / 10.0f));
            float scienceMultiplier = HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;

            //Add science to the resource pool
            if (sciencePerCycle > 0.0f)
                scienceAdded += sciencePerCycle * successFactor * scienceMultiplier;

            //Reputation
            if (reputationPerCycle > 0.0f)
                reputationAdded += reputationPerCycle * successFactor;

            //Funds
            if (fundsPerCycle > 0.0f)
                fundsAdded += fundsPerCycle * successFactor;
        }
        #endregion
    }
}
