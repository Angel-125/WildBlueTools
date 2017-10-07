using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015 - 2016, by Michael Billard (Angel-125)
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
    internal class LoadExperimentView : Dialog<LoadExperimentView>
    {
        static Texture transferIcon;
        static Texture experimentIcon;

        public Part part = null;
        public WBIModuleScienceExperiment transferRecipient = null;
        public string defaultExperiment = null;
        public bool checkCreationResources;
        public string creationTags = string.Empty;
        public string defaultCreationResource = string.Empty;
        public double minimumCreationAmount = 0f;

        private Vector2 scrollPos = new Vector2(0, 0);
        private Vector2 scrollPosSynopsis = new Vector2(0, 0);
        private string experimentSynopsis;
        private GUILayoutOption[] synopsisOptions = new GUILayoutOption[] { GUILayout.Width(175) };
        List<ConfigNode> experimentDefs = null;
        private Vector2 scrollPosPanel = new Vector2(0, 0);
        private GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Width(64), GUILayout.Height(64) };
        private GUILayoutOption[] experimentPanelOptions = new GUILayoutOption[] { GUILayout.Height(75) };
        private GUILayoutOption[] iconOptions = new GUILayoutOption[] { GUILayout.Width(24), GUILayout.Height(24) };
        private ConfigNode lastExpDef = null;
        private int confirmCreationIndex = -1;

        public LoadExperimentView() :
            base("Load Experiment", 600, 330)
        {
            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            ConfigNode[] experiments = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION");

            WindowTitle = this.part.partInfo.title + ": Load Experiment";
            confirmCreationIndex = -1;

            setupIcons();

            if (newValue && experimentDefs == null)
            {
                experimentDefs = new List<ConfigNode>();
                string techNode;

                //Find WBI specific experiments. They'll have mass and description nodes
                foreach (ConfigNode experiment in experiments)
                {
                    if (experiment.HasValue("description") && experiment.HasValue("mass"))
                    {
                        if (experiment.GetValue("id") != defaultExperiment)
                        {
                            //Filter check: Only allow experiments that match my tagFilter.
                            //If an experiment has no filter, then it won't be included.
                            if (string.IsNullOrEmpty(creationTags) == false)
                            {
                                if (experiment.HasValue("tags") == false)
                                    continue;

                                bool includesFilter = false;
                                string[] filters = experiment.GetValue("tags").Split(',');
                                for (int index = 0; index < filters.Length; index++)
                                {
                                    if (creationTags.Contains(filters[index]))
                                    {
                                        includesFilter = true;
                                        break;
                                    }
                                }
                                if (includesFilter == false)
                                    continue;
                            }

                            //Resource check: if the experiment has no specified resources, then
                            //setup the default.
                            if (checkCreationResources)
                            {
                                string creationResources = experiment.GetValue("creationResources");
                                if (string.IsNullOrEmpty(creationResources))
                                    experiment.AddValue("creationResources", defaultCreationResource + "," + minimumCreationAmount);
                            }

                            //Tech check
                            if (experiment.HasValue("techRequired") == false || 
                                (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && 
                                HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX))
                            {
                                experimentDefs.Add(experiment);
                            }

                            else if (ResearchAndDevelopment.Instance != null)
                            {
                                techNode = experiment.GetValue("techRequired");
                                if (ResearchAndDevelopment.GetTechnologyState(techNode) == RDTech.State.Available)
                                    experimentDefs.Add(experiment);
                            }
                        }
                    }
                }
            }
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();

            drawExperiments();
            drawSynopsis();

            GUILayout.EndHorizontal();
        }

        protected bool buildExperiment(string requiredResources)
        {
            try
            {
                string[] resourceList = requiredResources.Split(';');
                string[] resourceInfo;
                Dictionary<string, double> shoppingList = new Dictionary<string, double>();
                double amount;
                string resourceName;

                //Make sure we have enough of the resource aboard, and add it to our shopping list
                //if we do.
                for (int resourceIndex = 0; resourceIndex < resourceList.Length; resourceIndex++)
                {
                    resourceInfo = resourceList[resourceIndex].Split(',');
                    resourceName = resourceInfo[0];
                    amount = double.Parse(resourceInfo[1]);

                    if (ResourceHelper.GetTotalResourceAmount(resourceName, this.part.vessel) >= amount)
                        shoppingList.Add(resourceInfo[0], amount);
                }

                //If our shopping list count doesn't match the resourceList count then we're done.
                if (shoppingList.Count != resourceList.Length)
                    return false;

                //Ok, go shopping
                string[] keyList = shoppingList.Keys.ToArray<string>();
                for (int index = 0; index < keyList.Length; index++)
                {
                    resourceName = keyList[index];
                    this.part.RequestResource(resourceName, shoppingList[resourceName], ResourceFlowMode.ALL_VESSEL);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        protected void drawExperiments()
        {
            ConfigNode experimentDef;
            ConfigNode[] defs = experimentDefs.ToArray();

            if (experimentDefs == null)
            {
                GUILayout.Label("No experiment slots available");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            for (int index = 0; index < defs.Length; index++)
            {
                experimentDef = defs[index];
                GUILayout.BeginVertical();

                GUILayout.BeginScrollView(scrollPosPanel, experimentPanelOptions);

                GUILayout.BeginHorizontal();
                //Transfer button
                if (GUILayout.Button(transferIcon, buttonOptions))
                {
                    //If needed make sure we can build the experiment.
                    if (checkCreationResources && confirmCreationIndex == index)
                    {
                        confirmCreationIndex = -1;

                        if (buildExperiment(experimentDef.GetValue("creationResources")))
                        {
                            transferRecipient.LoadFromDefinition(experimentDef.GetValue("id"), true);
                            SetVisible(false);
                        }

                        else //Not enough resources
                        {
                            ScreenMessages.PostScreenMessage("Insufficient resources to create the experiment.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                    }

                    else if (checkCreationResources)
                    {
                        confirmCreationIndex = index;
                        ScreenMessages.PostScreenMessage("Click again to confirm experiment creation.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }

                    //Resources not required, just transfer the experiment.
                    else
                    {
                        transferRecipient.LoadFromDefinition(experimentDef.GetValue("id"), true);
                        SetVisible(false);
                    }

                }
                GUILayout.BeginVertical();

                //Title
                GUILayout.BeginHorizontal();
                GUILayout.Label(experimentIcon, iconOptions);
                GUILayout.Label(experimentDef.GetValue("title"));
                GUILayout.EndHorizontal();

                //Creation costs
                if (checkCreationResources)
                {
                    GUILayout.Label("To build: " + experimentDef.GetValue("creationResources"));
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();

                //Hit test for the experiment panel
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    if (lastExpDef != experimentDef)
                    {
                        lastExpDef = experimentDef;
                        experimentSynopsis = getSynopsis(experimentDef);
                        scrollPosSynopsis = new Vector2(0, 0);
                    }
                }

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        protected void drawSynopsis()
        {
            scrollPosSynopsis = GUILayout.BeginScrollView(scrollPosSynopsis, synopsisOptions);
            if (string.IsNullOrEmpty(experimentSynopsis) == false)
                GUILayout.Label(experimentSynopsis);
            else
                GUILayout.Label(" ");
            GUILayout.EndScrollView();
        }

        protected void setupIcons()
        {
            if (transferIcon == null)
                transferIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/CompletedIcon", false);
            if (experimentIcon == null)
                experimentIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/ExperimentIcon", false);
        }

        protected string getSynopsis(ConfigNode node)
        {
            StringBuilder info = new StringBuilder();

            info.Append(node.GetValue("title") + "\r\n\r\n");
            info.Append(node.GetValue("description") + "\r\n\r\n");
            info.Append("<b>Requirements</b>\r\n\r\n");

            //Celestial bodies
            if (node.HasValue("celestialBodies"))
                info.Append("<b>Allowed Planets: </b>" + node.GetValue("celestialBodies") + "\r\n");
            //Flight states
            if (node.HasValue("situations"))
                info.Append("<b>Allowed Sitiations: </b>" + node.GetValue("situations") + "\r\n");
            //Mininum Crew
            if (node.HasValue("minCrew"))
                info.Append("<b>Minimum Crew: </b>" + node.GetValue("minCrew") + "\r\n");
            //Min Altitude
            if (node.HasValue("minAltitude"))
                info.Append(string.Format("<b>Min altitude: </b>{0:f2}m\r\n", float.Parse(node.GetValue("minAltitude"))));
            //Max Altitude
            if (node.HasValue("maxAltitude"))
                info.Append(string.Format("<b>Max altitude: </b>{0:f2}m\r\n", float.Parse(node.GetValue("maxAltitude"))));
            //Required parts
            if (node.HasValue("requiredPart"))
            {
                string[] requiredParts = node.GetValues("requiredPart");
                info.Append("<b>Requires one of: </b>\r\n");
                for (int index = 0; index < requiredParts.Length; index++)
                    info.Append(requiredParts[index] + "\r\n");
            }
            //Required resources
            if (node.HasValue("requiredResources"))
            {
                info.Append("<b>Resources: </b>\r\n");

                //Build resource map
                string[] resources = node.GetValue("requiredResources").Split(new char[] { ';' });
                string[] resourceAmount = null;

                foreach (string resource in resources)
                {
                    resourceAmount = resource.Split(new char[] { ',' });
                    info.Append(resourceAmount[0] + string.Format(" ({0:f2})\r\n", double.Parse(resourceAmount[1])));
                }
            }
            return info.ToString();
        }

    }
}
