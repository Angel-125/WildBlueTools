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
    internal class LoadExperimentView : Window<LoadExperimentView>
    {
        static Texture transferIcon;
        static Texture experimentIcon;

        public Part part = null;
        public WBIModuleScienceExperiment transferRecipient = null;
        public string defaultExperiment = null;

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

        public LoadExperimentView() :
            base("Load Experiment", 600, 330)
        {
            Resizable = false;
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);
            ConfigNode[] experiments = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION");

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

        protected void drawExperiments()
        {
            if (experimentDefs == null)
            {
                GUILayout.Label("No experiment slots available");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (ConfigNode experimentDef in experimentDefs)
            {
                GUILayout.BeginVertical();

                GUILayout.BeginScrollView(scrollPosPanel, experimentPanelOptions);

                GUILayout.BeginHorizontal();
                //Transfer button
                if (GUILayout.Button(transferIcon, buttonOptions))
                {
                    transferRecipient.LoadFromDefinition(experimentDef.GetValue("id"), true);
                    SetVisible(false);
                }
                GUILayout.BeginVertical();

                //Title
                GUILayout.BeginHorizontal();
                GUILayout.Label(experimentIcon, iconOptions);
                GUILayout.Label(experimentDef.GetValue("title"));
                GUILayout.EndHorizontal();

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
                transferIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/MOLE/Icons/TransferIcon", false);
            if (experimentIcon == null)
                experimentIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/MOLE/Icons/ExperimentIcon", false);
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
            if (node.HasValue("requiredParts"))
                info.Append("<b>Parts: </b>" + node.GetValue("requiredParts") + "\r\n");
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
