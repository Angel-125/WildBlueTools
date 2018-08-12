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
    public class WBIOmniConverter : WBIModuleResourceConverterFX, IOpsView
    {
        #region Fields
        [KSPField]
        public string managedName = "OmniConverter";

        [KSPField]
        public string templateNodes = "OMNICONVERTER";

        [KSPField]
        public string templateTags = string.Empty;

        [KSPField(isPersistant = true)]
        public string currentTemplateName = string.Empty;

        [KSPField]
        public string reconfigureSkill = "ConverterSkill";

        [KSPField]
        public int reconfigureRank = 0;

        [KSPField]
        public string requiredResource = "Equipment";

        [KSPField]
        public float requiredAmount;

        [KSPField]
        public float BaseEfficiency = 1.0f;
        #endregion

        /// <summary>
        /// Title of the GUI window.
        /// </summary>
        [KSPField]
        public string opsViewTitle = "Reconfigure Converter";

        [KSPField]
        public string opsButtonLabel = "Converters";

        [KSPField]
        public bool showOpsView = false;

        #region Housekeeping
        public ConfigNode currentTemplate;
        public ConfigNode viewedTemplate;
        public int templateIndex;

        protected TemplateManager templateManager;

        private Vector2 scrollPos, scrollPosInfo;
        private string converterInfo;
        private bool confirmedReconfigure;
        private WBIAffordableSwitcher switcher;
        private WBISingleOpsView opsView;
        #endregion

        #region Overrides

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine("<b>" + managedName + "</b>");
            info.AppendLine("Inputs and outputs vary depending upon current configuration.");

            return info.ToString();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            //Setup the template manager if needed
            setupTemplateManager();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            //Setup the template manager if needed.
            setupTemplateManager();

            //Get the switcher
            switcher = this.part.FindModuleImplementing<WBIAffordableSwitcher>();
            if (switcher != null)
                switcher.OnGetReconfigureResources += GetRequiredResources;

            //GUI button
            this.Events["ToggleOpsView"].guiName = "Setup " + managedName;
            this.Events["ToggleOpsView"].active = showOpsView;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (node.HasValue("currentTemplateName"))
                node.SetValue("currentTemplateName", currentTemplateName);
        }
        #endregion

        #region Resource Conversion
        protected void setupTemplateManager()
        {
            //Create the templateManager
            if (templateManager == null)
                templateManager = new TemplateManager(this.part, this.vessel, new LogDelegate(Log), templateNodes, templateTags);

            //Get the current template
            if (string.IsNullOrEmpty(currentTemplateName))
            {
                templateIndex = 0;
            }
            else
            {
                templateIndex = templateManager.FindIndexOfTemplate(currentTemplateName, "ConverterName");
                if (templateIndex != -1)
                    currentTemplate = templateManager.templateNodes[templateIndex];
                else
                    templateIndex = 0;
            }

            //Setup the inputs and outputs
            setupConverterTemplate();
        }

        protected virtual void setupConverterTemplate()
        {
            if (currentTemplate == null)
                return;

            //ConverterName
            if (currentTemplate.HasValue("ConverterName"))
                ConverterName = currentTemplate.GetValue("ConverterName");

            //Action names
            if (currentTemplate.HasValue("StartActionName"))
                StartActionName = currentTemplate.GetValue("StartActionName");
            if (currentTemplate.HasValue("StopActionName"))
                StopActionName = currentTemplate.GetValue("StopActionName");
            if (currentTemplate.HasValue("ToggleActionName"))
                ToggleActionName = currentTemplate.GetValue("ToggleActionName");

            //AutoShutdown
            if (currentTemplate.HasValue("AutoShutdown"))
                bool.TryParse(currentTemplate.GetValue("AutoShutdown"), out AutoShutdown);

            //Specialist stuff
            if (currentTemplate.HasValue("UseSpecialistBonus"))
                bool.TryParse(currentTemplate.GetValue("UseSpecialistBonus"), out UseSpecialistBonus);
            if (UseSpecialistBonus)
            {
                if (currentTemplate.HasValue("SpecialistEfficiencyFactor"))
                    float.TryParse(currentTemplate.GetValue("SpecialistEfficiencyFactor"), out SpecialistEfficiencyFactor);

                if (currentTemplate.HasValue("SpecialistBonusBase"))
                    float.TryParse(currentTemplate.GetValue("SpecialistBonusBase"), out SpecialistBonusBase);

                if (currentTemplate.HasValue("ExperienceEffect"))
                    ExperienceEffect = currentTemplate.GetValue("ExperienceEffect");
            }

            //Efficiency
            if (currentTemplate.HasValue("EfficiencyBonus"))
                float.TryParse(currentTemplate.GetValue("EfficiencyBonus"), out EfficiencyBonus);
            EfficiencyBonus *= BaseEfficiency;

            //Clear existing lists
            inputList.Clear();
            outputList.Clear();
            reqList.Clear();

            //Inputs
            ConfigNode[] resourceNodes;
            ResourceRatio resourceRatio;
            if (currentTemplate.HasNode("INPUT_RESOURCE"))
            {
                resourceNodes = currentTemplate.GetNodes("INPUT_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    inputList.Add(resourceRatio);
                }
            }

            //Outputs
            if (currentTemplate.HasNode("OUTPUT_RESOURCE"))
            {
                resourceNodes = currentTemplate.GetNodes("OUTPUT_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    outputList.Add(resourceRatio);
                }
            }

            //Required
            if (currentTemplate.HasNode("REQUIRED_RESOURCE"))
            {
                resourceNodes = currentTemplate.GetNodes("REQUIRED_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    reqList.Add(resourceRatio);
                }
            }
        }

        #endregion

        #region Templates
        protected void previewTemplate(ConfigNode templateNode)
        {
            if (templateNode == null)
                return;
            if (!string.IsNullOrEmpty(converterInfo))
                return;
            StringBuilder info = new StringBuilder();

            //Converter Name
            if (templateNode.HasValue("ConverterName"))
                info.AppendLine("<color=lightblue><b>" + templateNode.GetValue("ConverterName") + "</b></color>");

            //Description
            info.AppendLine(" ");
            if (templateNode.HasValue("description"))
                info.AppendLine(templateNode.GetValue("description"));

            ConfigNode[] resourceNodes;
            ResourceRatio resourceRatio;
            PartResourceDefinition definition;
            if (templateNode.HasNode("INPUT_RESOURCE"))
            {
                info.AppendLine(" ");
                info.AppendLine("<color=white><b>Inputs</b></color>");
                resourceNodes = templateNode.GetNodes("INPUT_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    definition = ResourceHelper.DefinitionForResource(resourceRatio.ResourceName);
                    if (definition != null)
                    {
                        info.AppendLine("<b>" + definition.displayName + "</b>: " + formatRate(resourceRatio.Ratio));
                        info.AppendLine("Flow mode: " + resourceRatio.FlowMode.displayDescription());
                        info.AppendLine("Dumps excess: " + resourceRatio.DumpExcess);
                    }
                    else
                    {
                        info.AppendLine("No definition for " + resourceRatio.ResourceName);
                    }
                }
            }

            //Outputs
            if (templateNode.HasNode("OUTPUT_RESOURCE"))
            {
                info.AppendLine(" ");
                info.AppendLine("<color=lightblue><b>Outputs</b></color>");
                resourceNodes = templateNode.GetNodes("OUTPUT_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    definition = ResourceHelper.DefinitionForResource(resourceRatio.ResourceName);
                    if (definition != null)
                    {
                        info.AppendLine("<b>" + definition.displayName + "</b>: " + formatRate(resourceRatio.Ratio));
                        info.AppendLine("Flow mode: " + resourceRatio.FlowMode.displayDescription());
                        info.AppendLine("Dumps excess: " + resourceRatio.DumpExcess);
                    }
                    else
                    {
                        info.AppendLine("No definition for " + resourceRatio.ResourceName);
                    }
                }
            }

            //Required
            if (templateNode.HasNode("REQUIRED_RESOURCE"))
            {
                info.AppendLine("<color=orange><b>Required</b></color>");
                resourceNodes = templateNode.GetNodes("REQUIRED_RESOURCE");
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceRatio = new ResourceRatio();
                    resourceRatio.Load(resourceNodes[index]);
                    definition = ResourceHelper.DefinitionForResource(resourceRatio.ResourceName);
                    if (definition != null)
                    {
                        info.AppendLine("<b>" + definition.displayName + "</b>: " + formatRate(resourceRatio.Ratio));
                        info.AppendLine("Flow mode: " + resourceRatio.FlowMode.displayDescription());
                        info.AppendLine("Dumps excess: " + resourceRatio.DumpExcess);
                    }
                    else
                    {
                        info.AppendLine("No definition for " + resourceRatio.ResourceName);
                    }
                }
            }

            //Specialist info
            bool useBonus = false;
            if (templateNode.HasValue("UseSpecialistBonus"))
                bool.TryParse(templateNode.GetValue("UseSpecialistBonus"), out useBonus);
            if (useBonus)
            {
                info.AppendLine(" ");
                if (templateNode.HasValue("ExperienceEffect"))
                    info.AppendLine("<color=white><b>Bonus Output Skill: </b>" + templateNode.GetValue("ExperienceEffect") + "</color>");
            }

            //Set the information view string
            converterInfo = info.ToString();
        }
        #endregion

        #region IOpsView
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Setup Converter")]
        public void ToggleOpsView()
        {
            //Setup the ops view
            if (opsView == null)
            {
                opsView = new WBISingleOpsView();
                opsView.WindowTitle = opsViewTitle;
                opsView.buttonLabel = opsButtonLabel;
                opsView.opsView = this;
            }
            opsView.SetVisible(!opsView.IsVisible());
        }

        public List<string> GetButtonLabels()
        {
            List<string> buttonLabels = new List<string>();
            buttonLabels.Add(managedName);
            return buttonLabels;
        }

        public void DrawOpsWindow(string buttonLabel)
        {
            GUILayout.BeginHorizontal();

            //Current configuration
            GUILayout.BeginVertical(new GUILayoutOption[] { GUILayout.Width(250) });
            if (currentTemplate != null)
                GUILayout.Label("<color=white><b>Configuration: </b>" + currentTemplate.GetValue("ConverterName") + "</color>");
            else
                GUILayout.Label("<color=white><b>Configuration: </b>NONE</color>");

            //Reconfigure cost
            if (!string.IsNullOrEmpty(requiredResource))
                GUILayout.Label("<color=white><b>Cost: </b>" + requiredResource + string.Format("({0:f2})", requiredAmount) + "</color>");

            //Reconfigure skill
            if (!string.IsNullOrEmpty(reconfigureSkill))
                GUILayout.Label("<color=white><b>Reconfigure Trait(s): </b>" + getRequiredTraits() + "</color>");

            //Draw the converter options
            ConfigNode template;
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            for (int index = 0; index < templateManager.templateNodes.Length; index++)
            {
                template = templateManager.templateNodes[index];
                if (templateManager.TemplateTagsMatch(template))
                {
                    if (TemplateManager.TemplateTechResearched(template))
                    {
                        if (GUILayout.Button(template.GetValue("ConverterName")))
                        {
                            converterInfo = null;
                            previewTemplate(template);
                            viewedTemplate = template;
                        }
                    }
                    else
                    {
                        GUILayout.Label("<color=#959595>" + template.GetValue("ConverterName") + ":\r\nNeeds " + TemplateManager.GetTechTreeTitle(template) + "</color>");
                    }
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Reconfigure"))
            {
                //In flight mode, we need to make sure that we can afford to reconfigure the converter and that
                //the user has confirmed the operation.
                if (HighLogic.LoadedSceneIsFlight)
                {
                    //Can we afford to reconfigure the converter?
                    if (canAffordReconfigure() && hasSufficientSkill())
                    {
                        //Confirm reconfigure.
                        if (!confirmedReconfigure)
                        {
                            ScreenMessages.PostScreenMessage("Click again to confirm reconfiguration.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            confirmedReconfigure = true;
                        }

                        else
                        {
                            payForReconfigure();
                            reconfigureConverter();
                        }
                    }
                }

                //Not in flight so just reconfigure
                else
                {
                    reconfigureConverter();
                }
            }
            GUILayout.EndVertical();

            //Draw the info pane
            scrollPosInfo = GUILayout.BeginScrollView(scrollPosInfo);
            if (!string.IsNullOrEmpty(converterInfo))
                GUILayout.Label(converterInfo);
            GUILayout.EndScrollView();

            GUILayout.EndHorizontal();
        }

        public void SetParentView(IParentView parentView)
        {
        }

        public void SetContextGUIVisible(bool isVisible)
        {
        }

        public string GetPartTitle()
        {
            return this.part.partInfo.title;
        }
        #endregion

        #region Logging
        public virtual void Log(object message)
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedScene == GameScenes.LOADINGBUFFER ||
                HighLogic.LoadedScene == GameScenes.PSYSTEM || HighLogic.LoadedScene == GameScenes.SETTINGS)
                return;

            if (!WBIMainSettings.EnableDebugLogging)
                return;

            Debug.Log("[" + this.ClassName + "] - " + message);
        }
        #endregion

        #region Helpers
        //Switchers will ask us for the resources needed to reconfigure the converter.
        protected void GetRequiredResources()
        {
            if (currentTemplate == null)
                return;
            if (string.IsNullOrEmpty(requiredResource))
                return;

            if (switcher.inputList.ContainsKey(requiredResource))
                switcher.inputList[requiredResource] += requiredAmount;
            else
                switcher.inputList.Add(requiredResource, requiredAmount);
        }

        protected void payForReconfigure()
        {
            //If we don't pay to reconfigure then we're done.
            if (!WBIMainSettings.PayToReconfigure)
                return;

            //If we have a switcher, tell it to pay the cost. It might be able to ask resource distributors.
            if (switcher != null)
            {
                switcher.payForReconfigure(requiredResource, requiredAmount);
                return;
            }

            //We have to pay for it ourselves.
            this.part.RequestResource(requiredResource, requiredAmount, ResourceFlowMode.ALL_VESSEL);
        }

        protected bool hasSufficientSkill()
        {
            if (string.IsNullOrEmpty(reconfigureSkill))
                return true;

            if (WBIMainSettings.RequiresSkillCheck)
            {
                //Check the crew roster
                ProtoCrewMember[] vesselCrew = vessel.GetVesselCrew().ToArray();
                for (int index = 0; index < vesselCrew.Length; index++)
                {
                    if (vesselCrew[index].HasEffect(reconfigureSkill))
                    {
                        if (reconfigureRank > 0)
                        {
                            int crewRank = vesselCrew[index].experienceTrait.CrewMemberExperienceLevel();
                            if (crewRank >= reconfigureRank)
                                return true;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }

                //Insufficient skill
                ScreenMessages.PostScreenMessage("Insufficient skill to reconfigure the converter.");
                return false;
            }

            return true;
        }

        protected bool canAffordReconfigure()
        {
            if (string.IsNullOrEmpty(requiredResource))
                return true;
            if (!WBIMainSettings.PayToReconfigure)
                return true;

            if (switcher != null)
            {
                //If we're inflatable and not inflated then we're done.
                if (switcher.isInflatable && !switcher.isDeployed)
                    return true;

                //Ask the switcher if we can afford it. It might be able to ask distributed resource containers...
                return switcher.canAffordResource(requiredResource, requiredAmount);
            }

            //Check the available amount of the resource.
            double amountAvailable = ResourceHelper.GetTotalResourceAmount(requiredResource, this.part.vessel);
            if (amountAvailable >= requiredAmount)
                return true;

            //Can't afford it.
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition resourceDef = definitions[requiredResource];
            ScreenMessages.PostScreenMessage("Insufficient " + resourceDef.displayName + " to reconfigure the converter.");
            return false;
        }

        protected void reconfigureConverter()
        {
            currentTemplate = viewedTemplate;
            currentTemplateName = currentTemplate.GetValue("ConverterName");
            templateIndex = templateManager.FindIndexOfTemplate(currentTemplateName, "ConverterName");

            setupConverterTemplate();
        }

        string formatRate(double rate)
        {
            if (rate < 0.0001)
                return string.Format("{0:f2}/day", rate * (double)KSPUtil.dateTimeFormatter.Day);
            else if (rate < 0.01)
                return string.Format("{0:f2}/hr", rate * (double)KSPUtil.dateTimeFormatter.Hour);
            else
                return string.Format("{0:f2}/sec", rate);
        }

        protected string getRequiredTraits()
        {
            string[] traits = Utils.GetTraitsWithEffect(reconfigureSkill);
            string requiredTraits = "";

            if (traits.Length == 1)
            {
                requiredTraits = traits[0];
            }

            else if (traits.Length > 1)
            {
                for (int index = 0; index < traits.Length - 1; index++)
                    requiredTraits += traits[index] + ",";
                requiredTraits += " or " + traits[traits.Length - 1];
            }

            else
            {
                requiredTraits = reconfigureSkill;
            }

            return requiredTraits;
        }
        #endregion
    }
}
