using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

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
    public class WBIInternalModule : InternalModule
    {
        public virtual void OnFixedTimeTick()
        {
        }
    }

    public class WBIPropStateHelper : ExtendedPartModule
    {
        protected Dictionary<string, string> propModuleProperties = new Dictionary<string, string>();

        #region KSPAPI
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode saveNode;

            foreach (string key in propModuleProperties.Keys)
            {
                saveNode = ConfigNode.CreateConfigFromObject(this);
                saveNode.name = "PROPVALUE";
                saveNode.AddValue("name", key);
                saveNode.AddValue("value", propModuleProperties[key]);
                node.AddNode(saveNode);
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ConfigNode[] propValues = node.GetNodes("PROPVALUE");
            foreach (ConfigNode propValueNode in propValues)
                propModuleProperties.Add(propValueNode.GetValue("name"), propValueNode.GetValue("value"));
        }
        #endregion

        #region API
        public void SaveProperty(int propID, string property, string value)
        {
            string key = propID.ToString() + property;

            if (propModuleProperties.ContainsKey(key))
                propModuleProperties[key] = value;
            else
                propModuleProperties.Add(key, value);
        }

        public string LoadProperty(int propID, string property)
        {
            string key = propID.ToString() + property;

            if (propModuleProperties.ContainsKey(key))
                return propModuleProperties[key];
            else
                return string.Empty;
        }
        #endregion
    }
}
