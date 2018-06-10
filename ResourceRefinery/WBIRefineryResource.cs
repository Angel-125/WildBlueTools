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

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    /// <summary>
    /// A RefineryResource defines a resource that is produced by the Refinery. This class tracks things like the current amount in storage, the max storage capacity, and how much to produce per day.
    /// </summary>
    public class WBIRefineryResource
    {
        public const string kRefineryNode = "REFINERY";

        public string kResourceName = "<color=white><b>{0}</b></color>";
        public string kResource = "<color=white>Resource: {0:s}</color>";
        public string kUnitsPerDay = "<color=white>Units Per Day: {0:f2}</color>";
        public string kCost = "<color=white>Cost Per Day: {0:n2}</color>";
        public string kStorage = "<color=white>Storage: {0:n2}/{1:n2}</color>";
        public string kMaxStorage = "<color=white>Max Storage: {0:n2}</color>";
        public string kUpgradeCost = "<color=white>Upgrade Cost: {0:n2}</color>";
        public string kNextTier = "<color=lightblue>Next Tier</color>";
        public string kProductionLevel = "<color=orange>Production Level: {0}</color>";

        /// <summary>
        /// Name of the resource
        /// </summary>
        public string resourceName = string.Empty;

        /// <summary>
        /// Specifies the tech node that must be unlocked in order to make the tier available.
        /// In the case of upgrades, it tells you what node must be unlocked before the next tier becomes available.
        /// </summary>
        public string techRequired = string.Empty;

        //How many units to produce per day. Can be zero.
        public double unitsPerDay = 0.0;

        /// <summary>
        /// Number of units to produce. Use this to limit production and avoid depeleting your bank account.
        /// </summary>
        public double unitsToProduce = 0.0f;

        /// <summary>
        /// Flag to indicate whether or not to produce an infinite number of resources- at least until your bank account is empty.
        /// </summary>
        public bool limitProduction = false;
        
        /// <summary>
        /// How much to charge for producing a unit of the resource. Total cost will be resource unitCost * unitCostMultiplier * unitsPerDay.
        /// </summary>
        public double unitCostMultiplier = 1.0;
        
        /// <summary>
        /// Maximum number of units that the Refinery can store.
        /// </summary>
        public double maxAmount = 0.0;

        /// <summary>
        /// Current number of units in storage.
        /// </summary>
        public double amount = 0.0;

        /// <summary>
        /// The cost to unlock the resource production tier.
        /// </summary>
        public float unlockCost = 0;

        /// <summary>
        /// Flag to indicate whether or not the refinery is producing the resource.
        /// </summary>
        public bool isRunning = false;

        /// <summary>
        /// ConfigNode array that specifies the production tiers. These come from REFINERY config nodes.
        /// </summary>
        public List<ConfigNode> productionTiers = new List<ConfigNode>();

        /// <summary>
        /// Cached resource definition for the refinery resource.
        /// </summary>
        public PartResourceDefinition resourceDef = null;

        /// <summary>
        /// Sends the resource to the refinery instead of recovering and selling it.
        /// </summary>
        public bool sendToRefinery = false;

        /// <summary>
        /// Current tier level. 0-based. -1 means no tiers have been unlocked.
        /// </summary>
        public int currentTier = -1;

        public WBIRefineryResource()
        {
        }

        public WBIRefineryResource(ConfigNode node)
        {
            if (node == null)
                return;
            AddTier(node);
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            if (!definitions.Contains(resourceName))
                return;
            resourceDef = definitions[resourceName];
        }

        /// <summary>
        /// Returns production in terms of units per second
        /// </summary>
        public double UnitsPerSec
        {
            get
            {
                double unitsPerSec = unitsPerDay / WBIRefinery.SecondsPerDay;
                if (!isRunning)
                    unitsPerSec = 0f;
                return unitsPerSec;
            }
        }

        /// <summary>
        /// Returns the cost to produce the resource in Funds per second.
        /// </summary>
        public double CostPerSec
        {
            get
            {
                double costPerSec = (unitsPerDay / WBIRefinery.SecondsPerDay) * resourceDef.unitCost * unitCostMultiplier;
                if (!isRunning)
                    costPerSec = 0f;
                return costPerSec;
            }
        }

        /// <summary>
        /// Static method to load REFINERY config nodes and create the refinery resource map.
        /// </summary>
        /// <returns>A Dictionary containing the resource names (key) refinery resources (value)</returns>
        public static Dictionary<string, WBIRefineryResource> LoadRefineryResources()
        {
            Dictionary<string, WBIRefineryResource> refineryResourceMap = new Dictionary<string, WBIRefineryResource>();
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes(kRefineryNode);
            if (nodes == null || nodes.Length == 0)
                return refineryResourceMap;
            ConfigNode node;
            string resourceName;
            WBIRefineryResource refineryResource;

            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("resourceName"))
                {
                    //Get the refinery resource
                    resourceName = node.GetValue("resourceName");
                    if (!refineryResourceMap.ContainsKey(resourceName))
                    {
                        refineryResourceMap.Add(resourceName, new WBIRefineryResource(node));
                    }
                    else
                    {
                        refineryResource = refineryResourceMap[resourceName];

                        //Load the node
                        refineryResource.AddTier(node);
                    }
                }
            }

            return refineryResourceMap;
        }

        /// <summary>
        /// Loads the config node values into the resource.
        /// </summary>
        /// <param name="node"></param>
        public void Load(ConfigNode node)
        {
            if (node.HasValue("resourceName"))
                resourceName = node.GetValue("resourceName");
            else
                return;

            if (node.HasValue("techRequired"))
                techRequired = node.GetValue("techRequired");

            if (node.HasValue("currentTier"))
                int.TryParse(node.GetValue("currentTier"), out currentTier);

            if (node.HasValue("unitsPerDay"))
                double.TryParse(node.GetValue("unitsPerDay"), out unitsPerDay);

            if (node.HasValue("unitCostMultiplier"))
                double.TryParse(node.GetValue("unitCostMultiplier"), out unitCostMultiplier);

            if (node.HasValue("maxAmount"))
                double.TryParse(node.GetValue("maxAmount"), out maxAmount);

            if (node.HasValue("amount"))
                double.TryParse(node.GetValue("amount"), out amount);

            if (node.HasValue("unlockCost"))
                float.TryParse(node.GetValue("unlockCost"), out unlockCost);

            if (node.HasValue("isRunning"))
                bool.TryParse(node.GetValue("isRunning"), out isRunning);

            if (node.HasValue("unitsToProduce"))
                double.TryParse(node.GetValue("unitsToProduce"), out unitsToProduce);

            if (node.HasValue("infiniteProduction"))
                bool.TryParse(node.GetValue("infiniteProduction"), out isRunning);

            if (node.HasValue("sendToRefinery"))
                bool.TryParse(node.GetValue("sendToRefinery"), out sendToRefinery);
        }

        /// <summary>
        /// Saves the current values to a config node.
        /// </summary>
        /// <returns></returns>
        public ConfigNode Save()
        {
            ConfigNode node = new ConfigNode(kRefineryNode);

            node.AddValue("resourceName", resourceName);
            if (!string.IsNullOrEmpty(techRequired))
                node.AddValue("techRequired", techRequired);
            node.AddValue("currentTier", currentTier);
            node.AddValue("unitsPerDay", unitsPerDay);
            node.AddValue("unitCostMultiplier", unitCostMultiplier);
            node.AddValue("maxAmount", maxAmount);
            node.AddValue("amount", amount);
            node.AddValue("unlockCost", unlockCost);
            node.AddValue("isRunning", isRunning);
            node.AddValue("unitsToProduce", unitsToProduce);
            node.AddValue("limitProduction", limitProduction);
            node.AddValue("sendToRefinery", sendToRefinery);

            return node;
        }

        /// <summary>
        /// Adds a new tier node. if the refinery resource hasn't been initialized then the node's stats will be used to initialize the resource.
        /// </summary>
        /// <param name="node"></param>
        public void AddTier(ConfigNode node)
        {
            productionTiers.Add(node);

            if (string.IsNullOrEmpty(resourceName))
                Load(node);
        }

        /// <summary>
        /// Returns the config node for the next production tier.
        /// </summary>
        /// <returns>A ConfigNode containing the next tier or null if there are no more tiers.</returns>
        public ConfigNode GetNextTier()
        {
            int nextTier = currentTier + 1;

            if (nextTier < productionTiers.Count)
                return productionTiers[nextTier];
            else
                return null;
        }

        /// <summary>
        /// Determines whether or not the refinery resource is at max tier.
        /// </summary>
        /// <returns>True if the tier is at max, false if not</returns>
        public bool IsMaxTier()
        {
            return currentTier == productionTiers.Count - 1 ? true : false;
        }

        /// <summary>
        /// Determines whether or not the production tier's required tech node is unlocked
        /// </summary>
        /// <returns>True if the node is unlocked, false if not.</returns>
        public bool IsTechUnlocked()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return true;

            //Have we unlocked the tech node?
            if (!string.IsNullOrEmpty(techRequired))
            {
                if (ResearchAndDevelopment.GetTechnologyState(techRequired) == RDTech.State.Unavailable)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether or not the refinery resource is unlocked.
        /// </summary>
        public bool IsUnlocked
        {
            get
            {
                return currentTier != -1 ? true : false;
            }
        }

        /// <summary>
        /// Determines whether or not the player can afford to unlock the node
        /// </summary>
        /// <returns>True if the player can afford the upgrade, false if not.</returns>
        public bool CanAffordUnlock()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return true;
            else
                return Funding.CanAfford(unlockCost);
        }

        /// <summary>
        /// Upgrades the tier to the next level.
        /// </summary>
        public void UnlockNextTier()
        {
            int tierCount = productionTiers.Count;

            //Pay for the upgrade
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                if (IsTechUnlocked() && Funding.CanAfford(unlockCost))
                {
                    Funding.Instance.AddFunds(-unlockCost, TransactionReasons.Vessels);
                }
                else
                {
                    ScreenMessages.PostScreenMessage(WBIRefinery.kCannotAffordUpgrade, WBIRefinery.kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }

            //Now get the next tier and load it.
            if (!IsMaxTier())
            {
                currentTier += 1;
                Load(productionTiers[currentTier]);
            }
        }

        /// <summary>
        /// Upgrades the production tier to the maximum level. Typically used in non-career game mode.
        /// </summary>
        public void UpgradeToMaxTier()
        {
            currentTier = productionTiers.Count - 1;
            Load(productionTiers[currentTier]);
        }

        /// <summary>
        /// Updates the production of the resource.
        /// </summary>
        public void UpdateProduction(double elapsedTime)
        {
            if (!isRunning)
                return;
            if (resourceDef == null)
                return;
            double unitsPerFrame = (unitsPerDay / WBIRefinery.SecondsPerDay) * elapsedTime;
            double resourceCost;

            //Make sure we have enough room
            if (unitsPerFrame + amount > maxAmount)
                unitsPerFrame = maxAmount - amount;

            //Add the produced units if we can afford them
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                resourceCost = resourceDef.unitCost * unitCostMultiplier * unitsPerFrame;
                if (Funding.CanAfford((float)resourceCost))
                {
                    Funding.Instance.AddFunds(-resourceCost, TransactionReasons.Vessels);
                    amount += unitsPerFrame;
                }

                //Can't afford it so stop production
                else
                {
                    isRunning = false;
                }
            }

            //Just add the units
            else
            {
                amount += unitsPerFrame;
            }

            //If we're full then we're done.
            if (amount >= maxAmount)
                isRunning = false;

            //If we have a limited production run then record what we've produced.
            if (limitProduction)
            {
                unitsToProduce -= unitsPerFrame;
                if (unitsToProduce <= 0)
                {
                    unitsToProduce = 0;
                    isRunning = false;
                }
            }
        }
    }
}
