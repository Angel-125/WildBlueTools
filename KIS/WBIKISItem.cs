using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using System.Reflection;

/*
Source code copyright 2017, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public class WBIKISItem
    {
        static Type typeKISItem;
        static FieldInfo fiAvailablePart;
        static FieldInfo fiVolume;
        static FieldInfo fiKISIcon;
        static FieldInfo fiQuantity;
        static FieldInfo fiPartNode;
        static PropertyInfo piTotalMass;

        object itemObject;

        public static void InitClass(Assembly kisAssembly)
        {
            typeKISItem = kisAssembly.GetTypes().First(t => t.Name.Equals("KIS_Item"));

            fiAvailablePart = typeKISItem.GetField("availablePart");
            fiVolume = typeKISItem.GetField("volume");
            fiKISIcon = typeKISItem.GetField("icon");
            fiQuantity = typeKISItem.GetField("quantity");
            fiPartNode = typeKISItem.GetField("partNode");

            piTotalMass = typeKISItem.GetProperty("totalMass");
        }

        public WBIKISItem()
        {
        }

        public ConfigNode partNode
        {
            get
            {
                return (ConfigNode)fiPartNode.GetValue(itemObject);
            }
        }

        public int quantity
        {
            get 
            { 
                return (int)fiQuantity.GetValue(itemObject); 
            }
        }

        public WBIKISIcon Icon
        {
            get
            {
                object kisIcon = fiKISIcon.GetValue(itemObject);
                if (kisIcon == null)
                    return null;

                return new WBIKISIcon(kisIcon);
            }
        }

        public WBIKISItem(object obj)
        {
            if (WBIKISWrapper.kisAssembly == null)
                WBIKISWrapper.Init();

            itemObject = obj;
        }

        public AvailablePart availablePart
        {
            get
            {
                return (AvailablePart)fiAvailablePart.GetValue(itemObject);
            }

            set
            {
                fiAvailablePart.SetValue(itemObject, value);
            }
        }

        public float volume
        {
            get
            {
                return (float)fiVolume.GetValue(itemObject);
            }
        }

        public float totalMass
        {
            get
            {
                return (float)piTotalMass.GetValue(itemObject, null);
            }
        }
    }
}
