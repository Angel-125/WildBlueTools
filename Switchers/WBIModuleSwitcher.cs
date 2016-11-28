using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

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
    [KSPModule("Module Switcher")]
    public class WBIModuleSwitcher : WBIResourceSwitcher
    {
        protected List<PartModule> addedPartModules = new List<PartModule>();
        protected List<ConfigNode> moduleSettings = new List<ConfigNode>();

        private bool _showGUI = true;

        #region API
        public bool ShowGUI
        {
            get
            {
                return _showGUI;
            }

            set
            {
                _showGUI = value;
                initModuleGUI();
            }
        }
        #endregion

        #region Overrides
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            ConfigNode[] moduleNodes = node.GetNodes("WBIMODULE");
            if (moduleNodes == null)
                return;

            //Save the module settings, we'll need these for later.
            foreach (ConfigNode moduleNode in moduleNodes)
                moduleSettings.Add(moduleNode);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            ConfigNode saveNode;

            if (addedPartModules == null)
            {
                Log("addedPartModules is null");
                return;
            }

            foreach (PartModule addedModule in addedPartModules)
            {
                //Create a node for the module
                saveNode = ConfigNode.CreateConfigFromObject(addedModule);
                if (saveNode == null)
                {
                    Log("save node is null");
                    continue;
                }

                //Tell the module to save its data
                saveNode.name = "WBIMODULE";
                try
                {
                    addedModule.Save(saveNode);
                }
                catch (Exception ex)
                {
                    string exInfo = ex.ToString();
                }

                //Add it to our node
                node.AddNode(saveNode);
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;
            base.OnStart(state);
        }

        public override void OnRedecorateModule(ConfigNode templateNode)
        {
            Log("OnRedecorateModule called");

            //Load the modules
            loadModulesFromTemplate(templateNode);
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);
            string value;

            value = protoNode.GetValue("showGUI");
            if (string.IsNullOrEmpty(value) == false)
                _showGUI = bool.Parse(value);
        }

        #endregion

        #region Helpers
        protected void loadModuleSettings(PartModule module, ConfigNode moduleNode, int index)
        {
            if (HighLogic.LoadedSceneIsFlight == false && HighLogic.LoadedSceneIsEditor == false && HighLogic.LoadedScene != GameScenes.SPACECENTER)
                return;

            Log("loadModuleSettings called");
            if (index > moduleSettings.Count - 1)
            {
                Log("Index > moduleSettings.Count!");
                return;
            }
            ConfigNode nodeSettings = moduleSettings[index];

            //Add any missing settings
            foreach (ConfigNode.Value nodeValue in moduleNode.values)
            {
                if (nodeSettings.HasValue(nodeValue.name) == false)
                    nodeSettings.AddValue(nodeValue.name, nodeValue.value);
            }

            //nodeSettings may have persistent fields. If so, then set them.
            foreach (ConfigNode.Value nodeValue in nodeSettings.values)
            {
                try
                {
                    if (nodeValue.name != "name")
                        moduleNode.SetValue(nodeValue.name, nodeValue.value, true);

                    if (module.Fields[nodeValue.name] != null)
                    {
                        Log("Set Field " + nodeValue.name + " to " + nodeValue.value);
                        module.Fields[nodeValue.name].Read(nodeValue.value, module);
                    }
                }
                catch (Exception ex)
                {
                    Log("Encountered an exception while setting values for " + moduleNode.GetValue("name") + ": " + ex);
                    continue;
                }
            }

            //Actions
            if (nodeSettings.HasNode("ACTIONS"))
            {
                ConfigNode actionsNode = nodeSettings.GetNode("ACTIONS");
                BaseAction action;

                foreach (ConfigNode node in actionsNode.nodes)
                {
                    action = module.Actions[node.name];
                    if (action != null)
                    {
                        action.actionGroup = (KSPActionGroup)Enum.Parse(typeof(KSPActionGroup), node.GetValue("actionGroup"));
                        Log("Set " + node.name + " to " + action.actionGroup);
                    }
                }
            }
        }

        protected bool canLoadModule(ConfigNode node)
        {
            string value;

            //If we are in career mode, make sure we have unlocked the tech node.
            if (ResearchAndDevelopment.Instance != null)
            {
                value = node.GetValue("TechRequired");
                if (!string.IsNullOrEmpty(value) && (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
                {
                    if (ResearchAndDevelopment.GetTechnologyState(value) != RDTech.State.Available)
                        return false;
                }
            }

            //Now check for required mod
            value = node.GetValue("needs");
            if (!string.IsNullOrEmpty(value))
            {
                if (TemplateManager.CheckNeeds(value) != EInvalidTemplateReasons.TemplateIsValid)
                    return false;
            }

            return true;
        }

        protected virtual void loadModulesFromTemplate(ConfigNode templateNode)
        {
            Log("loadModulesFromTemplate called for template: " + templateNode.GetValue("name"));
            ConfigNode[] moduleNodes;
            string moduleName;
            PartModule module;

            moduleNodes = templateNode.GetNodes("MODULE");
            if (moduleNodes == null)
            {
                Log("loadModulesFromTemplate - moduleNodes is null! Cannot proceed.");
                return;
            }

            //Remove any previously added modules
            foreach (PartModule doomed in addedPartModules)
                this.part.RemoveModule(doomed);
            addedPartModules.Clear();

            //Add the modules
            foreach (ConfigNode moduleNode in moduleNodes)
            {
                try
                {
                    moduleName = moduleNode.GetValue("name");
                    Log("Checking " + moduleName);

                    //Make sure we can load the module
                    if (canLoadModule(moduleNode) == false)
                        continue;

                    //Courtesy of http://forum.kerbalspaceprogram.com/threads/27851-part-AddModule%28ConfigNode-node%29-NullReferenceException-in-PartModule-Load%28node%29-help
                    module = this.part.AddModule(moduleName);
                    if (module == null)
                        continue;

                    //Add the module to our list
                    addedPartModules.Add(module);

                    //Now wake up the module
                    module.Awake();
                    module.OnAwake();
                    module.OnActive();

                    //Load up the config
                    loadModuleSettings(module, moduleNode, addedPartModules.Count - 1);
                    module.Load(moduleNode);

                    //Start it up
                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        Log("calling module.OnStart with state: " + this.part.vessel.situation);

                        switch (this.part.vessel.situation)
                        {
                            case Vessel.Situations.ORBITING:
                                module.OnStart(PartModule.StartState.Orbital);
                                break;
                            case Vessel.Situations.LANDED:
                                module.OnStart(PartModule.StartState.Landed);
                                break;
                            case Vessel.Situations.SPLASHED:
                                module.OnStart(PartModule.StartState.Splashed);
                                break;

                            case Vessel.Situations.SUB_ORBITAL:
                                module.OnStart(PartModule.StartState.SubOrbital);
                                break;

                            case Vessel.Situations.FLYING:
                                module.OnStart(PartModule.StartState.Flying);
                                break;

                            default:
                                module.OnStart(PartModule.StartState.None);
                                break;
                        }
                    }

                    else
                    {
                        module.OnStart(PartModule.StartState.None);
                    }

                    Log("Added " + moduleName);
                }
                catch (Exception ex)
                {
                    Log("loadModulesFromTemplate encountered an error: " + ex + ". Moving on to next PartModule");
                    continue;
                }
            }//foreach

            //Clear the module settings after loading all the part modules
            moduleSettings.Clear();
        }
        #endregion
    }
}
