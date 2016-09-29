using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2016, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIModuleWetWorkshop : PartModule
    {
        [KSPField]
        public string hideObjects = string.Empty;

        [KSPField]
        public string hideObjectsForTemplates = string.Empty;

        [KSPField(isPersistant = true)]
        public bool objectsHidden;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            WBIResourceSwitcher switcher = this.part.FindModuleImplementing<WBIResourceSwitcher>();
            if (switcher != null)
            {
                switcher.onModuleRedecorated += new ModuleRedecoratedEvent(OnModuleRedecorated);

                OnModuleRedecorated(switcher.CurrentTemplate);
            }
        }

        public void OnModuleRedecorated(ConfigNode nodeTemplate)
        {
            if (string.IsNullOrEmpty(hideObjects))
                return;
            if (string.IsNullOrEmpty(hideObjectsForTemplates))
                return;
            if (nodeTemplate.HasValue("shortName") == false)
                return;

            //See if we should hide or show the hideObjects for the current template.
            objectsHidden = hideObjectsForTemplates.Contains(nodeTemplate.GetValue("shortName"));

            showObjects(!objectsHidden);
        }

        public void showObjects(bool isVisible)
        {
            if (string.IsNullOrEmpty(hideObjects))
                return;

            char[] delimiters = { ',' };
            string[] transformNames = hideObjects.Replace(" ", "").Split(delimiters);
            Transform[] targets;

            //Sanity checks
            if (transformNames == null || transformNames.Length == 0)
            {
                Debug.Log("transformNames are null");
                return;
            }

            //Go through all the named panels and find their transforms.
            foreach (string transformName in transformNames)
            {
                //Get the targets
                targets = part.FindModelTransforms(transformName);
                if (targets == null)
                {
                    Debug.Log("No targets found for " + transformName);
                    continue;
                }

                foreach (Transform target in targets)
                {
                    target.gameObject.SetActive(isVisible);
                    Collider collider = target.gameObject.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = isVisible;
                }
            }
        }

    }
}
