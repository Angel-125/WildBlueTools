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
    [KSPModule("Resource Converter")]
    public class WBIResourceConverter : ModuleBreakableConverter
    {
        private const float kminimumSuccess = 80f;
        private const float kCriticalSuccess = 95f;
        private const float kCriticalFailure = 33f;
        private const float kDefaultHoursPerCycle = 1.0f;

        //Result messages for lastAttempt
        protected string attemptCriticalFail = "Critical Failure";
        protected string attemptCriticalSuccess = "Critical Success";
        protected string attemptFail = "Fail";
        protected string attemptSuccess = "Success";
        protected string requiredResource = "Requires ";
        protected string needCrew = "Missing {0} Crew";

        public static bool showResults = true;

        [KSPField]
        public int crewsRequired = 0;

        [KSPField]
        public bool checkCrewsWholeVessel;

        [KSPField]
        public float minimumSuccess;

        [KSPField]
        public float criticalSuccess;

        [KSPField]
        public float criticalFail;

        [KSPField]
        public double hoursPerCycle;

        [KSPField(isPersistant = true)]
        public double cycleStartTime;

        [KSPField(guiActive = true, guiName = "Progress", isPersistant = true)]
        public string progress = string.Empty;

        [KSPField(guiActive = true, guiName = "Last Attempt", isPersistant = true)]
        public string lastAttempt = string.Empty;

        [KSPField(isPersistant = true)]
        public bool showGUI = true;

        public double elapsedTime;
        protected float totalCrewSkill = -1.0f;
        protected double secondsPerCycle = 0f;

        public string GetMissingRequiredResource()
        {
            PartResourceDefinition definition;
            Dictionary<string, PartResource> resourceMap = new Dictionary<string, PartResource>();

            foreach (PartResource res in this.part.Resources)
            {
                resourceMap.Add(res.resourceName, res);
            }

            //If we have required resources, make sure we have them.
            if (reqList.Count > 0)
            {
                foreach (ResourceRatio resRatio in reqList)
                {
                    //Do we have a definition?
                    definition = ResourceHelper.DefinitionForResource(resRatio.ResourceName);
                    if (definition == null)
                    {
                        return resRatio.ResourceName;
                    }

                    //Do we have the resource aboard?
                    if (resourceMap.ContainsKey(resRatio.ResourceName) == false)
                    {
                        return resRatio.ResourceName;
                    }

                    //Do we have enough?
                    if (resourceMap[resRatio.ResourceName].amount < resRatio.Ratio)
                    {
                        return resRatio.ResourceName;
                    }
                }
            }

            return null;
        }

        public override void StartResourceConverter()
        {
            string absentResource = GetMissingRequiredResource();

            //Do we have enough crew?
            if (hasMinimumCrew() == false)
            {
                return;
            }

            //If we have required resources, make sure we have them.
            if (!string.IsNullOrEmpty(absentResource))
            {
                status = requiredResource + absentResource;
                StopResourceConverter();
                return;
            }

            base.StartResourceConverter();
            cycleStartTime = Planetarium.GetUniversalTime();
            lastUpdateTime = cycleStartTime;
            elapsedTime = 0.0f;
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
            progress = "None";
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Setup
            progress = "None";
            if (hoursPerCycle == 0f)
                hoursPerCycle = kDefaultHoursPerCycle;

            if (minimumSuccess == 0)
                minimumSuccess = kminimumSuccess;
            if (criticalSuccess == 0)
                criticalSuccess = kCriticalSuccess;
            if (criticalFail == 0)
                criticalFail = kCriticalFailure;

            //Check minimum crew
            hasMinimumCrew();
        }

        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            bool missingResources = false;
            Events["StartResourceConverter"].guiActive = false;
            Events["StopResourceConverter"].guiActive = false;

            if (FlightGlobals.ready == false)
                return;
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            if (ModuleIsActive() == false)
            {
                return;
            }
            if (this.part.vessel.IsControllable == false)
            {
                StopResourceConverter();
                return;
            }
            if (hoursPerCycle == 0f)
                return;

            //If w're missing required resources, then we're done
            string missingRequiredResource = GetMissingRequiredResource();
            if (!string.IsNullOrEmpty(missingRequiredResource))
            {
                status = requiredResource + missingRequiredResource;
                StopResourceConverter();
                return;
            }

            //Make sure we have the minimum crew
            if (hasMinimumCrew() == false)
                return;

            //Now run the base converter stuff
            base.PostProcess(result, deltaTime);

            if (cycleStartTime == 0f)
            {
                cycleStartTime = Planetarium.GetUniversalTime();
                lastUpdateTime = cycleStartTime;
                elapsedTime = 0.0f;
                return;
            }

            //Calculate the crew skill and seconds of research per cycle.
            //Thes values can change if the player swaps out crew.
            totalCrewSkill = GetTotalCrewSkill();
            secondsPerCycle = GetSecondsPerCycle();

            //If we're missing resources then we're done.
            if (!string.IsNullOrEmpty(result.Status))
            {
                if (result.Status.ToLower().Contains("missing"))
                {
                    status = result.Status;
                    missingResources = true;
                    return;
                }
            }

            //Calculate elapsed time
            elapsedTime = Planetarium.GetUniversalTime() - cycleStartTime;

            //Calculate progress
            CalculateProgress();

            //If we've elapsed time cycle then perform the analyis.
            float completionRatio = (float)(elapsedTime / secondsPerCycle);
            if (completionRatio > 1.0f && !missingResources)
            {
                int cyclesSinceLastUpdate = Mathf.RoundToInt(completionRatio);
                int currentCycle;
                for (currentCycle = 0; currentCycle < cyclesSinceLastUpdate; currentCycle++)
                {
                    PerformAnalysis();

                    //Reset start time
                    cycleStartTime = Planetarium.GetUniversalTime();
                }
            }

        }

        public override void SetGuiVisible(bool isVisible)
        {
            base.SetGuiVisible(isVisible);

            Fields["lastAttempt"].guiActive = isVisible;
            Fields["lastAttempt"].guiActiveEditor = isVisible;
            Fields["progress"].guiActive = isVisible;
            Fields["progress"].guiActiveEditor = isVisible;
            Fields["status"].guiActive = isVisible;
        }

        public virtual void CalculateProgress()
        {
            //Get elapsed time (seconds)
            progress = string.Format("{0:f1}%", ((elapsedTime / secondsPerCycle) * 100));
        }

        public virtual float GetTotalCrewSkill()
        {
            float totalSkillPoints = 0f;

            if (this.part.CrewCapacity == 0)
                return 0f;
            if (Utils.IsExperienceEnabled() == false)
                return 0f;

            ProtoCrewMember[] crewMembers = this.part.protoModuleCrew.ToArray();
            ProtoCrewMember crewMember; 

            for (int index = 0; index < crewMembers.Length; index++)
            {
                crewMember = crewMembers[index];
                if (crewMember.HasEffect(ExperienceEffect))
                    totalSkillPoints += crewMember.experienceTrait.CrewMemberExperienceLevel();
            }

            return totalSkillPoints;
        }

        public virtual double GetSecondsPerCycle()
        {
            return hoursPerCycle * 3600;
        }

        public virtual void PerformAnalysis()
        {
            float analysisRoll = performAnalysisRoll();

            if (analysisRoll <= criticalFail)
                onCriticalFailure();

            else if (analysisRoll >= criticalSuccess)
                onCriticalSuccess();

            else if (analysisRoll >= minimumSuccess)
                onSuccess();

            else
                onFailure();

        }

        protected virtual float performAnalysisRoll()
        {
            float roll = 0.0f;

            //Roll 3d6 to approximate a bell curve, then convert it to a value between 1 and 100.
            roll = UnityEngine.Random.Range(1, 6);
            roll += UnityEngine.Random.Range(1, 6);
            roll += UnityEngine.Random.Range(1, 6);
            roll *= 5.5556f;

            //Factor in crew
            roll += totalCrewSkill;

            //Done
            return roll;
        }

        protected virtual bool hasMinimumCrew()
        {
            int totalCrew = 0;
            int crewCount = 0;
            ProtoCrewMember astronaut;
            List<ProtoCrewMember> astronauts;

            //Do we have enough crew?
            if (crewsRequired > 0)
            {
                if (checkCrewsWholeVessel)
                {
                    totalCrew = this.part.vessel.GetCrewCount();
                    astronauts = this.part.vessel.GetVesselCrew();
                }
                else
                {
                    totalCrew = this.part.protoModuleCrew.Count;
                    astronauts = this.part.protoModuleCrew;
                }

                if (!string.IsNullOrEmpty(ExperienceEffect))
                {
                    for (int index = 0; index < totalCrew; index++)
                    {
                        astronaut = astronauts[index];
                        if (astronaut.HasEffect(ExperienceEffect))
                            crewCount += 1;
                    }
                }
                else
                {
                    crewCount = totalCrew;
                }

                if (crewsRequired > crewCount)
                {
                    status = string.Format(needCrew, crewsRequired - crewCount);
                    return false;
                }
            }

            return true;
        }

        protected virtual void onCriticalFailure()
        {
            lastAttempt = attemptCriticalFail;
            qualityControl.DeclarePartBroken();
        }

        protected virtual void onCriticalSuccess()
        {
            lastAttempt = attemptCriticalSuccess;
        }

        protected virtual void onFailure()
        {
            lastAttempt = attemptFail;
        }

        protected virtual void onSuccess()
        {
            lastAttempt = attemptSuccess;
        }

        public virtual void Log(object message)
        {
            Debug.Log(this.ClassName + " [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: " + message);
        }
    }
}
