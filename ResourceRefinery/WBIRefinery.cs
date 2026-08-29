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
    /// This class defines a Refinery that produces one or more resources for spacecraft to purchase and use. If a resource is defined as a refinery resource,
    /// then it cannot be purchased from within the editor. Players must wait until vessel launch to purchase the resource. Resources are defined using a
    /// REFINERY config node.
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class WBIRefinery : ScenarioModule
    {
        #region PlayerMessages
        public static float kMessageDuration = 12.0f;
        public static string kCannotAffordUpgrade = "You cannot afford to upgrade production of {0}";
        public static string kRefineryResourcesRemoved = "Resources produced by the Refinery were removed; they must be purchased after launch from the Space Center.";
        public static string kRefineryDescription = "<color=lightgreen>At the Refinery, our workers toil away in mostly safe conditions to produce resources vital to the space program. We can store resources produced at the Refinery as well as those from recovered spacecraft. Once a craft is prepared for launch, the Refinery can load desired resources as well.</color>";
        public static string kUpgradeButton = "<b>Upgrade\n<color=#ffa500ff>£{0:n2}</color></b>";
        public static string kPurchaseButton = "<b>Purchase\n<color=#ffa500ff>£{0:n2}</color></b>";
        public static string kFullyUpgraded = "<color=lightblue><b>Fully Upgraded</b></color>";
        public static string kInsufficientFunds = "Insufficient Funds";
        public static string kInsufficientTech = "Needs more research";
        public static string kEnableRefinery = "Enable production";
        public static string kStorage = "{0:n2} of {1:n2}";
        public static string kUnitsPerSec = "+{0:n2}u/sec";
        public static string kCost = "Cost: ";
        public static string kProductionCost = "Production Cost: £{0:n2}";
        public static string kLimitProduction = "Limit production";
        public static string kFillTanks = "Fill tanks";
        public static string kEmptyTanks = "Empty tanks";
        public static string ksendToRefinery = "Send resource to the Refinery upon vessel recovery.";
        #endregion

        #region Housekeeping
        /// <summary>
        /// How many seconds must elapse before performing a quality check. Kerbin Time
        /// </summary>
        public static double SecondsPerDayKerbin = 21600;

        /// <summary>
        /// How many seconds must elapse before performing a quality check. Earth TIme
        /// </summary>
        public static double SecondsPerDayEarth = 86400;

        /// <summary>
        /// Resource Refinery map containing the resource name (key) and the refinery resource (value)
        /// </summary>
        public Dictionary<string, WBIRefineryResource> refineryResourceMap;

        public static WBIRefinery Instance;

        protected double lastUpdate;
        public WBIRefineryResource[] refineryResources = null;

        static bool resourcesCleared = false;

        public void FixedUpdate()
        {
            //Run the refinery but only at the space center so we don't slow down the game.
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER)
                return;
            if (refineryResources == null)
                return;

            //Get elapsed time
            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdate;
            if (lastUpdate <= 0)
                elapsedTime = TimeWarp.fixedDeltaTime;

            //If less than an hour has passed then just do one update.
            if (elapsedTime < 3600.0f)
            {
                //Drive production
                for (int index = 0; index < refineryResources.Length; index++)
                    refineryResources[index].UpdateProduction(elapsedTime);

                //Update the last update time index
                lastUpdate = Planetarium.GetUniversalTime();
            }

            //Play catchup
            else
            {
                //Loop through one hour at a time.
                int totalHours = (int)(elapsedTime / 3600.0f);
                for (int hour = 0; hour < totalHours; hour++)
                {
                    //Drive production
                    for (int index = 0; index < refineryResources.Length; index++)
                        refineryResources[index].UpdateProduction(3600.0f);

                    //Subtract for elapsed time
                    elapsedTime -= 3600.0f;
                }

                //Update the last update time index, accounting for fractional hours
                lastUpdate = Planetarium.GetUniversalTime() - elapsedTime;
            }
        }

        public void Start()
        {
            //Tell user that we cleared the refinery resources
            if (resourcesCleared)
            {
                resourcesCleared = false;
                ScreenMessages.PostScreenMessage(kRefineryResourcesRemoved, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public override void OnAwake()
        {
            base.OnAwake();
            Instance = this;
            GameEvents.OnVesselRecoveryRequested.Add(OnVesselRecoveryRequested);

            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch.launchBtn.onClick.AddListener(new UnityEngine.Events.UnityAction(launchVessel));

            //Get the list of refinery resources
            refineryResourceMap = WBIRefineryResource.LoadRefineryResources();
            if (refineryResourceMap.Keys.Count > 0)
                refineryResources = refineryResourceMap.Values.ToArray();

            //Upgrade to max tier if needed
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                foreach (WBIRefineryResource refineryResource in refineryResourceMap.Values)
                    refineryResource.UpgradeToMaxTier();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            //Get the refinery nodes
            ConfigNode[] refineryNodes = node.GetNodes(WBIRefineryResource.kRefineryNode);
            string resourceName;
            for (int index = 0; index < refineryNodes.Length; index++)
            {
                resourceName = refineryNodes[index].GetValue("resourceName");
                if (refineryResourceMap.ContainsKey(resourceName))
                    refineryResourceMap[resourceName].Load(refineryNodes[index]);
            }

            //Other parameters
            if (node.HasValue("lastUpdate"))
                double.TryParse(node.GetValue("lastUpdate"), out lastUpdate);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            //Save the refinery nodes
            foreach (WBIRefineryResource refineryResource in refineryResourceMap.Values)
                node.AddNode(refineryResource.Save());

            //Save other parameters
            node.AddValue("lastUpdate", lastUpdate);
        }

        public void OnDestroy()
        {
            GameEvents.OnVesselRecoveryRequested.Remove(OnVesselRecoveryRequested);
            if (HighLogic.LoadedSceneIsEditor)
                EditorLogic.fetch.launchBtn.onClick.RemoveListener(new UnityEngine.Events.UnityAction(launchVessel));
        }

        public static double SecondsPerDay
        {
            get
            {
                return GameSettings.KERBIN_TIME == true ? SecondsPerDayKerbin : SecondsPerDayEarth;
            }
        }
        #endregion

        #region GameEvents
        public virtual void OnVesselRecoveryRequested(Vessel recoveryVessel)
        {
            if (refineryResources == null)
                return;

            //Grab each resource we added and store it in the KSC tank
            int partCount = recoveryVessel.parts.Count;
            int resourceCount;
            PartResource resource;
            WBIRefineryResource refineryResource;
            double amount = 0;
            for (int index = 0; index < partCount; index++)
            {
                resourceCount = recoveryVessel.parts[index].Resources.Count;
                for (int resourceIndex = 0; resourceIndex < resourceCount; resourceIndex++)
                {
                    //Get the resource
                    resource = recoveryVessel.parts[index].Resources[resourceIndex];

                    //Clear the resource and add it to the storage tank
                    if (refineryResourceMap.ContainsKey(resource.resourceName))
                    {
                        //Get the refinery resource
                        refineryResource = refineryResourceMap[resource.resourceName];

                        //Send resource to the Refinery if we're allowed to.
                        if (refineryResource.sendToRefinery)
                        {
                            Debug.Log("[WBIRefinery] - Sending resource to refinery: " + refineryResource.resourceName);
                            //Scoop up the resources that we can hold
                            if (resource.amount + refineryResource.amount <= refineryResource.maxAmount)
                            {
                                refineryResource.amount += resource.amount;
                                resource.amount = 0f;
                            }

                            //Take what we can up to the max. Leave the rest.
                            else
                            {
                                amount = refineryResource.maxAmount - refineryResource.amount;
                                refineryResource.amount = refineryResource.maxAmount;
                                resource.amount -= amount;
                            }

                            //Update the map
                            refineryResourceMap[resource.resourceName] = refineryResource;
                        }
                    }
                }
            }
        }
        #endregion

        #region API
        public bool IsRefineryResource(string resourceName)
        {
            return refineryResourceMap.ContainsKey(resourceName);
        }
        #endregion

        #region Helpers
        internal void launchVessel()
        {
            if (CheatOptions.InfinitePropellant)
                return;
            ShipConstruct ship = EditorLogic.fetch.ship;

            //Clear refinery resources in the ship (Career only)
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
                return;
            int partCount = ship.parts.Count;
            Part part;
            PartResource resource;
            int index = 0;
            int resourceCount;
            resourcesCleared = false;
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                part = ship.parts[partIndex];
                resourceCount = part.Resources.Count;
                if (resourceCount == 0)
                    continue;

                for (index = 0; index < resourceCount; index++)
                {
                    resource = part.Resources[index];
                    if (!IsRefineryResource(resource.resourceName))
                        continue;
                    if (resource.amount <= 0)
                        continue;
                    resource.amount = 0f;
                    resourcesCleared = true;
                }
            }
        }
        #endregion
    }
}
