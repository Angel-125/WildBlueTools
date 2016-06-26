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
    public delegate void LogDelegate(object message);

    public class ExtendedPartModule : PartModule
    {
        //Nodes found in the part file's MODULE config node
        //These aren't availble after the first time the part is loaded.
        static protected Dictionary<string, ConfigNode> protoPartNodes = new Dictionary<string, ConfigNode>();

        private bool _loggingEnabled;

        #region Logging
        public virtual void Log(object message)
        {
            //            if (!_loggingEnabled)
            //                return;

            Debug.Log(this.ClassName + " [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: " + message);
        }

        public virtual void Log(object message, UnityEngine.Object context = null)
        {
            if (!_loggingEnabled)
                return;

            Debug.Log(this.ClassName + " [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: " + message, context);
        }
        #endregion

        #region Overrides

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            string protoNodeKey = getMyPartName() + this.moduleName;

            try
            {
                string value = node.GetValue("enableLogging");

                //When the part is loaded for the first time as the game starts up, we'll be reading the MODULE config node in the part's config file.
                //At that point we'll have access to all fields in the MODULE node. Later on when the part is loaded, the game doesn't load the MODULE config node.
                //Instead, we seem to load an instance of the part.
                //Let's make a copy of the config node and load it up when the part is instanced.
                if (HighLogic.LoadedScene == GameScenes.LOADING)
                {
                    Log("Looking for proto node for " + protoNodeKey);
                    if (protoPartNodes.ContainsKey(protoNodeKey) == false)
                    {
                        protoPartNodes.Add(protoNodeKey, node);
                        Log("Config node added for " + protoNodeKey);
                    }
                }
            }

            catch (Exception ex)
            {
                Log("OnLoad generated an exception: " + ex);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            string protoNodeKey = getMyPartName() + this.moduleName;
            string value;
            ConfigNode protoNode = null;
            bool foundMyPart = protoPartNodes.ContainsKey(protoNodeKey);

            //Try an alternate method to find the part
            if (!foundMyPart)
            {
                foreach (string key in protoPartNodes.Keys)
                    if (protoNodeKey.Contains(key))
                    {
                        foundMyPart = true;
                        protoNode = protoPartNodes[key];
                    }
            }

            //Logging
            if (foundMyPart)
            {
                //Get the proto config node
                if (protoNode == null)
                    protoNode = protoPartNodes[protoNodeKey];

                value = protoNode.GetValue("enableLogging");

                if (!string.IsNullOrEmpty(value))
                    _loggingEnabled = bool.Parse(value);

                //Call virtual method to let other objects get protoNode values
                getProtoNodeValues(protoNode);
            }
        }

        #endregion

        #region Helpers        
        public void FixedUpdate()
        {
            OnFixedUpdate();
        }

        protected virtual void getProtoNodeValues(ConfigNode protoNode)
        {
            //Dummy
        }

        protected string getMyPartName()
        {
            //When first loaded as the game starts, this.part.name won't have extra info tacked on such as ship name or (Clone)
            //We like this name when creating the proto node.
            string partName = this.part.name;

            //After loading, we need the pre-loaded part name, which comes from part.partInfo.
            if (this.part.partInfo != null)
                partName = this.part.partInfo.name;

            //Account for Editor
            partName = partName.Replace("(Clone)", "");

            //Strip out invalid characters
            partName = string.Join("_", partName.Split(System.IO.Path.GetInvalidFileNameChars()));

            return partName;
        }

        protected void hideAllEmitters()
        {
            KSPParticleEmitter[] emitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            if (emitters == null)
                return;

            foreach (KSPParticleEmitter emitter in emitters)
            {
                emitter.emit = false;
                emitter.enabled = false;
            }
        }

        protected void showOnlyEmittersInList(List<string> emittersToShow)
        {
            KSPParticleEmitter[] emitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            if (emitters == null)
                return;

            foreach (KSPParticleEmitter emitter in emitters)
            {
                //If the emitter is on the list then show it
                if (emittersToShow.Contains(emitter.name))
                {
                    emitter.emit = true;
                    emitter.enabled = true;
                }

                //Emitter is not on the list, hide it.
                else
                {
                    emitter.emit = false;
                    emitter.enabled = false;
                }
            }
        }

        protected void hideEmittersInList(List<string> emittersToHide)
        {
            KSPParticleEmitter[] emitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            if (emitters == null)
                return;

            foreach (KSPParticleEmitter emitter in emitters)
            {
                //If the emitter is on the list then hide it
                if (emittersToHide.Contains(emitter.name))
                {
                    emitter.emit = false;
                    emitter.enabled = false;
                }
            }
        }

        protected void showAndHideEmitters(List<string> emittersToShow, List<string> emittersToHide)
        {
            KSPParticleEmitter[] emitters = part.GetComponentsInChildren<KSPParticleEmitter>();

            if (emitters == null)
                return;

            foreach (KSPParticleEmitter emitter in emitters)
            {
                //If the emitter is on the show list then show it
                if (emittersToShow.Contains(emitter.name))
                {
                    emitter.emit = true;
                    emitter.enabled = true;
                }

                //Emitter is on the hide list, then hide it.
                else if (emittersToHide.Contains(emitter.name))
                {
                    emitter.emit = false;
                    emitter.enabled = false;
                }
            }
        }

        #endregion
    }
}
