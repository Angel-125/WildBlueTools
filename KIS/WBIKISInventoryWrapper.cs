using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Reflection;

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
    public class WBIKISWrapper
    {
        public static Assembly kisAssembly;

        public static void Init()
        {
            if (kisAssembly == null)
            {
                //Get the assembly
                foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
                {
                    if (loadedAssembly.name == "KIS")
                    {
                        kisAssembly = loadedAssembly.assembly;
                        break;
                    }

                }
                if (kisAssembly == null)
                    return;

                //Now init classes
                WBIKISInventoryWrapper.InitClass(kisAssembly);
                WBIKISItem.InitClass(kisAssembly);
                WBIKISIcon.InitClass(kisAssembly);
            }
        }

        public static bool IsKISInstalled()
        {
            Init();

            if (kisAssembly != null)
                return true;
            else
                return false;
        }
    }

    public class WBIKISInventoryWrapper
    {
        static Type typeModuleKISInventory;
        static FieldInfo fiPodSeat;
        static FieldInfo fiInvName;
        static FieldInfo fiInvType;
        static FieldInfo fiMaxVolume;
        static FieldInfo fiItems;
        static MethodInfo miRefreshMassAndVolume;
        static MethodInfo miIsFull;
        static MethodInfo miAddItem;
        static MethodInfo miDeleteItem;
        static MethodInfo miGetContentVolume;
        static Type typeInventoryType;

        public enum InventoryType { Container, Pod, Eva }

        public PartModule inventoryModule;

        public static void InitClass(Assembly kisAssembly)
        {
            typeModuleKISInventory = kisAssembly.GetTypes().First(t => t.Name.Equals("ModuleKISInventory"));
            typeInventoryType = typeModuleKISInventory.GetNestedTypes().First(t => t.Name.Equals("InventoryType"));

            miRefreshMassAndVolume = typeModuleKISInventory.GetMethod("RefreshMassAndVolume");
            miIsFull = typeModuleKISInventory.GetMethod("isFull");
            miAddItem = typeModuleKISInventory.GetMethod("AddItem", new[] { typeof(AvailablePart), typeof(ConfigNode), typeof(int), typeof(int) });
            miGetContentVolume = typeModuleKISInventory.GetMethod("GetContentVolume");
            miDeleteItem = typeModuleKISInventory.GetMethod("DeleteItem", new[] {typeof(int)});

            fiPodSeat = typeModuleKISInventory.GetField("podSeat");
            fiInvType = typeModuleKISInventory.GetField("invType");
            fiMaxVolume = typeModuleKISInventory.GetField("maxVolume");
            fiInvName = typeModuleKISInventory.GetField("invName");
            fiItems = typeModuleKISInventory.GetField("items");
        }

        public static List<WBIKISInventoryWrapper> GetInventories(Vessel vessel, InventoryType inventoryType = InventoryType.Container)
        {
            List<WBIKISInventoryWrapper> inventories = new List<WBIKISInventoryWrapper>();
            WBIKISInventoryWrapper wrapper;
            Part part;
            int totalParts = vessel.parts.Count;

            for (int index = 0; index < totalParts; index++)
            {
                part = vessel.parts[index];
                wrapper = GetInventory(part, inventoryType);
                if (wrapper != null)
                    inventories.Add(wrapper);
            }

            return inventories;
        }

        public static WBIKISInventoryWrapper GetInventory(Part part, InventoryType inventoryType = InventoryType.Container)
        {
            int totalModules = part.Modules.Count;
            PartModule module;
            WBIKISInventoryWrapper wrapper;

            for (int index = 0; index < totalModules; index++)
            {
                module = part.Modules[index];
                if (module.moduleName == "ModuleKISInventory")
                {
                    wrapper = new WBIKISInventoryWrapper(module);
                    if (wrapper.invType == inventoryType)
                        return wrapper;
                }
            }

            return null;
        }

        public WBIKISInventoryWrapper(PartModule pm)
        {
            if (WBIKISWrapper.kisAssembly == null)
                WBIKISWrapper.Init();

            inventoryModule = pm;
        }

        public void HideToggleInventory()
        {
            inventoryModule.Events["ToggleInventory"].guiActive = false;
            inventoryModule.Events["ToggleInventory"].guiActiveEditor = false;
            inventoryModule.Events["ToggleInventory"].guiActiveUnfocused = false;
        }

        public Dictionary<int, WBIKISItem> items
        {
            get
            {
                Dictionary<int, WBIKISItem> kisItems = new Dictionary<int, WBIKISItem>();

                IDictionary inventoryItems = (IDictionary)fiItems.GetValue(inventoryModule);

                foreach (DictionaryEntry entry in inventoryItems)
                    kisItems.Add((int)entry.Key, new WBIKISItem(entry.Value));

                return kisItems;
            }
        }

        public float GetContentVolume()
        {
            return (float)miGetContentVolume.Invoke(inventoryModule, null);
        }

        public WBIKISItem AddItem(AvailablePart availablePart, ConfigNode partNode, int qualtity, int slot=-1)
        {
            object obj = miAddItem.Invoke(inventoryModule, new object[] { availablePart, partNode, qualtity, slot });
            return new WBIKISItem(obj);
        }

        public void DeleteItem(int slotID)
        {
            miDeleteItem.Invoke(inventoryModule, new object[] { slotID });
        }

        public void RefreshMassAndVolume()
        {
            miRefreshMassAndVolume.Invoke(inventoryModule, null);
        }

        public bool isFull()
        {
            return (bool)miIsFull.Invoke(inventoryModule, null);
        }

        public int podSeat
        {
            get
            {
                return (int)fiPodSeat.GetValue(inventoryModule);
            }

            set
            {
                fiPodSeat.SetValue(inventoryModule, value);
            }
        }

        public string invName
        {
            get
            {
                return (string)fiInvName.GetValue(inventoryModule);
            }
        }

        public InventoryType invType
        {
            get
            {
                return (InventoryType)Enum.Parse(typeof(InventoryType), fiInvType.GetValue(inventoryModule).ToString());
            }

            set
            {
                //info.SetValue(newObject, Enum.ToObject(info.PropertyType, (int)dr.GetValue(index)), null);
                fiInvType.SetValue(inventoryModule, Enum.ToObject(typeInventoryType, (int)value));
            }
        }

        public float maxVolume
        {
            get
            {
                return (float)fiMaxVolume.GetValue(inventoryModule);
            }

            set
            {
                fiMaxVolume.SetValue(inventoryModule, value);
            }
        }

    }
}
