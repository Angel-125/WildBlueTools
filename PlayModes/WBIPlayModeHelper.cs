using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using KSP.IO;
using KSP.UI.Screens;
using System.IO;

/*
Source code copyrighgt 2017, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    internal class WBIPlayModeHelper
    {
        //For now, we have no default play mode. Eventually it will become Classic Stock.
        public static string DefaultPlayMode = string.Empty;

        //Where we find the play modes
        public const string TemplatesFolder = "GameData/WildBlueIndustries/000WildBlueTools/Templates";

        public const string PlayModeFileName = "WBIPlayMode.cfg";
        public const string PlayModeNodeName = "WBIPLAYMODE";
        public const string SettingNode = "WBIPLAYMODESETTING";
        public const string PlayModeExtensionName = "WBIPLAYMODEEXT";

        public ConfigNode[] playModeNodes;

        public void CreateModeFile()
        {
            if (playModeNodes == null)
                GetModes();

            //Check default
            int index = GetPlayModeIndex(DefaultPlayMode);
            if (index != -1)
            {
                SetPlayMode(DefaultPlayMode);
                return;
            }

            //auto-detect
            index = AutodetectMode();
            if (index != -1)
            {
                SetPlayMode(playModeNodes[index].GetValue("name"));
            }

            //The first item in the array?
            else if (playModeNodes.Length > 0)
            {
                SetPlayMode(playModeNodes[0].GetValue("name"));
            }
        }

        public int GetCurrentModeIndex()
        {
            if (playModeNodes == null)
                GetModes();

            //Check file
            string playModeName = GetPlayModeFromFile();
            int index = GetPlayModeIndex(playModeName);
            if (index != -1)
                return index;

            //Check default
            index = GetPlayModeIndex(DefaultPlayMode);
            if (index != -1)
                return index;

            //auto-detect
            index = AutodetectMode();
            if (index != -1)
                return index;

            //The first item in the array?
            else if (playModeNodes.Length > 0)
                return 0;

            //We ain't got no nodes.
            else
                return -1;
        }

        public void SetPlayMode(int index)
        {
            if (playModeNodes == null)
                GetModes();
            if (index < 0 || index > playModeNodes.Length)
                return;
            string modeName = playModeNodes[index].GetValue("name");

            SetPlayMode(modeName);
        }

        public void SetPlayMode(string name)
        {
            Debug.Log("[WBIPlayModeHelper] - SetPlayMode called for mode: " + name);
            if (playModeNodes == null)
                GetModes();

            if (!string.IsNullOrEmpty(name))
            {
                ConfigNode node = new ConfigNode("root");
                string playModeFile = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/" + PlayModeFileName;

                //Remove any existing file
                if (System.IO.File.Exists(playModeFile))
                    System.IO.File.Delete(playModeFile);

                //Create new file
                ConfigNode nodePlayMode = new ConfigNode(SettingNode);
                nodePlayMode.AddValue("name", name);
                node.AddNode(nodePlayMode);
                node.Save(playModeFile);

                //Update the templates
                updateTemplates(name);
            }
        }

        public ConfigNode[] GetModes()
        {
            playModeNodes = GameDatabase.Instance.GetConfigNodes(PlayModeNodeName);
            if (playModeNodes != null)
                Debug.Log("[WBIPlayModeHelper] - Modes found: " + playModeNodes.Length);
            return playModeNodes;
        }

        public int AutodetectMode()
        {
            if (playModeNodes == null)
                return -1;
            Debug.Log("[WBIPlayModeHelper] - Auto-detecting play mode...");
            int modeIndex = -1;
            ConfigNode playNode;
            string[] filePaths;

            //Find the mode that is active
            for (int index = 0; index < playModeNodes.Length; index++)
            {
                playNode = playModeNodes[index];

                //Get the file paths for all the config files in the play mode
                filePaths = playNode.GetValues("templatePath");

                //Sort through the file list and find one that ends with .cfg. That will indicate which play mode is active.
                foreach (string filePath in filePaths)
                {
                    if (IsActiveMode(playNode, KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/" + filePath))
                    {
                        Debug.Log("[WBIPlayModeHelper] - Auto-detected Play Mode: " + playNode.GetValue("name"));
                        modeIndex = index;
                        return modeIndex;
                    }
                }
            }

            Debug.Log("[WBIPlayModeHelper] - No Play Mode auto-detected");
            return modeIndex;
        }

        public bool IsActiveMode(ConfigNode playNode, string filePath)
        {
            string[] filePathNames;

            //Check file list.
            filePathNames = Directory.GetFiles(filePath);
            if (filePathNames.Length > 0)
            {
                foreach (string filePathName in filePathNames)
                {
                    if (filePathName.EndsWith(".cfg"))
                    {
                        return true;
                    }
                }
            }

            //Search sub-directories
            string[] subDirectories = Directory.GetDirectories(filePath);
            if (subDirectories.Length > 0)
            {
                foreach (string subDirectory in subDirectories)
                {
                    if (IsActiveMode(playNode, subDirectory))
                        return true;
                }
            }

            return false;
        }

        public string GetPlayModeFromFile()
        {
            string playModeFile = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/" + PlayModeFileName;

            //If the file doesn't even exist, then we're done.
            if (!System.IO.File.Exists(playModeFile))
                return null;

            //The file exists, try to get the app mode name.
            ConfigNode node = ConfigNode.Load(playModeFile);
            if (node.HasNode(SettingNode))
                node = node.GetNode(SettingNode);

            if (node.HasValue("name"))
                return node.GetValue("name");
            else
                return null;
        }

        public int GetPlayModeIndex(string modeName)
        {
            if (playModeNodes == null)
                return -1;
            if (string.IsNullOrEmpty(modeName))
                return -1;

            ConfigNode node;
            for (int index = 0; index < playModeNodes.Length; index++)
            {
                node = playModeNodes[index];
                if (node.HasValue("name"))
                {
                    if (node.GetValue("name") == modeName)
                        return index;
                }
            }

            return -1;
        }

        protected void updateTemplates(string modeName)
        {
            ConfigNode playNode;
            string[] filePaths;
            int selectedIndex = GetPlayModeIndex(modeName);
            bool asTextFile = false;

            //Go through all the Play Mode config files and rename them.
            //This only applies to WBIPLAYMODE nodes.
            for (int index = 0; index < playModeNodes.Length; index++)
            {
                playNode = playModeNodes[index];
                if (!playNode.HasValue("name"))
                    continue;
                if (!playNode.HasValue("templatePath"))
                    continue;

                //Get the file paths for all the config files in the play mode
                filePaths = playNode.GetValues("templatePath");
                asTextFile = playNode.GetValue("name") == modeName ? false : true;

                //Sort through the file list and find one that ends with .cfg. That will indicate which play mode is active.
                foreach (string filePath in filePaths)
                {
                    renameFiles(KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/" + filePath, asTextFile);
                }

            }

            //Different mods tie into the play modes and have their own files to rename.
            //These extensions are nodes with the WBIPLAYMODEEXT.
            //Wild blue's extensions are found in their respective Templates folders.
            ConfigNode[] extensionNodes = GameDatabase.Instance.GetConfigNodes(PlayModeExtensionName);
            if (extensionNodes == null)
                return;
            foreach (ConfigNode extensionNode in extensionNodes)
            {
                if (!extensionNode.HasValue("name"))
                    continue;
                if (!extensionNode.HasValue("templatePath"))
                    continue;

                filePaths = extensionNode.GetValues("templatePath");
                asTextFile = extensionNode.GetValue("name") == modeName ? false : true;

                foreach (string filePath in filePaths)
                {
                    renameFiles(KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/" + filePath, asTextFile);
                }
            }
        }

        protected void renameFiles(string filePath, bool asTextFile)
        {
            if (!Directory.Exists(filePath))
                return;

            string[] filePathNames;
            string renamedFile;

            //Check file list.
            filePathNames = Directory.GetFiles(filePath);
            if (filePathNames.Length > 0)
            {
                foreach (string filePathName in filePathNames)
                {
                    if (asTextFile)
                    {
                        if (filePathName.EndsWith(".cfg"))
                        {
                            renamedFile = filePathName.Replace(".cfg", ".txt");
                            System.IO.File.Move(filePathName, renamedFile);
//                            Debug.Log("[WBIPlayModeHelper] - renameFiles: renaming " + filePathName + " to " + renamedFile);
                        }
                    }
                    else
                    {
                        if (filePathName.EndsWith(".txt"))
                        {
                            renamedFile = filePathName.Replace(".txt", ".cfg");
                            System.IO.File.Move(filePathName, renamedFile);
//                            Debug.Log("[WBIPlayModeHelper] - renameFiles: renaming " + filePathName + " to " + renamedFile);
                        }
                    }

                }
            }

            //Search sub-directories
            string[] subDirectories = Directory.GetDirectories(filePath);
            if (subDirectories.Length > 0)
            {
                foreach (string subDirectory in subDirectories)
                {
                    renameFiles(subDirectory, asTextFile);
                }
            }
        }
    }
}
