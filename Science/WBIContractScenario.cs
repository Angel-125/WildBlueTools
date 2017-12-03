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

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("contractsAvailable"))
                contractsAvailable = int.Parse(node.GetValue("contractsAvailable"));

            contractCounts.Clear();
            ConfigNode[] contractCountNodes = node.GetNodes("CONTRACT_COUNT");
            foreach (ConfigNode contractCountNode in contractCountNodes)
            {
                contractCounts.Add(node.GetValue("name"), int.Parse(node.GetValue("count")));
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
        }
    }
}
