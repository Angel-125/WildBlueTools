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
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class WBTAppButton : MonoBehaviour
    {
        static protected Texture2D appIcon = null;
        static protected ApplicationLauncherButton appLauncherButton = null;
        public static PlayModesWindow playModesWindow;

        public void Awake()
        {
            playModesWindow = new PlayModesWindow();
            appIcon = GameDatabase.Instance.GetTexture("WildBlueIndustries/000WildBlueTools/Icons/WildBlueLogo", false);
            GameEvents.onGUIApplicationLauncherReady.Add(SetupGUI);
            playModesWindow.changePlayModeDelegate = changePlayMode;
        }

        public void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(SetupGUI);
        }

        private void SetupGUI()
        {
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                if (appLauncherButton == null)
                    appLauncherButton = ApplicationLauncher.Instance.AddModApplication(ToggleGUI, ToggleGUI, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, appIcon);

                //Show the play modes window if we have no config file.
                if (string.IsNullOrEmpty(playModesWindow.playModeHelper.GetPlayModeFromFile()))
                {
                    playModesWindow.playModeHelper.CreateModeFile();
                    playModesWindow.SetVisible(true);
                }
            }

            else if (appLauncherButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
        }

        private void ToggleGUI()
        {
            playModesWindow.SetVisible(!playModesWindow.IsVisible());
        }

        public void changePlayMode(string modeName)
        {
            WBIMainSettings.PayToReconfigure = playModesWindow.payToRemodel;
            WBIMainSettings.RequiresSkillCheck = playModesWindow.requireSkillCheck;
        }
    }


    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class WBIModeChecker : MonoBehaviour
    {
        public static bool ModeMismatchChecked = false;

        public void Awake()
        {
            //Only do this once...
            if (ModeMismatchChecked)
                return;
            ModeMismatchChecked = true;

            WBIPlayModeHelper helper = new WBIPlayModeHelper();

            //If we have a config file, then get the current mode.
            helper.GetModes();
            string playModeName = helper.GetPlayModeFromFile();
            int index = helper.GetPlayModeIndex(playModeName);
            if (index == -1)
            {
                //No config file so let's see if CRP is installed.
                string playModeFile = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/CommunityResourcePack/CRP.version";

                //If the file exists then find the CRP play mode
                if (System.IO.File.Exists(playModeFile))
                {
                    index = helper.GetPlayModeIndex("CRP");
                    if (index == -1)
                        return;
                }
            }

            //Now auto-detect the current mode.
            int autoDetectIndex = helper.AutodetectMode();
            if (autoDetectIndex == -1)
                return;

            //If the modes don't match then make sure we're using the correct templates.
            //This will ensure that we can distribute mods with Classic Stock as the default mode
            //but not break players' saves.
            if (index != autoDetectIndex)
            {
                Debug.Log("[WBIModeChecker] - Play Mode mismatch detected, setting correct templates.");
                helper.SetPlayMode(index);
            }
        }
    }
}
