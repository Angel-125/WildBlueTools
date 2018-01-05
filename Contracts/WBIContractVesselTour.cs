using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;
using Contracts.Parameters;
using KSP;
using KSPAchievements;
using ContractsPlus.Contracts;
using FinePrint.Contracts.Parameters;

namespace ContractsPlus.Contracts
{
    public class WBIContractVesselTour : Contract
    {
        public const int CurrentContractVersion = 1;

        protected string ContractType = "WBIContractVesselTour";
        protected string ContractTitle = "Ferry {0} tourists to {1}";
        protected string ContractTitleSingle = "Ferry a tourist to {0}";
        protected string SynopsisMany = "Ferry {0} tourists to {1}";
        protected string SynopsisSingle = "Ferry a tourist to {0}";
        protected string ContractCompleteMsg = "{0} tourists successfully delivered to {1}";
        protected string DescSingle = "{0} would like to visit {1} for several days and then return home safely. It would be the experience of a lifetime!";
        protected string DescMany = "A group of tourists would like to visit {0} for several days and then return home safely. It would be the experience of a lifetime!";
        protected string DescNote = "\r\n\r\nNOTE: Tourists cannot EVA, so you'll need a way to connect the transport craft to {0}.";
        protected string ReturnHomeDesc = "Bring {0} home";

        protected float fundsAdvance = 15000f;
        protected float fundsCompleteBase = 15000f;
        protected float fundsFerryComplete = 5000f;
        protected float fundsStayComplete = 1000f;
        protected float fundsFailure = 35000f;
        protected float repComplete = 10f;
        protected float repFailure = 10f;
        protected float rewardAdjustmentFactor = 0.4f;
        protected int MaxTourists = 6;
        protected int minimumDays = 2;
        protected int maximumDays = 20;

        protected CelestialBody targetBody = null;
        protected int versionNumber = 1;
        protected string contractID = string.Empty;
        protected int totalTourists = 0;
        protected string vesselName = "None";
        protected List<Vessel> destinationCandidates = new List<Vessel>();
        protected int totalDays;
        protected ProtoCrewMember tourist;
        protected List<string> kerbalNames = new List<string>();

        protected void Log(string message)
        {
            if (WildBlueIndustries.WBIMainSettings.EnableDebugLogging)
            {
                Debug.Log("[" + ContractType + "] - " + message);
            }
        }

        protected override bool Generate()
        {
            int contractCount = WBIContractScenario.Instance.GetContractCount(ContractType);
            Log("Trying to generate a " + ContractType + ", count: " + contractCount + "/" + WBIContractScenario.maxContracts);
            if (contractCount == WBIContractScenario.maxContracts)
                return false;

            //Find destination candidates
            if (destinationCandidates.Count == 0)
                getDestinationCandidates();
            if (destinationCandidates.Count == 0) 
                return false;

            //Determine which candidate to use
            int candidateID = UnityEngine.Random.Range(0, destinationCandidates.Count);
            Vessel targetVessel = destinationCandidates[candidateID];
            vesselName = targetVessel.vesselName;
            targetBody = targetVessel.mainBody;
            Log("Target vessel: " + vesselName);
            Log("Target body: " + targetBody);

            bool isOrbiting = false;
            if (targetVessel.situation == Vessel.Situations.ORBITING)
                isOrbiting = true;

            //Generate number of tourists
            totalTourists = UnityEngine.Random.Range(1, MaxTourists) * ((int)prestige + 1);
            Log("totalTourists: " + totalTourists);

            //Generate total days
            totalDays = UnityEngine.Random.Range(minimumDays, maximumDays) * ((int)prestige + 1);
            Log("totalDays: " + totalDays);

            //Calculate completion funds
            float deliveryFunds;
            float stayFunds;
            float totalFunds;
            if (!isOrbiting)
            {
                deliveryFunds = fundsFerryComplete * targetBody.scienceValues.LandedDataValue;
                stayFunds = fundsStayComplete * (float)totalDays * targetBody.scienceValues.LandedDataValue;
                totalFunds = fundsCompleteBase * targetBody.scienceValues.LandedDataValue;
            }
            else
            {
                deliveryFunds = fundsFerryComplete * targetBody.scienceValues.InSpaceLowDataValue;
                stayFunds = fundsStayComplete * (float)totalDays * targetBody.scienceValues.InSpaceLowDataValue;
                totalFunds = fundsCompleteBase * targetBody.scienceValues.InSpaceLowDataValue;
            }
            stayFunds *= ((float)prestige + 1.0f);
            totalFunds *= ((float)prestige + 1.0f);

            //Be in command of <targetVessel> parameter
            SpecificVesselParameter specificVesselParam = new SpecificVesselParameter(targetVessel);
            this.AddParameter(specificVesselParam, null);

            //Generate kerbals
            WBIFerryKerbalParam ferryParameter;
            WBIKerbalStayParam stayParameter;
            WBIReturnKerbalHome returnHomeParam;
            string himHer;
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
            kerbalNames.Clear();
            for (int index = 0; index < totalTourists; index++)
            {
                tourist = createTourist();

                //Stay at vessel parameter
                stayParameter = new WBIKerbalStayParam(vesselName, tourist.name, totalDays);
                this.AddParameter(stayParameter, null); //Do this before setting other things in the parameter
                stayParameter.SetFunds(stayFunds, targetBody);

                //Ferry to vessel parameter
                ferryParameter = new WBIFerryKerbalParam(vesselName, tourist.name);
                stayParameter.AddParameter(ferryParameter, null);
                ferryParameter.SetFunds(deliveryFunds, targetBody);

                //Return safely parameter
                himHer = tourist.gender == ProtoCrewMember.Gender.Male ? "him" : "her";
                returnHomeParam = new WBIReturnKerbalHome(string.Format(ReturnHomeDesc, himHer), tourist.name);
                stayParameter.AddParameter(returnHomeParam);

                //Record funds
                totalFunds += stayFunds + deliveryFunds;

                //Clean up the roster- we only generate tourists when the contract is accepted.
                kerbalNames.Add(tourist.name);
                roster.Remove(tourist.name);
            }

            //Set rewards
            base.SetExpiry();
            base.SetDeadlineYears(10f, targetBody);
            base.SetReputation(repComplete, repFailure, targetBody);
            base.SetFunds(fundsAdvance, totalFunds, totalFunds * 0.25f, targetBody);

            //Record contract
            contractCount += 1;
            if (contractCount > WBIContractScenario.maxContracts)
                contractCount = WBIContractScenario.maxContracts;
            WBIContractScenario.Instance.SetContractCount(ContractType, contractCount);

            //Done
            if (string.IsNullOrEmpty(contractID))
                contractID = Guid.NewGuid().ToString();
            return true;
        }

        public override bool CanBeCancelled()
        {
            return true;
        }

        public override bool CanBeDeclined()
        {
            return true;
        }

        protected override string GetHashString()
        {
            if (string.IsNullOrEmpty(contractID))
                contractID = Guid.NewGuid().ToString();

            return contractID;
        }

        protected override string GetTitle()
        {
            if (totalTourists > 1)
                return string.Format(ContractTitle, totalTourists, vesselName);
            else
                return string.Format(ContractTitleSingle, vesselName);
        }

        protected override string GetDescription()
        {
            if (totalTourists > 1)
            {
                return string.Format(DescMany, vesselName);
            }
            else
            {
                return string.Format(DescSingle, tourist.name, vesselName);
            }
        }

        protected override string GetSynopsys()
        {
            if (totalTourists > 1)
                return string.Format(SynopsisMany, totalTourists, vesselName);
            else
                return string.Format(SynopsisSingle, vesselName);
        }

        protected override string MessageCompleted()
        {
            return string.Format(ContractCompleteMsg, totalTourists, vesselName);
        }

        protected override void OnLoad(ConfigNode node)
        {
            contractID = node.GetValue("contractID");
            if (int.TryParse("versionNumber", out versionNumber) == false)
                versionNumber = CurrentContractVersion;

            int bodyID = int.Parse(node.GetValue("targetBody"));
            foreach (var body in FlightGlobals.Bodies)
            {
                if (body.flightGlobalsIndex == bodyID)
                    targetBody = body;
            }
            totalTourists = int.Parse(node.GetValue("totalTourists"));
            vesselName = node.GetValue("vesselName");
            totalDays = int.Parse(node.GetValue("totalDays"));

            ConfigNode[] touristNodes = node.GetNodes("TOURIST");
            kerbalNames.Clear();
            foreach (ConfigNode touristNode in touristNodes)
            {
                kerbalNames.Add(touristNode.GetValue("name"));
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("contractID", contractID);
            node.AddValue("versionNumber", CurrentContractVersion);

            int bodyID = targetBody.flightGlobalsIndex;
            node.AddValue("targetBody", bodyID);
            node.AddValue("totalTourists", totalTourists);
            node.AddValue("vesselName", vesselName);
            node.AddValue("totalDays", totalDays);

            ConfigNode touristNode;
            foreach (string kerbalName in kerbalNames)
            {
                touristNode = new ConfigNode("TOURIST");
                touristNode.AddValue("name", kerbalName);
                node.AddNode(touristNode);
            }
        }

        protected override void OnParameterStateChange(ContractParameter p)
        {
            base.OnParameterStateChange(p);

            foreach (ContractParameter parameter in AllParameters)
            {
                if (parameter.State == ParameterState.Incomplete || parameter.State == ParameterState.Failed)
                {
                    return;
                }
            }

            //All parameters are complete
            SetState(State.Completed);
        }

        protected void decrementContractCount()
        {
            int contractCount = WBIContractScenario.Instance.GetContractCount(ContractType) - 1;
            if (contractCount < 0)
                contractCount = 0;
            WBIContractScenario.Instance.SetContractCount(ContractType, contractCount);
        }

        protected override void OnAccepted()
        {
            base.OnAccepted();

            foreach (string kerbalName in kerbalNames)
            {
                createTourist(kerbalName);
            }
        }

        protected override void OnCompleted()
        {
            base.OnCompleted();
            decrementContractCount();
            removeTourists();
        }

        protected override void OnFailed()
        {
            base.OnFailed();
            decrementContractCount();
            removeTourists();
        }

        protected override void OnFinished()
        {
            base.OnFinished();
            decrementContractCount();
            removeTourists();
        }

        protected override void OnDeclined()
        {
            base.OnDeclined();
            decrementContractCount();
            removeTourists();
        }

        protected override void OnOfferExpired()
        {
            base.OnOfferExpired();
            decrementContractCount();
            removeTourists();
        }

        protected override void OnCancelled()
        {
            base.OnCancelled();
            decrementContractCount();
            removeTourists();
        }

        public override bool MeetRequirements()
        {
            int contractCount = WBIContractScenario.Instance.GetContractCount(ContractType);
            if (contractCount == WBIContractScenario.maxContracts)
                return false;
            else
                return true;
            /*
            Log("Checking for requirements...");
            //Is there a vessel that has one of the required parts?
            getDestinationCandidates();
            if (destinationCandidates.Count > 0)
                return true;
            else
                return false;
             */
        }

        protected void removeTourists()
        {
            WBIKerbalStayParam stayParam;
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            foreach (ContractParameter parameter in AllParameters)
            {
                if (parameter is WBIKerbalStayParam)
                {
                    stayParam = (WBIKerbalStayParam)parameter;
                    if (roster[stayParam.kerbalName] != null)
                    {
                        //Remove them if they haven't flown yet.
                        if (roster[stayParam.kerbalName].rosterStatus == ProtoCrewMember.RosterStatus.Available)
                            roster.Remove(stayParam.kerbalName);

                        //Remove them when they recover
                        else
                            WBIContractScenario.Instance.registerKerbal(stayParam.kerbalName);
                    }
                }
            }
        }

        protected virtual void getDestinationCandidates()
        {
            Log("Looking for destination candidates");
            destinationCandidates.Clear();

            //Loaded vessels
            int vesselCount = FlightGlobals.VesselsLoaded.Count;
            Vessel vessel;
            for (int index = 0; index < vesselCount; index++)
            {
                vessel = FlightGlobals.VesselsLoaded[index];
                if (vessel.vesselType == VesselType.Debris || vessel.vesselType == VesselType.Flag || vessel.vesselType == VesselType.SpaceObject || vessel.vesselType == VesselType.Unknown)
                    continue;
                if (vessel.mainBody.isHomeWorld && prestige != ContractPrestige.Trivial)
                    continue;
                if (vessel.GetCrewCapacity() >= MaxTourists)
                    destinationCandidates.Add(vessel);
            }

            //Unloaded vessels
            vesselCount = FlightGlobals.VesselsUnloaded.Count;
            for (int index = 0; index < vesselCount; index++)
            {
                vessel = FlightGlobals.VesselsUnloaded[index];
                if (vessel.vesselType == VesselType.Debris || vessel.vesselType == VesselType.Flag || vessel.vesselType == VesselType.SpaceObject || vessel.vesselType == VesselType.Unknown)
                    continue;
                if (vessel.mainBody.isHomeWorld && prestige != ContractPrestige.Trivial)
                    continue;
                if (vessel.protoVessel.crewableParts >= 1)
                    destinationCandidates.Add(vessel);
            }

            //Did we find any?
            if (destinationCandidates.Count > 0)
            {
                if (WildBlueIndustries.WBIMainSettings.EnableDebugLogging)
                {
                    vesselCount = destinationCandidates.Count;
                    Log("Found " + vesselCount + " destination candidates");
                    for (int index = 0; index < vesselCount; index++)
                        Log("Destination candidate: " + destinationCandidates[index].vesselName);
                }
            }
            else
            {
                Log("No candidates found");
            }

        }

        protected ProtoCrewMember createTourist(string kerbalName = null)
        {
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
            string message = string.Empty;
            ProtoCrewMember newRecruit = roster.GetNewKerbal(ProtoCrewMember.KerbalType.Tourist);
            if (!string.IsNullOrEmpty(kerbalName))
                newRecruit.ChangeName(kerbalName);
            Log("Created new tourist: " + newRecruit.name);

            newRecruit.rosterStatus = ProtoCrewMember.RosterStatus.Available;

            //Game events
            newRecruit.UpdateExperience();
            roster.Update(Planetarium.GetUniversalTime());
            GameEvents.onKerbalAdded.Fire(newRecruit);

            return newRecruit;
        }
    }
}
