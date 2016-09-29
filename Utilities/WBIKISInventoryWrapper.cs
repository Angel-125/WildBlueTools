using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
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
    public class WBIKISInventoryWrapper
    {
        static Assembly kisAssembly;
        static Type typeModuleKISInventory;
        static FieldInfo fiPodSeat;
        static FieldInfo fiInvType;
        static FieldInfo fiMaxVolume;
        static MethodInfo miRefreshMassAndVolume;
        static Type typeInventoryType;

        public enum InventoryType { Container, Pod, Eva }

        PartModule inventoryModule;

        public WBIKISInventoryWrapper(PartModule pm)
        {
            if (kisAssembly == null)
            {
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

                typeModuleKISInventory = kisAssembly.GetTypes().First(t => t.Name.Equals("ModuleKISInventory"));
                typeInventoryType = typeModuleKISInventory.GetNestedTypes().First(t => t.Name.Equals("InventoryType"));

                miRefreshMassAndVolume = typeModuleKISInventory.GetMethod("RefreshMassAndVolume");

                fiPodSeat = typeModuleKISInventory.GetField("podSeat");
                fiInvType = typeModuleKISInventory.GetField("invType");
                fiMaxVolume = typeModuleKISInventory.GetField("maxVolume");
            }

            inventoryModule = pm;
        }

        public void RefreshMassAndVolume()
        {
            miRefreshMassAndVolume.Invoke(inventoryModule, null);
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
