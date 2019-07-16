using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractsPlus.Contracts
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class WBIContractScenario : ScenarioModule
    {
        public const int maxContracts = 6;
        public static WBIContractScenario Instance;
        public int contractsAvailable;
        public Dictionary<string, int> contractCounts = new Dictionary<string, int>();
        public List<string> kerbals = new List<string>();

        public int GetContractCount(string contractType)
        {
            if (contractCounts.ContainsKey(contractType) == false)
            {
                contractCounts.Add(contractType, 0);
                return 0;
            }

            return contractCounts[contractType];
        }

        public void SetContractCount(string contractType, int count)
        {
            if (contractCounts.ContainsKey(contractType) == false)
            {
                contractCounts.Add(contractType, count);
                return;
            }

            contractCounts[contractType] = count;
        }

        public override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
        }

        public void Start()
        {
            GameEvents.onVesselDestroy.Add(removekerbals);
            GameEvents.onVesselRecovered.Add(removekerbals);
            GameEvents.onVesselTerminated.Add(removekerbals);
        }

        public void OnDestroy()
        {
            GameEvents.onVesselDestroy.Remove(removekerbals);
            GameEvents.onVesselRecovered.Remove(removekerbals);
            GameEvents.onVesselTerminated.Remove(removekerbals);
        }

        protected void removekerbals(ProtoVessel protoVessel)
        {
            List<ProtoCrewMember> crewMembers = protoVessel.GetVesselCrew();
            removekerbals(crewMembers);
        }

        protected void removekerbals(Vessel vessel)
        {
            List<ProtoCrewMember> crewMembers = vessel.GetVesselCrew();
            removekerbals(crewMembers);
        }

        protected void removekerbals(ProtoVessel protoVessel, bool someBool)
        {
            List<ProtoCrewMember> crewMembers = protoVessel.GetVesselCrew();
            removekerbals(crewMembers);
        }

        public void removekerbals(List<ProtoCrewMember> crewMembers)
        {
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

            //Remove all of the vessel's registered crew members.
            foreach (ProtoCrewMember crewMember in crewMembers)
            {
                if (kerbals.Contains(crewMember.name) && roster[crewMember.name] != null)
                {
                    roster.Remove(crewMember.name);
                    kerbals.Remove(crewMember.name);
                }
            }

            //Also clean up the list
            List<string> doomed = new List<string>();
            foreach (string kerbalName in kerbals)
            {
                if (roster[kerbalName] == null)
                    doomed.Add(kerbalName);
            }
            foreach (string doomedKerbal in doomed)
                kerbals.Remove(doomedKerbal);
        }

        public void registerKerbal(ProtoCrewMember kerbal)
        {
            registerKerbal(kerbal.name);
        }

        public void registerKerbal(string kerbalName)
        {
            if (kerbals.Contains(kerbalName) == false)
                kerbals.Add(kerbalName);
        }

        public void unregisterKerbal(string kerbalName)
        {
            if (kerbals.Contains(kerbalName))
                kerbals.Remove(kerbalName);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("contractsAvailable"))
                contractsAvailable = int.Parse(node.GetValue("contractsAvailable"));

            contractCounts.Clear();
            ConfigNode[] contractCountNodes = node.GetNodes("CONTRACT_COUNT");
            foreach (ConfigNode contractCountNode in contractCountNodes)
            {
                contractCounts.Add(contractCountNode.GetValue("name"), int.Parse(contractCountNode.GetValue("count")));
            }

            ConfigNode[] crewNodes = node.GetNodes("CREW");
            foreach (ConfigNode crewNode in crewNodes)
            {
                kerbals.Add(crewNode.GetValue("name"));
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("contractsAvailable", contractsAvailable);

            ConfigNode contractCountNode;
            foreach (string key in contractCounts.Keys)
            {
                contractCountNode = new ConfigNode("CONTRACT_COUNT");
                contractCountNode.AddValue("name", key);
                contractCountNode.AddValue("count", contractCounts[key]);
                node.AddNode(contractCountNode);
            }

            ConfigNode crewNode;
            foreach (string crewName in kerbals)
            {
                crewNode = new ConfigNode("CREW");
                crewNode.AddValue("name", crewName);
                node.AddNode(crewNode);
            }
        }
    }
}
