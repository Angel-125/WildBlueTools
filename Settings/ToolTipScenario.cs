using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using KSP.IO;


namespace WildBlueIndustries
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class ToolTipScenario : ScenarioModule
    {
        public static ToolTipScenario Instance;

        List<string> toolTipList = new List<string>();


        public void AddToolTipDisplayedFlag(string partID)
        {
            if (toolTipList.Contains(partID) == false)
                toolTipList.Add(partID);
        }


        public void ClearToolTipDisplayedFlag(string partID)
        {
            if (toolTipList.Contains(partID))
                toolTipList.Remove(partID);
        }

        public bool HasDisplayedToolTip(string partID)
        {
            return toolTipList.Contains(partID);
        }

        public override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ConfigNode tipNode;
            ConfigNode[] tips = node.GetNodes("TOOLTIP");
            for (int index = 0; index < tips.Length; index++)
            {
                tipNode = tips[index];
                toolTipList.Add(tipNode.GetValue("PartID"));
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode tipNode;
            string[] tipsToSave = toolTipList.ToArray();
            for (int index = 0; index < tipsToSave.Length; index++)
            {
                tipNode = new ConfigNode("TOOLTIP");
                tipNode.AddValue("PartID", tipsToSave[index]);
            }
        }
    }
}
