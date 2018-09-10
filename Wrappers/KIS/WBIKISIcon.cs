using System;
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
    public class WBIKISIcon
    {
		static Type typeKISIcon;
		static FieldInfo fiTexture;
		static MethodInfo miDispose;

        object objKISIcon;
        Texture iconTexture = null;

		public Texture texture
		{
			get
			{
                if (iconTexture == null)
                    iconTexture = (Texture)fiTexture.GetValue(objKISIcon);
                return iconTexture;
			}
		}

		public void Dispose()
		{
            miDispose.Invoke(objKISIcon, null);
		}

		public WBIKISIcon(object obj)
		{
            objKISIcon = obj;
		}

        public WBIKISIcon(Part part, int resolution)
            : this(Activator.CreateInstance(typeKISIcon, new object[] { part, resolution }))
		{
		}

        public static void InitClass(Assembly kisAssembly)
		{
            typeKISIcon = kisAssembly.GetTypes().First(t => t.Name.Equals("KIS_IconViewer"));
            fiTexture = typeKISIcon.GetField("texture");
            miDispose = typeKISIcon.GetMethod("Dispose");
		}
    }
}
