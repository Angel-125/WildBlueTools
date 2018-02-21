using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
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
    public class WBIUnlockTechResult: PartModule, IWBIExperimentResults
    {
        public const string kUnlockOnePartMsg = "Technological breakthrough! Research results have created an experimental ";
        public const string kUnlockManyPartsMsg = "Technological breakthrough! Research results have created one or more experimental parts found in ";
        public const float kMessageDuration = 6.0f;

        [KSPField]
        public string priorityNodes = string.Empty;

        [KSPField]
        public string blacklistNodes = string.Empty;

        [KSPField]
        public string modFolders = string.Empty;

        [KSPField]
        public string excludeFolders = string.Empty;

        [KSPField]
        public bool unlockAll = false;

        [KSPField]
        public int dieRoll = 100;

        [KSPField]
        public int targetNumber = 0;

        protected string[] techNodeIds;
        protected Dictionary<string, string> techTitles = new Dictionary<string, string>();

        protected void Log(string message)
        {
            Debug.Log("[WBIUnlockTechResult] - " + message);
        }

        public void ExperimentRequirementsMet(string experimentID, float chanceOfSuccess, float resultRoll)
        {
            Log("ExperimentRequirementsMet called");

            //Career/Science mode only
            if (HighLogic.LoadedSceneIsFlight == false)
            {
                Log("Not in flight scene.");
                return;
            }
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                Log("Current game is neither career nor science sandbox.");
                return;
            }
            if (resultRoll < chanceOfSuccess)
            {
                Log(string.Format("resultRoll ({0:f2}) < chanceOfSuccess ({1:f2}), exiting.", resultRoll, chanceOfSuccess));
                return;
            }

            //Make sure we reach our target number
            int unlockRoll = UnityEngine.Random.Range(1, dieRoll);
            if (unlockRoll < targetNumber)
            {
                Log(string.Format("unlockRoll ({0:n}) < targetNumber ({1:n}), exiting.", unlockRoll, targetNumber));
                return;
            }

            //Get the locked parts
            getTechTreeTitles();
            AvailablePart[] loadedParts = getLockedParts();
            if (loadedParts == null || loadedParts.Length == 0)
            {
                Log("No parts to unlock. Exiting.");
                return;
            }

            //Build the priority list
            int index = 0;
            string nodeID;
            if (!string.IsNullOrEmpty(priorityNodes))
            {
                char[] delimiters = new char[] {';'};
                string[] priorityList = priorityNodes.Split(delimiters);
                Log("priorityList length: " + priorityList.Length);

                //Unlock all the parts in all the nodes on the priority list if needed
                if (unlockAll)
                {
                    for (index = 0; index < priorityList.Length; index++)
                    {
                        nodeID = priorityList[index];

                        //Flag parts as experimental
                        for (int partIndex = 0; partIndex < loadedParts.Length; partIndex++)
                        {
                            if (loadedParts[partIndex].TechRequired == nodeID)
                                ResearchAndDevelopment.AddExperimentalPart(loadedParts[partIndex]);
                        }

                        //Inform player
                        ScreenMessages.PostScreenMessage(kUnlockManyPartsMsg + techTitles[nodeID], kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                    }

                    //Done
                    return;
                }

                //Find the first node on the list that isn't unlocked, and flag its parts as experimental.
                //If all the nodes are unlocked, then fall through and randomly unlock a part.
                else
                {
                    bool partWasUnlocked = false;
                    while (index < priorityList.Length)
                    {
                        nodeID = priorityList[index];
                        Debug.Log("Checking nodeID: " + nodeID);

                        if (ResearchAndDevelopment.GetTechnologyState(nodeID) == RDTech.State.Unavailable)
                        {
                            //Flag the parts as experimental
                            for (int partIndex = 0; partIndex < loadedParts.Length; partIndex++)
                            {
                                if (loadedParts[partIndex].TechRequired == nodeID)
                                {
                                    ResearchAndDevelopment.AddExperimentalPart(loadedParts[partIndex]);
                                    partWasUnlocked = true;
                                }
                            }

                            //Inform player
                            if (partWasUnlocked)
                            {
                                ScreenMessages.PostScreenMessage(kUnlockManyPartsMsg + techTitles[nodeID], kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                                return;
                            }
                            else
                            {
                                index += 1;
                                if (index >= priorityList.Length)
                                    break;
                            }
                        }
                        else
                        {
                            index += 1;
                            if (index >= priorityList.Length)
                                break;
                        }
                    }
                }
            }

            //Ok, at this point we need to try and unlock a random part.
            index = UnityEngine.Random.Range(1, loadedParts.Length - 1);
            ResearchAndDevelopment.AddExperimentalPart(loadedParts[index]);
            ScreenMessages.PostScreenMessage(kUnlockOnePartMsg + loadedParts[index].title, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
        }

        protected AvailablePart[] getLockedParts()
        {
            List<AvailablePart> lockedParts = new List<AvailablePart>();

            AvailablePart[] loadedParts = PartLoader.LoadedPartsList.ToArray();
            AvailablePart availablePart;
            for (int index = 0; index < loadedParts.Length; index++)
            {
                availablePart = loadedParts[index];
                if (!ResearchAndDevelopment.PartModelPurchased(availablePart) &&
                    !ResearchAndDevelopment.IsExperimentalPart(availablePart) &&
                    !blacklistNodes.Contains(availablePart.TechRequired))
                {
                    //Check for excluded folders
                    if (!string.IsNullOrEmpty(excludeFolders))
                    {
                        string[] bannedFolders = excludeFolders.Split(new char[] { ';' });
                        bool partIsBanned = false;
                        for (int folderIndex = 0; folderIndex < excludeFolders.Length; folderIndex++)
                        {
                            if (availablePart.partUrl.Contains(excludeFolders[folderIndex]))
                            {
                                partIsBanned = true;
                                break;
                            }
                        }

                        if (partIsBanned)
                            continue;
                    }

                    //If we aren't unlocking parts exclusive to mod folders then just add the part.
                    if (string.IsNullOrEmpty(modFolders))
                    {
                        lockedParts.Add(availablePart);
                    }

                    //See if the part belongs to the mod whitelist.
                    else
                    {
                        string[] allowedFolders = modFolders.Split(new char[] { ';' });
                        for (int folderIndex = 0; folderIndex < allowedFolders.Length; folderIndex++)
                        {
                            if (availablePart.partUrl.Contains(allowedFolders[folderIndex]))
                            {
                                lockedParts.Add(availablePart);
                                break;
                            }
                        }
                    }
                }
            }

            if (lockedParts.Count > 0)
                return lockedParts.ToArray();
            else
                return null;
        }

        protected void getTechTreeTitles()
        {
            Log("getTechTreeTitles called");
            List<string> techTreeIDs = new List<string>();
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("TechTree");
            nodes = nodes[0].GetNodes("RDNode");
            Log("RDNode count: " + nodes.Length);

            //Add all the RDNode id values for all nodes not on the blacklist.
            string nodeID;
            string techTitle = string.Empty;
            techTitles.Clear();
            foreach (ConfigNode node in nodes)
            {
                nodeID = node.GetValue("id");

                if (node.HasValue("title"))
                    techTitle = node.GetValue("title");
                else
                    techTitle = string.Empty;

                if (!string.IsNullOrEmpty(blacklistNodes))
                {
                    if (blacklistNodes.Contains(nodeID) == false)
                    {
                        techTreeIDs.Add(nodeID);
                        if (!string.IsNullOrEmpty(techTitle))
                            techTitles.Add(nodeID, techTitle);
                    }
                }
                else
                {
                    techTreeIDs.Add(nodeID);
                    if (!string.IsNullOrEmpty(techTitle))
                        techTitles.Add(nodeID, techTitle);
                }
            }

            if (techTreeIDs.Count > 0)
                techNodeIds = techTreeIDs.ToArray();
            else
                techNodeIds = null;
        }
    }
}
