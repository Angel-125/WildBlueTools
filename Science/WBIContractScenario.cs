using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContractsPlus.Contracts
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class WBIContractScenario : ScenarioModule
    {
        public const int maxContracts = 4;
        public static WBIContractScenario Instance;
        public int contractsAvailable;

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
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("contractsAvailable", contractsAvailable);
        }
    }
}
