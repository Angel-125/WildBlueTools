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
    internal struct WBITextureOption
    {
        public string diffuseMap;
        public string normalMap;
        public string displayName;
    }

    public class WBITextureSwitcher : PartModule
    {
        [KSPField()]
        public string transformName = string.Empty;

        [KSPField(isPersistant = true)]
        public int selectedIndex = 0;

        List<WBITextureOption> textureOptions = new List<WBITextureOption>();

        [KSPEvent(guiActiveEditor = true)]
        public void SetNextTexture()
        {
            int nextIndex = selectedIndex;

            //Get the next index
            nextIndex = (nextIndex + 1) % this.textureOptions.Count;
            selectedIndex = nextIndex;

            //Set the texture
            SetTexture();

            //Update symmetry parts
            WBITextureSwitcher helper;
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    helper = symmetryPart.GetComponent<WBITextureSwitcher>();
                    helper.selectedIndex = selectedIndex;
                    helper.SetTexture();
                }
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            loadTextureOptions();

            //Set the texture
            SetTexture();
        }

        public void SetTexture()
        {
            if (string.IsNullOrEmpty(transformName))
                return;
            WBITextureOption textureOption = textureOptions[selectedIndex];
            Transform[] targets;
            Texture2D texture;
            Renderer rendererMaterial;

            if (string.IsNullOrEmpty(textureOption.diffuseMap))
                return;

            //Get the targets
            targets = part.FindModelTransforms(transformName);
            if (targets == null)
            {
                Debug.Log("No targets found for " + transformName);
            }

            //Now, replace the textures in each target
            foreach (Transform target in targets)
            {
                rendererMaterial = target.GetComponent<Renderer>();

                texture = GameDatabase.Instance.GetTexture(textureOption.diffuseMap, false);
                if (texture != null)
                    rendererMaterial.material.SetTexture("_MainTex", texture);

                if (!string.IsNullOrEmpty(textureOption.normalMap))
                {
                    texture = GameDatabase.Instance.GetTexture(textureOption.normalMap, false);
                    if (texture != null)
                        rendererMaterial.material.SetTexture("_BumpMap", texture);
                }
            }

            //Get the next index to set the guiName with.
            if (textureOptions.Count > 0)
            {
                int nextIndex = (selectedIndex + 1) % this.textureOptions.Count;
                if (!string.IsNullOrEmpty(textureOptions[nextIndex].displayName))
                    Events["SetNextTexture"].guiName = textureOptions[nextIndex].displayName;
                else
                    Events["SetNextTexture"].guiName = "Next Option";
            }
        }

        protected void loadTextureOptions()
        {
            ConfigNode[] textureOptionNodes;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode node = null;
            WBITextureOption textureOption;

            textureOptions.Clear();
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        if (node.HasNode("TEXTURE"))
                        {
                            textureOptionNodes = node.GetNodes("TEXTURE");
                            foreach (ConfigNode optionNode in textureOptionNodes)
                            {
                                textureOption = new WBITextureOption();
                                if (optionNode.HasValue("displayName"))
                                    textureOption.displayName = optionNode.GetValue("displayName");
                                if (optionNode.HasValue("diffuseMap"))
                                    textureOption.diffuseMap = optionNode.GetValue("diffuseMap");
                                if (optionNode.HasValue("normalMap"))
                                    textureOption.normalMap = optionNode.GetValue("normalMap");
                                textureOptions.Add(textureOption);
                            }
                        }
                        break;
                    }
                }
            }
        }

    }
}
