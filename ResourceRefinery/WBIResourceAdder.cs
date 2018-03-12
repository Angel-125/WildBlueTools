using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Portions of this software use code from the Firespitter plugin by Snjo, used with permission. Thanks Snjo for sharing how to switch meshes. :)

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    //Adds resources specified by RESOURCE nodes to the part when the part is created.
    //This is done to avoid issues with negative part cost that can happen when expensive resources
    //are added to the part but their amount is set to zero to start.
    public class WBIResourceAdder : PartModule, IPartCostModifier
    {
        public float totalResourceCost = 0f;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsEditor == false && HighLogic.LoadedSceneIsFlight == false)
                return;

            AddResourceNodes();
        }

        public virtual void AddResourceNodes()
        {
            if (this.part.partInfo.partConfig == null)
                return;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode adderNode = null;
            ConfigNode node = null;
            string moduleName;
            string resourceName;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceList resources = part.Resources;
            PartResourceDefinition resourceDef;
            double maxAmount = 0f;

            //Get the switcher config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        adderNode = node;
                        break;
                    }
                }
            }
            if (adderNode == null)
                return;

            //Get the nodes we're interested in
            nodes = adderNode.GetNodes("RESOURCE");

            //Get the option names
            totalResourceCost = 0f;
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name") && node.HasValue("amount") && node.HasValue("maxAmount"))
                {
                    //Get name. If the resource already exists then continue.
                    resourceName = node.GetValue("name");

                    //Get max amount
                    if (node.HasValue("maxAmount"))
                    {
                        if (!double.TryParse(node.GetValue("maxAmount"), out maxAmount))
                            maxAmount = 0f;
                    }

                    //Tally up the cost
                    resourceDef = definitions[resourceName];
                    if (resourceDef != null)
                        totalResourceCost += (float)(resourceDef.unitCost * maxAmount);

                    //Add the resource
                    if (!this.part.Resources.Contains(resourceName))
                        this.part.Resources.Add(node);
                }
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return totalResourceCost;
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return ModifierChangeWhen.CONSTANTLY;
            else
                return ModifierChangeWhen.FIXED;
        }
    }
}
