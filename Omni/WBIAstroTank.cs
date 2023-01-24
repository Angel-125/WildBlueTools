using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WildBlueIndustries
{
    public class WBIAstroTank: WBIOmniStorage
    {
        [KSPField]
        public bool debugMode;

        [KSPField(isPersistant = true)]
        public double originalMass = -1;

        [KSPField(isPersistant = true)]
        public double previousMass = -1;

        [KSPField(isPersistant = true)]
        public float currentStorageCapacity = -1;

        [KSPField]
        public float storagePercent = 0.85f;

        public ModuleAsteroid moduleAsteroid;
        public ModuleComet moduleComet;
        public ModuleSpaceObjectInfo spaceObjectInfo;
        public Dictionary<string, double> abundances = new Dictionary<string, double>();

        protected string abundanceResources;

        [KSPEvent(guiActive = true, guiName = "Setup dynamic storage")]
        public void SetObjectResources()
        {
            //Can we afford to reconfigure the container?
            if (!confirmedReconfigure && canAffordReconfigure() && hasSufficientSkill())
            {
                ScreenMessages.PostScreenMessage("Click again to confirm reconfiguration.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                confirmedReconfigure = true;
                return;
            }
            payForReconfigure();

            // Get list of resources. 
            List<ModuleSpaceObjectResource> resources = part.FindModulesImplementing<ModuleSpaceObjectResource>();
            ModuleSpaceObjectResource resource;
            int count = resources.Count;

            // Clear any previews
            if (count > 0)
            {
                previewResources.Clear();
                previewRatios.Clear();
                part.Resources.Clear();
                abundances.Clear();
            }

            // Add the resources
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < count; index++)
            {
                resource = resources[index];
                if (resource.abundance > 0)
                {
                    previewResources.Add(resource.resourceName, 0);
                    previewRatios.Add(resource.resourceName, 1.0f);
                    abundances.Add(resource.resourceName, resource.abundance);
                    ResourceHelper.AddResource(resource.resourceName, 0, 0, part);
                    builder.Append(resource.resourceName);
                }
            }
            abundanceResources = builder.ToString();

            // Recalculate max amounts
            currentStorageCapacity = 0;
            adjustedVolume = currentStorageCapacity;
            inventoryAdjustedVolume = currentStorageCapacity;
            recalculateMaxAmounts();
            reconfigureStorage();

            // Hide this event
            Events["SetObjectResources"].guiActive = part.Resources.Count == 0;
        }

        public void OnDestroy()
        {
            GameEvents.OnResourceConverterOutput.Remove(OnResourceConverterOutput);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get the part modules
            moduleAsteroid = part.vessel.FindPartModuleImplementing<ModuleAsteroid>();
            moduleComet = part.vessel.FindPartModuleImplementing<ModuleComet>();
            if (moduleAsteroid != null)
                spaceObjectInfo = moduleAsteroid.part.FindModuleImplementing<ModuleSpaceObjectInfo>();
            else if (moduleComet != null)
                spaceObjectInfo = moduleComet.part.FindModuleImplementing<ModuleSpaceObjectInfo>();

            if (spaceObjectInfo != null && originalMass < 0)
            {
                originalMass = spaceObjectInfo.currentMassVal;
                previousMass = originalMass;
            }

            // Setup storage capacity
            if (currentStorageCapacity > 0)
            {
                adjustedVolume = currentStorageCapacity;
            }
            else
            {
                currentStorageCapacity = adjustedVolume;
                previousMass = spaceObjectInfo.currentMassVal;
            }

            getAbundances();

            // If we have no resources in the part and we have storage capacity then show the initial resources button.
            Events["SetObjectResources"].guiActive = part.Resources.Count == 0 || debugMode;

            GameEvents.OnResourceConverterOutput.Add(OnResourceConverterOutput);

            if (part.Resources.Count > 0)
                ResourceScenario.Instance.gameSettings.MaxDeltaTime = 3600;
        }

        private void OnResourceConverterOutput(PartModule partModule, string resourceName, double amount)
        {
            if (!HighLogic.LoadedSceneIsFlight || part.Resources.Count <= 0 || spaceObjectInfo == null)
                return;

            // Make sure that the event is from our wessel.
            if (partModule.part.vessel != part.vessel)
                return;

            // Make sure the converter is a drill.
            if (!(partModule is ModuleAsteroidDrill) && !(partModule is ModuleCometDrill))
                return;

            // Make sure the resource extracted is one of the abundance resources.
            if (!abundanceResources.Contains(resourceName))
                return;

            if (!part.Resources.Contains(resourceName))
                return;

            PartResourceDefinition definition = definitions[resourceName];
            float storageCapacityLiters = definition.volume * (float)amount;
            inventoryAdjustedVolume += storageCapacityLiters;
            adjustedVolume += storageCapacityLiters;
            currentStorageCapacity = inventoryAdjustedVolume;

            part.Resources[resourceName].maxAmount += amount;
        }

        private void getAbundances()
        {
            List<ModuleSpaceObjectResource> resources = part.FindModulesImplementing<ModuleSpaceObjectResource>();
            ModuleSpaceObjectResource resource;
            int count = resources.Count;

            abundances.Clear();
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < count; index++)
            {
                resource = resources[index];
                if (resource.abundance > 0 && !abundances.ContainsKey(resource.resourceName))
                {
                    abundances.Add(resource.resourceName, resource.abundance);
                    builder.Append(resource.resourceName);
                }
            }
            abundanceResources = builder.ToString();
        }
    }
}
