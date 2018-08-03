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

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    [KSPModule("Convertible Storage")]
    public class WBIConvertibleStorage : WBIAffordableSwitcher, IOpsView
    {
        [KSPField]
        public bool fieldEVAConfigurable = true;

        protected ConvertibleStorageView storageView;

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 5.0f, guiName = "Reconfigure Storage")]
        public virtual void ReconfigureStorage()
        {
            setupStorageView(CurrentTemplateIndex);

            storageView.ToggleVisible();
        }

        public override void OnStart(StartState state)
        {
            storageView = new ConvertibleStorageView();
            base.OnStart(state);
            storageView.part = this.part;
            base.OnStart(state);
            hideEditorButtons();
            Events["ReconfigureStorage"].guiActiveUnfocused = fieldEVAConfigurable;
            Events["ReconfigureStorage"].guiActive = fieldReconfigurable;

            //Kludge: make sure that if we're deflated then dump resources.
            //This is to compensate for KIS.
            if (isInflatable && isDeployed == false)
            {
                foreach (PartResource resource in this.part.Resources)
                    resource.amount = 0;
            }

        }

        public void SetWindowTitle(string title)
        {
            storageView.WindowTitle = title;
        }

        public override void SetGUIVisible(bool isVisible)
        {
            base.SetGUIVisible(isVisible);

            hideEditorButtons();

            if (isVisible)
            {
                Events["ReconfigureStorage"].guiActiveUnfocused = fieldEVAConfigurable;
                Events["ReconfigureStorage"].guiActive = fieldReconfigurable;
                Events["ReconfigureStorage"].guiActiveEditor = fieldReconfigurable;
            }
            else
            {
                Events["ReconfigureStorage"].guiActiveUnfocused = false;
                Events["ReconfigureStorage"].guiActive = false;
                Events["ReconfigureStorage"].guiActiveEditor = false;
            }
        }

        protected override void hideEditorGUI(StartState state)
        {
            hideEditorButtons();
        }

        protected override void initModuleGUI()
        {
            base.initModuleGUI();

            hideEditorButtons();

            storageView.previewTemplate = PreviewTemplate;
            storageView.setTemplate = SwitchTemplateType;
            storageView.setupView = SetupView;
            Debug.Log("initModuleGUI() called");
            if (this.templateManager == null)
                Debug.Log("templateManager is null!");
            else
                Debug.Log("templateManager is not null at this point");
            storageView.templateManager = this.templateManager;
        }

        public override void UpdateContentsAndGui(int templateIndex)
        {
            base.UpdateContentsAndGui(templateIndex);
            hideEditorButtons();
        }

        protected void hideEditorButtons()
        {
            Events["NextType"].guiActive = false;
            Events["NextType"].guiActiveUnfocused = false;
            Events["NextType"].guiActiveEditor = false;

            Events["PrevType"].guiActive = false;
            Events["PrevType"].guiActiveUnfocused = false;
            Events["PrevType"].guiActiveEditor = false;
        }

        public void SetupView()
        {
            setupStorageView(CurrentTemplateIndex);
        }

        public void PreviewTemplate(string templateName)
        {
            //Get the template index associated with the template name
            int curTemplateIndex = templateManager.FindIndexOfTemplate(templateName);

            setupStorageView(curTemplateIndex);
        }

        public void SwitchTemplateType(string templateName)
        {
            Log("SwitchTemplateType called.");

            //Update symmetry parts
            if ((HighLogic.LoadedSceneIsFlight && storageView.updateSymmetry) || HighLogic.LoadedSceneIsEditor)
            {
                int templateIndex = templateManager.FindIndexOfTemplate(templateName);
                UpdateSymmetry(templateIndex);
            }

            //Can we use the index?
            EInvalidTemplateReasons reasonCode = templateManager.CanUseTemplate(templateName);
            if (reasonCode == EInvalidTemplateReasons.TemplateIsValid)
            {
                //If we require specific skills to perform the reconfigure, do we have sufficient skill to reconfigure it?
                if (WBIMainSettings.RequiresSkillCheck)
                {
                    if (hasSufficientSkill(templateName) == false)
                        return;
                }

                //If we have to pay to reconfigure the module, then do our checks.
                if (WBIMainSettings.PayToReconfigure)
                {
                    //Can we afford it?
                    if (canAffordReconfigure(templateName) == false)
                        return;

                    //Yup, we can afford it
                    //Pay the reconfigure cost
                    payPartsCost(templateManager.FindIndexOfTemplate(templateName));
                }

                //Update contents
                UpdateContentsAndGui(templateName);
                return;
            }

            switch (reasonCode)
            {
                case EInvalidTemplateReasons.InvalidIndex:
                    ScreenMessages.PostScreenMessage("Cannot find a suitable template.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;

                case EInvalidTemplateReasons.TechNotUnlocked:
                    ScreenMessages.PostScreenMessage("More research is required to switch to the module.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;

                default:
                    ScreenMessages.PostScreenMessage("Could not switch the module.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    break;
            }
        }

        protected void setupStorageView(int templateIndex)
        {
            //Template count
            storageView.templateCount = templateManager.templateNodes.Length;

            //Template name
            storageView.templateName = templateManager[templateIndex].GetValue("name");
            if (templateManager[templateIndex].HasValue("title"))
                storageView.templateTitle = templateManager[templateIndex].GetValue("title");

            //Required resource
            if (templateManager[templateIndex].HasValue("requiredResource"))
            {
                storageView.requiredResource = templateManager[templateIndex].GetValue("requiredResource");
            }
            else
            {
                buildInputList(CurrentTemplateName);
                if (inputList.Keys.Count > 0)
                {
                    StringBuilder resourceList = new StringBuilder();
                    foreach (string key in inputList.Keys)
                        resourceList.Append(key + string.Format(": {0:f2}; ", inputList[key]));
                    storageView.requiredResource = resourceList.ToString().TrimEnd(new char[] { ';' });
                }
            }

            //Resource cost
            if (templateManager[templateIndex].HasValue("requiredAmount"))
                storageView.resourceCost = float.Parse(templateManager[templateIndex].GetValue("requiredAmount"));
            else
                storageView.resourceCost = 0f;
            storageView.resourceCost *= materialCostModifier;

            //Required skill
            if (templateManager[templateIndex].HasValue("reconfigureSkill"))
                storageView.requiredSkill = templateManager[templateIndex].GetValue("reconfigureSkill");
            else
                storageView.requiredSkill = string.Empty;

            //Description
            storageView.info = getStorageInfo(templateIndex);

            //Decal
            string panelName;
            ConfigNode nodeTemplate = templateManager[templateIndex];

            panelName = nodeTemplate.GetValue("logoPanel");
            if (panelName != null)
                storageView.decal = GameDatabase.Instance.GetTexture(panelName, false);
            else
                storageView.decal = null;
        }

        protected string getStorageInfo(int templateIndex)
        {
            StringBuilder moduleInfo = new StringBuilder();
            StringBuilder converterInfo = new StringBuilder();
            ConfigNode nodeTemplate = templateManager[templateIndex];
            string value;
            PartModule partModule;
            bool addConverterHeader = true;
            double maxAmount = 0f;

            value = nodeTemplate.GetValue("title");
            if (!string.IsNullOrEmpty(value))
                moduleInfo.Append(value + "\r\n");

            value = nodeTemplate.GetValue("description");
            if (!string.IsNullOrEmpty(value))
                moduleInfo.Append("\r\n" + value + "\r\n");

            value = nodeTemplate.GetValue("CrewCapacity");
            if (!string.IsNullOrEmpty(value))
                moduleInfo.Append("Crew Capacity: " + nodeTemplate.GetValue("CrewCapacity") + "\r\n");

            //Add part module info
            if (nodeTemplate.nodes.Contains("MODULE"))
            {
                ConfigNode[] moduleNodes = nodeTemplate.nodes.GetNodes("MODULE");
                foreach (ConfigNode moduleNode in moduleNodes)
                {
                    if (moduleNode.GetValue("name") == "ModuleResourceConverter")
                    {
                        if (addConverterHeader)
                        {
                            converterInfo.Append("\r\n<b>Conversions</b>\r\n\r\n");
                            addConverterHeader = false;
                        }

                        partModule = this.part.AddModule("ModuleResourceConverter");
                        if (partModule != null)
                        {
                            partModule.Load(moduleNode);
                            converterInfo.Append(partModule.GetInfo());
                            converterInfo.Append("\r\n");
                            this.part.RemoveModule(partModule);
                        }
                    }

                    else
                    {
                        partModule = this.part.AddModule(moduleNode.GetValue("name"));
                        if (partModule != null)
                        {
                            partModule.Load(moduleNode);
                            moduleInfo.Append(partModule.GetInfo());
                            moduleInfo.Append("\r\n");
                            this.part.RemoveModule(partModule);
                        }
                    }
                }

                if (converterInfo.Length > 0)
                    moduleInfo.Append(converterInfo.ToString());
            }

            //Resources
            if (nodeTemplate.nodes.Contains("RESOURCE"))
            {
                ConfigNode[] resources = nodeTemplate.GetNodes("RESOURCE");
                foreach (ConfigNode node in resources)
                    Debug.Log("template resource: " + node.GetValue("name"));
                if (resources.Length > 0)
                {
                    if (isInflatable)
                        moduleInfo.Append("\r\n<b>Resources (deployed)</b>\r\n\r\n");
                    else
                        moduleInfo.Append("\r\n<b>Resources</b>\r\n\r\n");

                    foreach (ConfigNode resourceNode in resources)
                    {
                        maxAmount = double.Parse(resourceNode.GetValue("maxAmount")) * capacityFactor;

                        moduleInfo.Append(string.Format("{0:s}: {1:f2}\r\n", resourceNode.GetValue("name"), maxAmount));
                    }
                }
            }

            return moduleInfo.ToString();
        }

        #region IOpsView
        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }

        public virtual void SetContextGUIVisible(bool isVisible)
        {
            SetGUIVisible(isVisible);
            Events["ReconfigureStorage"].guiActive = fieldReconfigurable;
            Events["ReconfigureStorage"].guiActiveUnfocused = fieldEVAConfigurable;
            Events["ReconfigureStorage"].guiActiveEditor = true;
        }

        public virtual void SetParentView(IParentView parentView)
        {
        }

        public virtual List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();
            buttonLabels.Add("Config");
            return buttonLabels;
        }

        public virtual void DrawOpsWindow(string buttonLabel)
        {
            storageView.part = this.part;
            storageView.DrawView();
        }
        #endregion
    }
}
