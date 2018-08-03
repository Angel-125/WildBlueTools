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
    public delegate void DeployStateChangedEvent(bool deployed);
    public delegate void ModuleRedecoratedEvent(ConfigNode templateNode);
    public delegate void ResourcesDumpedEvent();

    [KSPModule("Resource Switcher")]
    public class WBIResourceSwitcher : WBIInflatablePartModule, IPartCostModifier, IPartMassModifier
    {
        private static string MAIN_TEXTURE = "_MainTex";
        private static string EMISSIVE_TEXTURE = "_Emissive";

        [KSPField(isPersistant = true)]
        public float currentVolume;

        //Name of the template nodes.
        [KSPField()]
        public string templateNodes = string.Empty;

        //Name of the template types allowed
        [KSPField()]
        public string templateTags = string.Empty;

        //List of resources that we must keep when performing a template switch.
        [KSPField()]
        public string resourcesToKeep = string.Empty;

        //Used when, say, we're in the editor, and we don't get no game-saved values from perisistent.
        [KSPField()]
        public string defaultTemplate = string.Empty;

        //Base amount of volume the part stores, if any.
        [KSPField()]
        public float baseStorage;

        [KSPField()]
        public float maxStorage;

        [KSPField(isPersistant = true)]
        public bool decalsVisible;

        [KSPField(isPersistant = true)]
        public bool fillToMaxInEditor = true;

        //Since not all storage containers are equal, the
        //capacityFactor is used to determine how much of the template's base resource amount
        //applies to the container.
        [KSPField(isPersistant = true)]
        public float capacityFactor = 0f;

        //Resources added by an omni storage
        [KSPField(isPersistant = true)]
        public string omniStorageResources = string.Empty;

        //Events
        public event ModuleRedecoratedEvent onModuleRedecorated;
        public event ResourcesDumpedEvent onResourcesDumped;
        public event DeployStateChangedEvent onDeployStateChanged;

        [KSPField(isPersistant = true)]
        public float partMass = 0f;

        //Index of the current module template we're using.
        public int CurrentTemplateIndex;

        //Determines whether or not the resource container can be reconfigured in the field.
        public bool fieldReconfigurable = false;

        //Decal names (these are the names of the graphics assets, including file path)
        protected string logoPanelName;
        protected string glowPanelName;

        //Name of the transform(s) for the colony decal.
        //These names come from the model itself.
        [KSPField()]
        public string logoPanelTransforms = string.Empty;

        //Helper objects
        public Dictionary<string, double> resourceMaxAmounts = new Dictionary<string, double>();
        protected string techRequiredToReconfigure;
        protected string capacityFactorTypes;
        protected bool confirmResourceSwitch = false;
        protected bool deflateConfirmed = false;
        protected bool dumpConfirmed = false;
        protected int originalCrewCapacity;
        protected TemplateManager templateManager;
        protected Dictionary<string, ConfigNode> parameterOverrides = new Dictionary<string, ConfigNode>();
        protected List<PartResource> templateResources = new List<PartResource>();
        private bool _switchClickedOnce = false;
        protected Dictionary<string, double> keptResources = null;

        #region Display Fields
        //We use this field to identify the template config node as well as have a GUI friendly name for the user.
        //When the module starts, we'll use the templateName to find the template and get the info we need.
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Configuration")]
        public string templateName;

        #endregion

        #region User Events & API
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Dump Resources", guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public virtual void DumpResources()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (dumpConfirmed == false)
                {
                    ScreenMessages.PostScreenMessage("Existing resources will be removed. Click a second time to confirm resource dump.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    dumpConfirmed = true;
                    return;
                }

                dumpConfirmed = false;
            }

            foreach (PartResource resource in this.part.Resources)
            {
                if (resource.resourceName != "ElectricCharge" && resource.flowState)
                    resource.amount = 0;
            }

            if (onResourcesDumped != null)
                onResourcesDumped();
        }

        [KSPAction("Dump Resources")]
        public void DumpResourcesAction(KSPActionParam param)
        {
            DumpResources();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Toggle Decals")]
        public void ToggleDecals()
        {
            WBIResourceSwitcher switcher;

            decalsVisible = !decalsVisible;

            ShowDecals(decalsVisible);

            //Handle symmetrical parts
            if (HighLogic.LoadedSceneIsEditor)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    switcher = symmetryPart.GetComponent<WBIResourceSwitcher>();
                    switcher.ShowDecals(decalsVisible);
                }
            }
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Next Type", active = true, externalToEVAOnly = false, unfocusedRange = 3.0f, guiActiveUnfocused = true)]
        public void NextType()
        {
            if (confirmResourceSwitch && HighLogic.LoadedSceneIsFlight)
            {
                if (_switchClickedOnce == false)
                {
                    ScreenMessages.PostScreenMessage("Existing resources will be removed. Click a second time to confirm switch.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    _switchClickedOnce = true;
                    return;
                }

                _switchClickedOnce = false;
            }

            int templateIndex = templateManager.GetNextUsableIndex(CurrentTemplateIndex);

            if (templateIndex != -1)
            {
                string nextName = templateManager[templateIndex].GetValue("name");
                if (canAffordReconfigure(nextName) && hasSufficientSkill(nextName))
                    payPartsCost(templateIndex);
                else
                    return;
                UpdateContentsAndGui(templateIndex);
                UpdateSymmetry(templateIndex);
                return;
            }

            //If we reach here then something went wrong.
            ScreenMessages.PostScreenMessage("Unable to find a template to switch to.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Prev Type", active = true, externalToEVAOnly = false, unfocusedRange = 3.0f, guiActiveUnfocused = true)]
        public void PrevType()
        {
            if (confirmResourceSwitch && HighLogic.LoadedSceneIsFlight)
            {
                if (_switchClickedOnce == false)
                {
                    ScreenMessages.PostScreenMessage("Existing resources will be removed. Click a second time to confirm switch.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    _switchClickedOnce = true;
                    return;
                }

                _switchClickedOnce = false;
            }

            int templateIndex = templateManager.GetPrevUsableIndex(CurrentTemplateIndex);

            if (templateIndex != -1)
            {
                string prevName = templateManager[templateIndex].GetValue("name");
                if (canAffordReconfigure(prevName) && hasSufficientSkill(prevName))
                    payPartsCost(templateIndex);
                else
                    return;
                UpdateContentsAndGui(templateIndex);
                UpdateSymmetry(templateIndex);
                return;
            }

            //If we reach here then something went wrong.
            ScreenMessages.PostScreenMessage("Unable to find a template to switch to.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
        }

        public void DumpResourcesToKeep()
        {            
            foreach (PartResource resource in this.part.Resources)
            {
                if (resource.resourceName == "ElectricCharge" || resourcesToKeep.Contains(resource.resourceName))
                    resource.amount = 0;
            }
        }

        public void MaxResources()
        {
            foreach (PartResource resource in this.part.Resources)
                resource.amount = resource.maxAmount;
        }

        public void ReloadTemplate()
        {
            if (CurrentTemplateIndex != -1)
            {
                UpdateContentsAndGui(CurrentTemplateIndex);
                UpdateSymmetry(CurrentTemplateIndex);
            }
        }

        public string CurrentTemplateName
        {
            get
            {
                ConfigNode currentTemplate = templateManager[CurrentTemplateIndex];

                if (currentTemplate != null)
                    return currentTemplate.GetValue("name");
                else
                    return "Unknown";
            }
        }

        public ConfigNode CurrentTemplate
        {
            get
            {
                return templateManager[CurrentTemplateIndex];
            }
        }

        public virtual void UpdateContentsAndGui(string templateName)
        {
            int index = templateManager.FindIndexOfTemplate(templateName);

            UpdateContentsAndGui(index);
        }

        public virtual void UpdateSymmetry(int templateIndex)
        {
            WBIResourceSwitcher resourceSwitcher;

            foreach (Part symmetryPart in this.part.symmetryCounterparts)
            {
                resourceSwitcher = symmetryPart.GetComponent<WBIResourceSwitcher>();
                resourceSwitcher.UpdateContentsAndGui(templateIndex);
            }

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
        }

        public virtual void UpdateContentsAndGui(int templateIndex)
        {
            string name;
            if (templateManager.templateNodes == null)
            {
                Log("NextModuleType templateNodes == null!");
                return;
            }

            //Make sure we have a valid index
            if (templateIndex == -1)
                return;

            //Ok, we're good
            CurrentTemplateIndex = templateIndex;

            //Set the current template name
            templateName = templateManager[templateIndex].GetValue("name");
            if (string.IsNullOrEmpty(templateName))
                return;

            //Change the toggle buttons' names
            templateIndex = templateManager.GetNextUsableIndex(CurrentTemplateIndex);
            if (templateIndex != -1 && templateIndex != CurrentTemplateIndex)
            {
                name = templateManager[templateIndex].GetValue("name");
                Events["NextType"].guiName = "Next: " + name;
            }

            templateIndex = templateManager.GetPrevUsableIndex(CurrentTemplateIndex);
            if (templateIndex != -1 && templateIndex != CurrentTemplateIndex)
            {
                name = templateManager[templateIndex].GetValue("name");
                Events["PrevType"].guiName = "Prev: " + name;
            }

            //Set up the module in its new configuration
            RedecorateModule();

            //Update the resource panel
            MonoUtilities.RefreshContextWindows(this.part);
        }

        public virtual void RedecorateModule(bool loadTemplateResources = true)
        {
            double maxAmount = 0;
            string resourceName = "";
            float capacityModifier = capacityFactor;

            try
            {
                Log("RedecorateModule called. loadTemplateResources: " + loadTemplateResources.ToString() + " template index: " + CurrentTemplateIndex);
                if (templateManager == null)
                    return;
                if (templateManager.templateNodes == null)
                    return;

                ConfigNode nodeTemplate = templateManager[CurrentTemplateIndex];
                if (nodeTemplate == null)
                    return;

                if (nodeTemplate.HasValue("capacityFactor"))
                    capacityModifier = float.Parse(nodeTemplate.GetValue("capacityFactor"));

                if (nodeTemplate.HasValue("mass"))
                    partMass = float.Parse(nodeTemplate.GetValue("mass"));

                //Get max resource amounts if the part is inflatable.
                if (isInflatable)
                {
                    //Clear our max amounts dictionary
                    resourceMaxAmounts.Clear();

                    //Get all the resources in the template and add their max amounts
                    ConfigNode[] templateResourceNodes = nodeTemplate.GetNodes("RESOURCE");
                    if (templateResourceNodes != null)
                    {
                        //Set the max amounts into our dictionary.
                        foreach (ConfigNode resourceNode in templateResourceNodes)
                        {
                            resourceName = resourceNode.GetValue("name");
                            maxAmount = double.Parse(resourceNode.GetValue("maxAmount")) * capacityModifier;

                            if (resourceMaxAmounts.ContainsKey(resourceName) == false)
                                resourceMaxAmounts.Add(resourceName, maxAmount);
                        }
                    }
                }

                //Crew capacity
                //The part itself has an inflated/deflated crew capacity, as do certain templates.
                //Priority goes to inflated parts and their crew capacities.
                if (!isInflatable && nodeTemplate.HasValue("CrewCapacity"))
                {
                    this.part.CrewCapacity = int.Parse(nodeTemplate.GetValue("CrewCapacity"));
                    this.part.CheckTransferDialog();
                    if (this.part.CrewCapacity == 0 && originalCrewCapacity > 0 && HighLogic.LoadedSceneIsFlight)
                    {
                        this.part.DespawnIVA();
                    }
                }
                else if (!isInflatable && this.part.CrewCapacity != originalCrewCapacity)
                {
                    this.part.CrewCapacity = originalCrewCapacity;
                    this.part.CheckTransferDialog();
                    if (this.part.CrewCapacity > 0 && HighLogic.LoadedSceneIsFlight)
                    {
                        this.part.SpawnIVA();
                    }
                }

                //Load the template resources into the module.
                OnEditorAttach();
                if (loadTemplateResources)
                    loadResourcesFromTemplate(nodeTemplate);
                else
                    updateResourcesFromTemplate(nodeTemplate);

                //Hide template type buttons?
                if (templateManager.templateNodes.Length == 1)
                {
                    Events["PrevType"].guiActiveUnfocused = false;
                    Events["PrevType"].guiActiveEditor = false;
                    Events["PrevType"].guiActive = false;

                    Events["NextType"].guiActiveUnfocused = false;
                    Events["NextType"].guiActiveEditor = false;
                    Events["NextType"].guiActive = false;
                }

                else if (templateManager.templateNodes.Length >= 4)
                {
                    Events["NextType"].guiActiveUnfocused = true;
                    Events["NextType"].guiActiveEditor = true;
                    Events["NextType"].guiActive = true;

                    Events["PrevType"].guiActive = true;
                    Events["PrevType"].guiActiveEditor = true;
                    Events["PrevType"].guiActiveUnfocused = true;
                }

                else
                {
                    Events["NextType"].guiActiveUnfocused = true;
                    Events["NextType"].guiActiveEditor = true;
                    Events["NextType"].guiActive = true;

                    Events["PrevType"].guiActiveUnfocused = false;
                    Events["PrevType"].guiActiveEditor = false;
                    Events["PrevType"].guiActive = false;
                }

                //Call the OnRedecorateModule method to give others a chance to do stuff
                OnRedecorateModule(nodeTemplate);

                //Finally, change the decals on the part.
                updateDecalsFromTemplate(nodeTemplate);

                if (onModuleRedecorated != null)
                    onModuleRedecorated(nodeTemplate);

                adjustKeptResources(nodeTemplate);
                Log("Module redecorated.");
            }
            catch (Exception ex)
            {
                Log("RedecorateModule encountered an ERROR: " + ex);
            }
        }

        protected void findKeptResources()
        {
            //If we have resources to keep, keep track of them.
            if (string.IsNullOrEmpty(resourcesToKeep) == false)
            {
                ConfigNode[] resourceNodes = this.part.partInfo.partConfig.GetNodes("RESOURCE");
                string resourceName;
                for (int index = 0; index < resourceNodes.Length; index++)
                {
                    resourceName = resourceNodes[index].GetValue("name");
                    if (resourcesToKeep.Contains(resourceName))
                    {
                        if (keptResources == null)
                            keptResources = new Dictionary<string, double>();

                        if (keptResources.ContainsKey(resourceName) == false)
                            keptResources.Add(resourceName, double.Parse(resourceNodes[index].GetValue("maxAmount")));
                    }
                }
            }
        }

        protected virtual void adjustKeptResources(ConfigNode nodeTemplate)
        {
            if (string.IsNullOrEmpty(resourcesToKeep))
                return;

            double maxAmount = 0;
            Dictionary<string, ConfigNode> configResources = new Dictionary<string, ConfigNode>();
            ConfigNode[] nodeResources = null;
            PartResource resource = null;

            //Compile the list of resources added by the template
            nodeResources = nodeTemplate.GetNodes("RESOURCE");
            foreach (ConfigNode node in nodeResources)
                configResources.Add(node.GetValue("name"), node);

            //We've had a redecoration event. If we have kept resources and they've been modified
            //(like we keep monopropellant and have switched to a template that has monopropellant)
            //make sure to combine the totals. By the same token, make sure that the kept resources
            //are at the proper levels.
            foreach (string key in keptResources.Keys)
            {
                maxAmount = keptResources[key];

                //If the template contains a kept resouce then increase max amount
                if (configResources.ContainsKey(key))
                {
                    resource = this.part.Resources[key];
                    resource.maxAmount = maxAmount + (double.Parse(configResources[key].GetValue("maxAmount")) * capacityFactor);
                    resourceMaxAmounts[key] = resource.maxAmount;

                    //Adjust for inflatables.
                    if (isInflatable && isDeployed == false)
                        resource.maxAmount = 0;

                    else if (HighLogic.LoadedSceneIsEditor)
                        resource.amount = resource.maxAmount;
                }

                //template does not contain a kept resource
                else
                {
                    resource = this.part.Resources[key];
                    resource.maxAmount = maxAmount;
                    resourceMaxAmounts[key] = resource.maxAmount;

                    //Adjust for inflatables.
                    if (isInflatable && isDeployed == false)
                        resource.maxAmount = 0;

                    else if (resource.amount > resource.maxAmount)
                        resource.amount = resource.maxAmount;
                }
            }
        }

        public void RemoveAllResources()
        {
            List<PartResource> doomedResources = new List<PartResource>();
            foreach (PartResource res in this.part.Resources)
            {
                if (string.IsNullOrEmpty(resourcesToKeep))
                    doomedResources.Add(res);

                else if (resourcesToKeep.Contains(res.resourceName) == false)
                    doomedResources.Add(res);
            }

            foreach (PartResource doomed in doomedResources)
            {
                ResourceHelper.RemoveResource(doomed.resourceName, this.part);
            }
            templateResources.Clear();
        }

        #endregion

        #region Module Overrides
        public override string GetInfo()
        {
            return "Check the tweakables menu for the different resources that the tank can hold.";
        }

        public override void ToggleInflation()
        {
            Log("ToggleInflation called.");
            DictionaryValueList<int, PartResource> resourceList = this.part.Resources.dict;
            PartModule inventory = this.part.Modules["ModuleKISInventory"];

            //If the module cannot be deflated then exit.
            if (CanBeDeflated() == false)
            {
                Log("ToggleInflation: Not deflating module.");
                return;
            }
            
            base.ToggleInflation();
            deflateConfirmed = false;

            //If the module is now inflated, re-add the max resource amounts to the list of resources.
            //If it isn't inflated, set max amount to 1.
            if (HighLogic.LoadedSceneIsFlight || (HighLogic.LoadedSceneIsEditor && fillToMaxInEditor))
            {
                foreach (PartResource resource in resourceList.Values)
                {
                    //If we are deployed then reset the max amounts.
                    if (isDeployed)
                    {
                        if (resourceMaxAmounts.ContainsKey(resource.resourceName))
                        {
                            resource.amount = 0;
                            resource.maxAmount = resourceMaxAmounts[resource.resourceName];
                        }
                    }

                    //No longer deployed, should we preserve the resource?
                    else if (string.IsNullOrEmpty(resourcesToKeep) == false)
                    {
                        if (resourcesToKeep.Contains(resource.resourceName) == false)
                        {
                            resource.amount = 0;
                            resource.maxAmount = 1;
                        }
                    }

                    //No longer deployed
                    else
                    {
                        resource.amount = 0;
                        resource.maxAmount = 1;
                    }
                }
            }

            //KIS container
            if (inventory != null)
            {
                if (isDeployed)
                {
                    //Check to see if the current template is a KIS template. If not then set KIS amount to the base amount.
                    string value = CurrentTemplate.GetValue("isKISInventory");
                    bool isKISInventory = false;
                    if (string.IsNullOrEmpty(value) == false)
                        isKISInventory = bool.Parse(value);
                    if (isKISInventory)
                        currentVolume = maxStorage;
                    else
                        currentVolume = baseStorage;

                    Utils.SetField("maxVolume", currentVolume, inventory);
                }
                else
                {
                    Utils.SetField("maxVolume", 1, inventory);
                }
            }

            //Fire event
            if (onDeployStateChanged != null)
                onDeployStateChanged(isDeployed);
        }

        public virtual bool HasResources()
        {
            DictionaryValueList<int, PartResource> resourceList = this.part.Resources.dict;

            if (HighLogic.LoadedSceneIsEditor == false)
            {
                foreach (PartResource res in resourceList.Values)
                {
                    if (res.amount > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual bool CanBeDeflated()
        {
            if (HighLogic.LoadedSceneIsEditor == false)
            {
                //If the module is inflatable, deployed, and has kerbals inside, then don't allow the module to be deflated.
                if (isInflatable && isDeployed && this.part.protoModuleCrew.Count() > 0)
                {
                    Log("CanBeDeflated: Module has crew aboard, cannot be deflated.");
                    ScreenMessages.PostScreenMessage(this.part.partName + " has crew aboard. Vacate the module before deflating it.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }

                //If the module is inflatable, deployed, has resources, and user hasn't confirmed yet, then get confirmation that user wants to deflate the module.
                if (HasResources() && isDeployed && isInflatable && deflateConfirmed == false)
                {
                    Log("CanBeDeflated: Resources detected, requesting confirmation to delfate the module.");
                    deflateConfirmed = true;
                    ScreenMessages.PostScreenMessage(this.part.partName + " has resources. Click again to confirm module deflation.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    return false;
                }
            }

            Log("CanBeDeflated: Module can be deflated.");
            return true;
        }

        public virtual void OnRedecorateModule(ConfigNode nodeTemplate)
        {
            //Dummy method
        }

        public virtual void OnEditorAttach()
        {
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            Log("OnLoad: " + this.part.partName + " " + node + " Scene: " + HighLogic.LoadedScene.ToString());

            //Watch for the editor attach event
            this.part.OnEditorAttach += OnEditorAttach;

            //Create the templateManager
            templateManager = new TemplateManager(this.part, this.vessel, new LogDelegate(Log), templateNodes, templateTags);
        }

        public override void OnStart(PartModule.StartState state)
        {
            bool loadTemplateResources = this.part.Resources.Count > 0 ? false : true;// templateResources.Count<PartResource>() > 0 ? false : true;
            base.OnStart(state);
            Log("OnStart - State: " + state + "  Part: " + getMyPartName());

            findKeptResources();
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            //Original crew capacity
            originalCrewCapacity = this.part.CrewCapacity;

            //Initialize the templates
            initTemplates();

            //Hide GUI only shown in the editor
            hideEditorGUI(state);

            //Since the module will be loaded as it was originally created, we won't have 
            //the proper decals and converter settings when the module and part are loaded in flight.
            //Thus, we must redecorate to configure the module and part correctly.
            //When we do, we don't make the player pay for the redecoration, and we want to preserve
            //the part's existing resources, not to mention the current settings for the converters.
            //Also, if we have converters already then we've loaded their states during the OnLoad method call.
            RedecorateModule(loadTemplateResources);

            //Init the module GUI
            initModuleGUI();

            if (string.IsNullOrEmpty(logoPanelTransforms))
            {
                Events["ToggleDecals"].guiActive = false;
                Events["ToggleDecals"].guiActiveEditor = false;
                Events["ToggleDecals"].guiActiveUnfocused = false;
            }
            else
            {
                ShowDecals(decalsVisible);
            }

            //Setup KISInventory if any
            setupKISInventory(CurrentTemplate);
        }

        #endregion

        #region Helpers
        protected virtual void updateResourcesFromTemplate(ConfigNode nodeTemplate)
        {
            string templateTags = nodeTemplate.GetValue("templateTags");
            PartResource resource = null;
            string value;
            float capacityModifier = capacityFactor;
            ConfigNode[] templateResourceNodes = nodeTemplate.GetNodes("RESOURCE");
            StringBuilder resourceNameBuilder = new StringBuilder();
            if (templateResourceNodes == null)
            {
                Log(nodeTemplate.GetValue("name") + " has no resources.");
                return;
            }

            Log("updateResourcesFromTemplate called for template: " + nodeTemplate.GetValue("name"));
            Log("template: " + nodeTemplate);
            Log("capacityFactor: " + capacityFactor);
            Log("template resource count: " + templateResourceNodes.Length);
            foreach (ConfigNode resourceNode in templateResourceNodes)
            {
                //If we kept the resource, then skip this template resource.
                //We won't know what the original values were if we merged values.
                value = resourceNode.GetValue("name");
                resourceNameBuilder.Append(value);
                if (this.part.Resources.Contains(value))
                    continue;

                resource = this.part.AddResource(resourceNode);
                Log("Added resource: " + resource.resourceName);

                //Apply the capacity factor
                if (HighLogic.LoadedSceneIsEditor && fillToMaxInEditor == true)
                    resource.amount *= capacityModifier;
                else
                    resource.amount = 0f;

                //Some templates don't apply the capaictyFactor
                //First, if we have no capacityFactorTypes entry, then apply the capacityFactor. This is for backwards compatibility.
                if (string.IsNullOrEmpty(capacityFactorTypes))
                    resource.maxAmount *= capacityModifier;

                //Next, if the capacityFactorTypes contains the template type then apply the capacity factor.
                else if (capacityFactorTypes.Contains(templateTags))
                    resource.maxAmount *= capacityModifier;

                //If we aren't deployed then set the current and max amounts
                if (isDeployed == false && isInflatable)
                {
                    resource.maxAmount = 1.0f;
                    resource.amount = 0f;
                }

                templateResources.Add(resource);
                resource.isTweakable = true;
            }

            //Clear the resources that shouldn't be there.
            List<PartResource> doomedResources = new List<PartResource>();
            string templateResourceNames = resourceNameBuilder.ToString();
            foreach (PartResource res in this.part.Resources)
            {
                //If the resource isn't in our template but it's one we should keep, then skip it.
                if (!string.IsNullOrEmpty(resourcesToKeep))
                {
                    if (resourcesToKeep.Contains(res.resourceName))
                        continue;
                }
                if (!string.IsNullOrEmpty(omniStorageResources))
                {
                    if (omniStorageResources.Contains(res.resourceName))
                        continue;
                }

                //If the resource isn't in our template then drop it.
                if (templateResourceNames.Contains(res.resourceName) == false)
                    doomedResources.Add(res);
            }
            foreach (PartResource doomed in doomedResources)
            {
                ResourceHelper.RemoveResource(doomed.resourceName, this.part);
            }
            Log("Extraneous resources cleared.");
        }

        protected virtual void setupKISInventory(ConfigNode nodeTemplate)
        {
            //KIS templates work differently. We have to know the part's base and max volume.
            //Base volume represents how much can be stored when not using a KIS template.
            //Max volume represents how much can be stored when the part is configured as a KIS storage container.
            //First, do we even have an inventory?
            if (this.part.Modules.Contains("ModuleKISInventory") == false)
                return;
            PartModule inventory = this.part.Modules["ModuleKISInventory"];

            //Storage volume
            float storageVolume = 0f;
            if (nodeTemplate.HasValue("storageVolume"))
                storageVolume = float.Parse(nodeTemplate.GetValue("storageVolume"));

            //Ok, is the template a KIS template?
            if (nodeTemplate.HasValue("isKISInventory"))
            {
                //If we are an inflatable module and inflated, set the base amount. Otherwise, set it to 1
                if (isInflatable)
                {
                    if (isDeployed && storageVolume > 0f)
                    {
                        Utils.SetField("maxVolume", storageVolume, inventory);
                    }

                    else if (isDeployed)
                    {
                        Utils.SetField("maxVolume", maxStorage, inventory);
                    }

                    else
                    {
                        Utils.SetField("maxVolume", 1, inventory);
                    }
                    return;

                }

                //It's not inflatable.
                else if (storageVolume > 0f)
                {
                    Utils.SetField("maxVolume", storageVolume, inventory);
                }
                else
                {
                    Utils.SetField("maxVolume", maxStorage, inventory);
                }
            }

            //Not a KIS inventory
            else if (baseStorage > 0f)
            {
                Utils.SetField("maxVolume", baseStorage, inventory);
            }
        }

        public virtual void loadResourcesFromTemplate(ConfigNode nodeTemplate)
        {
            PartResource resource = null;
            string value;
            string templateTags = nodeTemplate.GetValue("templateTags");
            float capacityModifier = capacityFactor;

            Log("loadResourcesFromTemplate called for template: " + nodeTemplate.GetValue("name"));
            Log("template: " + nodeTemplate);
            Log("capacityFactor: " + capacityFactor);
            ConfigNode[] templateResourceNodes = nodeTemplate.GetNodes("RESOURCE");
            if (templateResourceNodes == null)
            {
                Log(nodeTemplate.GetValue("name") + " has no resources.");
                return;
            }

            //Clear the list
            Log("Clearing resource list, should keep: " + resourcesToKeep);
            List<PartResource> doomedResources = new List<PartResource>();
            foreach (PartResource res in this.part.Resources)
            {
                if (string.IsNullOrEmpty(resourcesToKeep))
                    doomedResources.Add(res);

                else if (resourcesToKeep.Contains(res.resourceName) == false)
                    doomedResources.Add(res);
            }

            foreach (PartResource doomed in doomedResources)
            {
                ResourceHelper.RemoveResource(doomed.resourceName, this.part);
            }
            templateResources.Clear();
            Log("Resources cleared");

            //Set capacityModifier if there is an override for the template
            if (nodeTemplate.HasValue("capacityFactor"))
                capacityModifier = float.Parse(nodeTemplate.GetValue("capacityFactor"));

            value = nodeTemplate.GetValue("name");
            if (parameterOverrides.ContainsKey(value))
            {
                ConfigNode templateOverride = parameterOverrides[value];
                if (templateOverride != null)
                {
                    value = templateOverride.GetValue("capacityFactor");
                    if (string.IsNullOrEmpty(value) == false)
                        capacityModifier = float.Parse(value);
                }
            }

            //Add resources from template
            Log("template resource count: " + templateResourceNodes.Length);
            Log("capacityModifier: " + capacityModifier);
            foreach (ConfigNode resourceNode in templateResourceNodes)
            {
                //If we kept the resource, then skip this template resource.
                //We won't know what the original values were if we merged values.
                value = resourceNode.GetValue("name");
                if (this.part.Resources.Contains(value))
                    continue;

                resource = this.part.AddResource(resourceNode);
                Log("Added resource: " + resource.resourceName);

                //Apply the capacity factor
                if (HighLogic.LoadedSceneIsEditor && fillToMaxInEditor == true)
                    resource.amount *= capacityModifier;
                else
                    resource.amount = 0f;

                //Some templates don't apply the capaictyFactor
                //First, if we have no capacityFactorTypes entry, then apply the capacityFactor. This is for backwards compatibility.
                if (string.IsNullOrEmpty(capacityFactorTypes))
                    resource.maxAmount *= capacityModifier;

                //Next, if the capacityFactorTypes contains the template type then apply the capacity factor.
                else if (capacityFactorTypes.Contains(templateTags))
                    resource.maxAmount *= capacityModifier;

                //If we aren't deployed then set the current and max amounts
                if (isDeployed == false && isInflatable)
                {
                    resource.maxAmount = 1.0f;
                    resource.amount = 0f;
                }

                templateResources.Add(resource);
                resource.isTweakable = true;
            }


            //Setup inventory
            setupKISInventory(nodeTemplate);
        }

        protected void updateDecalsFromTemplate(ConfigNode nodeTemplate)
        {
            string value;

            value = nodeTemplate.GetValue("name");
            if (!string.IsNullOrEmpty(templateName))
            {
                //Set templateName
                templateName = value;
                Log("New name: " + templateName);

                //Logo panel
                if (parameterOverrides.ContainsKey(templateName))
                {
                    value = parameterOverrides[templateName].GetValue("logoPanel");

                    if (!string.IsNullOrEmpty(value))
                        logoPanelName = value;
                    else
                        logoPanelName = nodeTemplate.GetValue("logoPanel");
                }
                else
                {
                    logoPanelName = nodeTemplate.GetValue("logoPanel");
                }

                //Glow panel
                if (parameterOverrides.ContainsKey(templateName))
                {
                    value = parameterOverrides[templateName].GetValue("glowPanel");

                    if (!string.IsNullOrEmpty(value))
                        glowPanelName = value;
                    else
                        glowPanelName = nodeTemplate.GetValue("glowPanel");
                }
                else
                {
                    glowPanelName = nodeTemplate.GetValue("glowPanel");
                }

                //Change the decals
                changeDecals();
            }
            else
                Log("name is null");
        }

        public void ShowDecals(bool isVisible)
        {
            if (string.IsNullOrEmpty(logoPanelTransforms))
                return;

            char[] delimiters = { ',' };
            string[] transformNames = logoPanelTransforms.Replace(" ", "").Split(delimiters);
            Transform[] targets;

            //Sanity checks
            if (transformNames == null || transformNames.Length == 0)
            {
                Log("transformNames are null");
                return;
            }

            //Go through all the named panels and find their transforms.
            foreach (string transformName in transformNames)
            {
                //Get the targets
                targets = part.FindModelTransforms(transformName);
                if (targets == null)
                {
                    Log("No targets found for " + transformName);
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

        protected void changeDecals()
        {
            Log("changeDecals called.");

            if (string.IsNullOrEmpty(logoPanelTransforms))
            {
                Log("changeDecals has no named transforms to change.");
                return;
            }

            char[] delimiters = { ',' };
            string[] transformNames = logoPanelTransforms.Replace(" ", "").Split(delimiters);
            Transform[] targets;
            Texture textureForDecal;
            Renderer rendererMaterial;

            //Sanity checks
            if (transformNames == null)
            {
                Log("transformNames are null");
                return;
            }

            //Go through all the named panels and find their transforms.
            //Then replace their textures.
            foreach (string transformName in transformNames)
            {
                //Get the targets
                targets = part.FindModelTransforms(transformName);
                if (targets == null)
                {
                    Log("No targets found for " + transformName);
                    continue;
                }

                //Now, replace the textures in each target
                foreach (Transform target in targets)
                {
                    rendererMaterial = target.GetComponent<Renderer>();

                    textureForDecal = GameDatabase.Instance.GetTexture(logoPanelName, false);
                    if (textureForDecal != null)
                        rendererMaterial.material.SetTexture(MAIN_TEXTURE, textureForDecal);

                    textureForDecal = GameDatabase.Instance.GetTexture(glowPanelName, false);
                    if (textureForDecal != null)
                        rendererMaterial.material.SetTexture(EMISSIVE_TEXTURE, textureForDecal);
                }
            }
        }

        protected override void getProtoNodeValues(ConfigNode protoNode)
        {
            base.getProtoNodeValues(protoNode);

            ConfigNode[] overrideNodes = null;
            string value;

            //capacity factor
            if (capacityFactor == 0f)
            {
                value = protoNode.GetValue("capacityFactor");
                if (string.IsNullOrEmpty(value) == false)
                    capacityFactor = float.Parse(value);
            }

            value = protoNode.GetValue("fieldReconfigurable");
            if (string.IsNullOrEmpty(value) == false)
                fieldReconfigurable = bool.Parse(value);

            //Name of the nodes to use as templates
            if (string.IsNullOrEmpty(templateNodes))
                templateNodes = protoNode.GetValue("templateNodes");

            //Also get template types
            templateTags = protoNode.GetValue("templateTags");

            //Set the defaults. We'll need them when we're in the editor
            //because the persistent KSP field seems to only apply to savegames.
            defaultTemplate = protoNode.GetValue("defaultTemplate");

            //Get the list of resources that must be kept when switching templates
            //If empty, then all of the part's resources will be cleared during a template switch.
            resourcesToKeep = protoNode.GetValue("resourcesToKeep");

            value = protoNode.GetValue("confirmResourceSwitch");
            if (string.IsNullOrEmpty(value) == false)
                confirmResourceSwitch = bool.Parse(value);

            //Build dictionary of decal names & overrides
            overrideNodes = protoNode.GetNodes("OVERRIDE");
            foreach (ConfigNode decalNode in overrideNodes)
            {
                value = decalNode.GetValue("name");
                if (string.IsNullOrEmpty(value) == false)
                {
                    if (parameterOverrides.ContainsKey(value) == false)
                        parameterOverrides.Add(value, decalNode);
                }
            }

            //Get the list of transforms for the logo panels.
            if (logoPanelTransforms == null)
                logoPanelTransforms = protoNode.GetValue("logoPanelTransforms");
        }

        protected virtual void hideEditorGUI(PartModule.StartState state)
        {
            Log("hideEditorGUI called");

            if (state == StartState.Editor)
            {
                this.Events["NextType"].guiActive = true;
                this.Events["PrevType"].guiActive = true;
            }

            else if (fieldReconfigurable == false)
            {
                this.Events["NextType"].guiActive = false;
                this.Events["PrevType"].guiActive = false;
            }
            else
            {
                this.Events["NextType"].guiActive = true;
                this.Events["PrevType"].guiActive = true;
            }
        }

        public virtual void SetGUIVisible(bool isVisible)
        {
            Events["NextType"].active = isVisible;
            Events["PrevType"].active = isVisible;
            Events["DumpResources"].active = isVisible;
            Fields["templateName"].guiActive = isVisible;
            Fields["templateName"].guiActiveEditor = isVisible;            

            if (string.IsNullOrEmpty(logoPanelTransforms))
            {
                Events["ToggleDecals"].guiActive = false;
                Events["ToggleDecals"].guiActiveEditor = false;
                Events["ToggleDecals"].guiActiveUnfocused = false;
            }

            else
            {
                Events["ToggleDecals"].guiActive = isVisible;
                Events["ToggleDecals"].guiActiveEditor = isVisible;
                Events["ToggleDecals"].guiActiveUnfocused = isVisible;

                if (isVisible)
                    ShowDecals(decalsVisible);
            }
        }

        protected virtual void initModuleGUI()
        {
            Log("initModuleGUI called");
            int index;
            string value;

            //Change the toggle button's name
            index = templateManager.GetNextUsableIndex(CurrentTemplateIndex);
            if (index != -1 && index != CurrentTemplateIndex)
            {
                value = templateManager.templateNodes[index].GetValue("name");
                Events["NextType"].guiName = "Next: " + value;
            }

            index = templateManager.GetPrevUsableIndex(CurrentTemplateIndex);
            if (index != -1 && index != CurrentTemplateIndex)
            {
                value = templateManager.templateNodes[index].GetValue("name");
                Events["PrevType"].guiName = "Prev: " + value;
            }
        }
        
        public void initTemplates()
        {
            Log("initTemplates called templateNodes: " + templateNodes + " templateTags: " + templateTags);
            //Create templates object if needed.
            //This can happen when the object is cloned in the editor (On Load won't be called).
            if (templateManager == null)
                templateManager = new TemplateManager(this.part, this.vessel, new LogDelegate(Log));
            templateManager.templateNodeName = templateNodes;
            templateManager.templateTags = templateTags;
            templateManager.FilterTemplates();

            if (templateManager.templateNodes == null)
            {
                Log("OnStart templateNodes == null!");
                return;
            }

            //Set default template if needed
            //This will happen when we're in the editor.
            if (string.IsNullOrEmpty(templateName))
                templateName = defaultTemplate;

            //Set current template index
            CurrentTemplateIndex = templateManager.FindIndexOfTemplate(templateName);
            if (CurrentTemplateIndex == -1)
            {
                CurrentTemplateIndex = 0;
                templateName = templateManager[CurrentTemplateIndex].GetValue("name");
            }

            //If we have only one template then hide the next/prev buttons
            if (templateManager.templateNodes.Count<ConfigNode>() == 1)
            {
                Events["NextType"].guiActive = false;
                Events["NextType"].guiActiveEditor = false;
                Events["NextType"].guiActiveUnfocused = false;
                Events["PrevType"].guiActive = false;
                Events["PrevType"].guiActiveEditor = false;
                Events["PrevType"].guiActiveUnfocused = false;
                Events["NextType"].active = true;
                Events["PrevType"].active = true;
            }
            else if (templateManager.templateNodes.Count<ConfigNode>() >= 4)
            {
                Events["NextType"].guiActive = true;
                Events["NextType"].guiActiveEditor = true;
                Events["NextType"].guiActiveUnfocused = true;
                Events["PrevType"].guiActive = true;
                Events["PrevType"].guiActiveEditor = true;
                Events["PrevType"].guiActiveUnfocused = true;
                Events["NextType"].active = true;
                Events["PrevType"].active = true;
            }

        }
        #endregion

        #region ReconfigurationCosts
        protected virtual void recoverResourceCost(string resourceName, double recycleAmount)
        {
        }

        protected virtual bool payPartsCost(int templateIndex, bool deflatedModulesAutoPass = true)
        {
             return true;
        }

        protected virtual bool hasSufficientSkill(string templateName)
        {
            return true;
        }

        protected virtual bool canAffordReconfigure(string templateName, bool deflatedModulesAutoPass = true)
        {
            return true;
        }        
        #endregion

        #region IPartMassModifier
        public virtual float CalculatePartMass(float defaultMass, float currentPartMass)
        {
            if (isInflatable && !isDeployed)
                return 0;
            if (partMass > 0.001f)
                return partMass;
            else
                return 0;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return CalculatePartMass(defaultMass, partMass);
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }
        #endregion

        #region IPartCostModifier
        public float GetModuleCost()
        {
            float resourceCost = ResourceHelper.GetResourceCost(this.part);

            return resourceCost;
        }

        public float GetModuleCost(float modifier)
        {
            return GetModuleCost();
        }

        public virtual float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            return GetModuleCost();
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return ModifierChangeWhen.CONSTANTLY;
            else
                return ModifierChangeWhen.FIXED;
        }
        #endregion
    }
}
