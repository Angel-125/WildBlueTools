using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using FinePrint;
using Upgradeables;
using KSP.UI.Screens;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Reflection;


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
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class WBIPartDeprecator : MonoBehaviour
    {
        public static EditorPartListFilter<AvailablePart> deprecatedPartFilter;

        public void Start()
        {
            GameEvents.onLevelWasLoadedGUIReady.Add(OnLevelLoaded);
        }

        public void OnDestroy()
        {
            GameEvents.onLevelWasLoadedGUIReady.Remove(OnLevelLoaded);
        }

        public void OnLevelLoaded(GameScenes scene)
        {
            if (scene == GameScenes.EDITOR)
            {
                Func<AvailablePart, bool> partAvailableFunc = (_aPart) => partIsAvailable(_aPart);
                deprecatedPartFilter = new EditorPartListFilter<AvailablePart>("WBIPartDeprecator", partAvailableFunc);
                EditorPartList.Instance.ExcludeFilters.AddFilter(deprecatedPartFilter);
            }
        }

        protected bool partIsAvailable(AvailablePart availablePart)
        {
            //No part? Not available.
            if (availablePart == null)
                return false;

            //If the part URL contains "Deprecated" then it isn't available (and is in the standard Deprecated folder)
            if (availablePart.partUrl.Contains("Deprecated"))
            {
                if (WBIMainSettings.EnableDebugLogging)
                    Debug.Log("[WBIPartDeprecator] - " + availablePart.name + " is deprecated");
                return false;
            }

            //Part is available.
            return true;
        }
    }
}
