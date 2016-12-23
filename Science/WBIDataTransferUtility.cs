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
    public class WBIDataTransferUtility : PartModule, IOpsView
    {
        [KSPField(guiName = "Debug Mode")]
        public bool debugMode = false;

        protected ModuleScienceLab sciLab = null;
        private Vector2 scrollPos;

        [KSPEvent(guiName = "Add 50 Data")]
        public void AddData()
        {
            sciLab.dataStored += 50.0f;
        }

        public List<string> GetButtonLabels()
        {
            List<string> labels = new List<string>();

            labels.Add("Data Transfer");

            return labels;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            sciLab = this.part.FindModuleImplementing<ModuleScienceLab>();

            if (sciLab != null && debugMode)
            {
                Fields["AddData"].guiActive = true;
            }
        }

        public void DrawOpsWindow(string buttonLabel)
        {
            ModuleScienceLab curLab;
            float maxDataIn, maxDataOut = 0f;

            GUILayout.BeginVertical();

            if (HighLogic.LoadedSceneIsEditor)
            {
                GUILayout.Label("<color=yellow>This tab is working. However, there's nothing to do in the editor.</color>");
                GUILayout.EndVertical();
                return;
            }

            //If the part has no science lab then we're done.
            if (sciLab == null)
            {
                GUILayout.Label("<color=yellow>Can't seem to find ModuleScienceLab.</color>");
                GUILayout.EndVertical();
                return;
            }

            //find all the science lab modules on the vessel.
            List<ModuleScienceLab> labs = this.part.vessel.FindPartModulesImplementing<ModuleScienceLab>();
            ModuleScienceLab[] scienceLabs = labs.ToArray();
            int totalLabs = labs.Count;

            //List the amount of data in the science lab.
            GUILayout.BeginScrollView(new Vector2(0, 0), new GUILayoutOption[] { GUILayout.Height(50) });
            GUILayout.Label(string.Format("<color=white><b>Data: </b>{0:f2}/{1:f2}</color>", sciLab.dataStored, sciLab.dataStorage));
            GUILayout.EndScrollView();

            //For each part, list the name of the part, the amount of data it has, and a button to press to transfer into this part's lab.
            //But lists max that it can transfer
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            for (int index = 0; index < totalLabs; index++)
            {
                curLab = scienceLabs[index];
                if (curLab == sciLab)
                    continue;

                GUILayout.BeginScrollView(new Vector2(0, 0), new GUILayoutOption[] { GUILayout.Height(120) });
                GUILayout.Label("<color=white><b>Lab:</b> " + curLab.part.partInfo.title + "</color>");
                GUILayout.Label(string.Format("<color=white><b>Data: </b>{0:f2}/{1:f2}</color>", curLab.dataStored, curLab.dataStorage));

                //Draw transfer out button
                if (curLab.dataStored < curLab.dataStorage)
                {
                    if (sciLab.dataStored + curLab.dataStored <= curLab.dataStorage)
                        maxDataOut = sciLab.dataStored;
                    else
                        maxDataOut = curLab.dataStorage - curLab.dataStored;

                    if (GUILayout.Button(string.Format("Give {0:f2} data to {1:s}", maxDataOut, curLab.part.partInfo.title)))
                    {
                        curLab.dataStored += maxDataOut;
                        sciLab.dataStored -= maxDataOut;
                    }
                }

                //Draw transfer out button
                if (sciLab.dataStored < sciLab.dataStorage)
                {
                    if (curLab.dataStored + sciLab.dataStored <= sciLab.dataStorage)
                        maxDataIn = curLab.dataStored;
                    else
                        maxDataIn = sciLab.dataStorage - sciLab.dataStored;

                    if (GUILayout.Button(string.Format("Take {0:f2} data from {1:s}", maxDataIn, curLab.part.partInfo.title)))
                    {
                        sciLab.dataStored += maxDataIn;
                        curLab.dataStored -= maxDataIn;
                    }
                }

                GUILayout.EndScrollView();
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public void SetContextGUIVisible(bool isVisible)
        {
        }

        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }
    }
}
