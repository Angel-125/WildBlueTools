using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class ScienceLabResultsView : Window<ScienceLabResultsView>
    {
        const string kTransmitScience = "<color=lightBlue>Transmit science</color>";
        const string kTransferScience = "<color=white>Transfer to Mobile Processing Lab</color>";

        public WBIBasicScienceLab scienceLab = null;
        public Part part;

        protected ModuleScienceLab sciLab = null;

        bool transmitHighlighted = false;
        bool publishHighlighted = false;
        Texture transmitIconWhite;
        Texture transmitIconBlack;
        Texture transferIconWhite;
        Texture transferIconBlack;
        Texture transmitIcon;
        Texture transferIcon;
        Texture scienceIcon;
        Texture repIcon;
        Texture fundsIcon;
        Vector2 scrollPosition = new Vector2(0, 0);
        string resultsText = "";
        bool hasMPL = false;

        public ScienceLabResultsView(string title) :
        base(title, 600, 330)
        {
            Resizable = false;

            if (transmitIconWhite == null)
            {
                transmitIconWhite = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/WBITransmitWhite", false);
                transmitIconBlack = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/WBITransmit", false);

                transferIconWhite = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/WBIScienceWhite", false);
                transferIconBlack = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/WBIScience", false);

                scienceIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/WBIScienceWhite", false);
                repIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/WBIPublishWhite", false);
                fundsIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/WBISellWhite", false);
            }
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (newValue)
            {
                findMPL();
                getResultsText();
            }

            if (newValue == false)
                scienceLab = null;
        }

        public void DrawGUIControls()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginScrollView(new Vector2(), new GUIStyle(GUI.skin.textArea), new GUILayoutOption[] { GUILayout.Height(480) });

            if (HighLogic.LoadedSceneIsFlight == false)
            {
                GUILayout.Label("<color=yellow>This screen is working, but the contents can only be accessed in flight.</color>");
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                return;
            }

            GUILayout.BeginHorizontal();
            drawStatus();
            drawResultsText();
            GUILayout.EndHorizontal();
            drawTransmitButtons();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        protected override void DrawWindowContents(int windowId)
        {
            DrawGUIControls();
        }

        protected void findMPL()
        {
            //If Pathfinder is installed, see if WBISciLabOpsView is available somewhere on the vessel. If so, then MPL is available.
            //If not, MPL is not available.
            if (Utils.IsModInstalled("Pathfinder"))
            {
                foreach (Part vesselPart in this.part.vessel.Parts)
                {
                    if (vesselPart.Modules.Contains("WBISciLabOpsView"))
                    {
                        hasMPL = true;
                        return;
                    }
                }

                hasMPL = false;
                return;
            }

            //If no pathfinder is installed, then MPL is available if the vessel has a ModuleScienceConverter that is active.
            List<ModuleScienceConverter> converters = this.part.vessel.FindPartModulesImplementing<ModuleScienceConverter>();
            if (converters.Count > 0)
            {
                foreach (ModuleScienceConverter converter in converters)
                {
                    if (converter.enabled && converter.isEnabled)
                    {
                        hasMPL = true;
                        return;
                    }
                }
            }
        }

        protected void getResultsText()
        {
            List<string> results = new List<string>();
            string situation = ScienceUtil.GetExperimentSituation(part.vessel).ToString();
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(scienceLab.experimentID);
            string resultKey;

            this.WindowTitle = experiment.experimentTitle + " from " + this.part.vessel.mainBody.name + "'s " + Utils.GetCurrentBiome(this.part.vessel).name;

            foreach (string key in experiment.Results.Keys)
            {
                resultKey = key.Replace("*", "");

                if (resultKey.Contains(situation))
                {
                    results.Add(experiment.Results[key]);
                }
            }

            if (results.Count == 0)
            {
                if (experiment.Results.ContainsKey("default"))
                    resultsText = experiment.Results["default"];
                else
                    resultsText = "You've done some basic research.";
                return;
            }

            int resultIndex = UnityEngine.Random.Range(0, results.Count);
            if (resultIndex >= results.Count)
                resultIndex = results.Count - 1;
            resultsText = results[resultIndex];
        }

        protected void drawResultsText()
        {
            GUILayout.BeginVertical();

            //Results text
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, new GUILayoutOption[] { GUILayout.Width(360), GUILayout.Height(240) });
            GUILayout.Label(resultsText);
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        protected void drawTransmitButtons()
        {
            string message = "";

            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
                return;
            if (ResearchAndDevelopment.Instance == null)
                return;

            GUILayout.BeginHorizontal();

            //Transmit button
            if (GUILayout.Button(transmitIcon, new GUILayoutOption[] { GUILayout.Width(64), GUILayout.Height(64) }))
                scienceLab.TransmitResults();

            if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                transmitIcon = transmitIconWhite;
                transmitHighlighted = true;
                message = kTransmitScience;
            }
            else if (transmitHighlighted)
            {
                transmitIcon = transmitIconWhite;
                transmitHighlighted = false;
                message = kTransmitScience;
            }
            else
            {
                transmitIcon = transmitIconBlack;
            }

            if (ResearchAndDevelopment.GetTechnologyState("advExploration") == RDTech.State.Available && hasMPL)
            {
                //Transfer button
                if (GUILayout.Button(transferIcon, new GUILayoutOption[] { GUILayout.Width(64), GUILayout.Height(64) }))
                    scienceLab.TransferToScienceLab();

                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    transferIcon = transferIconWhite;
                    publishHighlighted = true;
                    message = kTransferScience;
                }
                else if (publishHighlighted)
                {
                    transferIcon = transferIconWhite;
                    publishHighlighted = false;
                    message = kTransferScience;
                }
                else
                {
                    transferIcon = transferIconBlack;
                }
            }

            GUILayout.BeginScrollView(new Vector2(0, 0));
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(message);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();
        }

        protected void drawStatus()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginScrollView(new Vector2(0, 0));
            GUILayout.Label("<color=white><b>Status: </b>" + scienceLab.status + "</color>");
            GUILayout.EndScrollView();

            GUILayout.BeginScrollView(new Vector2(0, 0));
            GUILayout.Label("<color=white><b>Progress: </b>" + scienceLab.progress + "</color>");
            GUILayout.EndScrollView();

            GUILayout.BeginScrollView(new Vector2(0, 0));
            GUILayout.Label("<color=white><b>Last Result: </b>" + scienceLab.lastAttempt + "</color>");
            GUILayout.EndScrollView();

            if (scienceLab.scienceAdded > 0.001f)
            {
                GUILayout.BeginScrollView(new Vector2(0, 0));
                GUILayout.Label(new GUIContent(string.Format("<color=lightBlue><b> Science Discovered: </b>{0:f2}</color>", scienceLab.scienceAdded), scienceIcon),
                    new GUILayoutOption[] { GUILayout.Height(24) });
                GUILayout.EndScrollView();
            }

            if (scienceLab.reputationAdded > 0.001f)
            {
                GUILayout.BeginScrollView(new Vector2(0, 0));
                GUILayout.Label(new GUIContent("<color=yellow><b> Reputation: </b>" + scienceLab.reputationAdded + "</color>", repIcon),
                    new GUILayoutOption[] { GUILayout.Height(24) });
                GUILayout.EndScrollView();
            }

            if (scienceLab.fundsAdded > 0.001f)
            {
                GUILayout.BeginScrollView(new Vector2(0, 0));
                GUILayout.Label(new GUIContent("<b> Funds: </b>" + scienceLab.fundsAdded, fundsIcon), new GUILayoutOption[] { GUILayout.Height(24) });
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
        }

    }
}
