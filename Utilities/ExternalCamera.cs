using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//Adapted from CactEyeCamera by Rubber Ducky
namespace WildBlueIndustries
{
    public class ExternalCamera: MonoBehaviour
    {
        //Camera resolution
        public int CameraWidth;
        public int CameraHeight;

        //Linear transform of the cameras
        public Transform CameraTransform;

        //Field of view of the camera
        public float FieldOfView;

        public bool RotationLock = false;

        //Texture stuff...
        private RenderTexture ScopeRenderTexture;
        private RenderTexture FullResolutionTexture;
        private Texture2D ScopeTexture2D;
        private Texture2D FullTexture2D;

        //I wonder if C# has a map data structure; a map would simplify some things
        private Camera[] CameraObject = { null, null, null, null, null, null };

        private Renderer[] skyboxRenderers;
        private ScaledSpaceFader[] scaledSpaceFaders;

        /*
         * Constructor
         * Input: The owning part's transform.
         * Purpose: This constructor will start up the owning part's camera object. The idea behind this
         * was to allow for multiple telescopes on the same craft. 
         */
        public ExternalCamera(Transform Position)
        {
            this.CameraTransform = Position;

            CameraWidth = (int)(Screen.width*0.4f);
            CameraHeight = (int)(Screen.height*0.4f);

            ScopeRenderTexture = new RenderTexture(CameraWidth, CameraHeight, 24);
            ScopeRenderTexture.Create();

            FullResolutionTexture = new RenderTexture(Screen.width, Screen.height, 24);
            FullResolutionTexture.Create();

            ScopeTexture2D = new Texture2D(CameraWidth, CameraHeight);
            FullTexture2D = new Texture2D(Screen.width, Screen.height);

            CameraSetup(0, "GalaxyCamera"); //As of KSP 1.0, the GalaxyCamera object was added. Thanks to MOARDv for figuring this one out.
            CameraSetup(1, "Camera ScaledSpace");
            CameraSetup(2, "Camera 01");
            CameraSetup(3, "Camera 00");
            CameraSetup(4, "Camera VE Underlay");
            CameraSetup(5, "Camera VE Overlay");

            skyboxRenderers = (from Renderer r in (FindObjectsOfType(typeof(Renderer)) as IEnumerable<Renderer>) where (r.name == "XP" || r.name == "XN" || r.name == "YP" || r.name == "YN" || r.name == "ZP" || r.name == "ZN") select r).ToArray<Renderer>();
            if (skyboxRenderers == null)
            {
                Debug.Log("ExternalCamera: Logical Error: skyboxRenderers is null!");
            }

            scaledSpaceFaders = FindObjectsOfType(typeof(ScaledSpaceFader)) as ScaledSpaceFader[];
            if (scaledSpaceFaders == null)
            {
                Debug.Log("ExternalCamera: Logical Error: scaledSpaceFaders is null!");
            }

            
        }


        #region Helper Functions

        public Texture2D UpdateTexture(Texture2D Output)
        {
            return UpdateTexture(ScopeRenderTexture, Output);
        }

        /*
         * Function name: UpdateTexture
         * Input: None
         * Output: A fully rendered texture of what's through the telescope.
         * Purpose: This function will produce a single frame texture of what image is being looked
         * at through the telescope. 
         * Note: Need to modify behavior depending on what processor is currently active.
         */
        public Texture2D UpdateTexture(RenderTexture RT, Texture2D Output)
        {

            RenderTexture CurrentRT = RenderTexture.active;
            RenderTexture.active = RT;

            //Update position of the cameras
            foreach (Camera Cam in CameraObject)
            {
                if (Cam != null)
                {
                    //The if statement fixes a bug with the camera position and timewarp.
                    if (Cam.name.Contains("0"))
                    {
                        //Cam.transform = CameraTransform;
                        Cam.transform.position = CameraTransform.position;
                    }
                    Cam.transform.up = CameraTransform.up;
                    Cam.transform.forward = CameraTransform.forward;
                    if (!RotationLock) 
                    {
                        Cam.transform.rotation = CameraTransform.rotation;
                    }
                    Cam.fieldOfView = FieldOfView;
                    Cam.targetTexture = RT;
                }
                else
                {
                    Debug.Log("ExternalCamera: " + Cam.name.ToString() + " was not found!");
                }
            }

            CameraObject[0].Render();
            CameraObject[1].Render();
            foreach (Renderer r in skyboxRenderers)
            {
                r.enabled = false;
            }
            foreach (ScaledSpaceFader s in scaledSpaceFaders)
            {
                s.r.enabled = true;
            }
            CameraObject[1].clearFlags = CameraClearFlags.Depth;
            CameraObject[1].farClipPlane = 3e30f;
            CameraObject[1].Render();
            foreach (Renderer r in skyboxRenderers)
            {
                r.enabled = true;
            }
            CameraObject[2].Render();
            CameraObject[3].Render();

            Output.ReadPixels(new Rect(0, 0, Output.width, Output.height), 0, 0);
            Output.Apply();
            RenderTexture.active = CurrentRT;
            return Output;
        }

        /*
         * Function name: GetCameraByName
         * Purpose: This returns the camera specified by the input "name." Copied and pasted
         * from Rastor Prop Monitor.
         */
        private Camera GetCameraByName(string name)
        {
            foreach (Camera cam in Camera.allCameras)
            {
                if (cam.name == name)
                {
                    return cam;
                }
            }
            return null;
        }

        /*
         * Function name: CameraSetup
         * Purpose: This will make a copy of the specified camera. Taken from
         * Rastor Prop Monitor.
         */
        private void CameraSetup(int Index, string SourceName)
        {

            if (CameraObject == null)
            {
                Debug.Log("ExternalCamera: Logical Error 2: The Camera Object is null. The mod author needs to perform a code review.");
            }
            else
            {

                GameObject CameraBody = new GameObject("CactEye " + SourceName);
                if (CameraBody == null)
                {
                    Debug.Log("ExternalCamera: logical Error: CameraBody was null!");
                }
                CameraBody.name = "ExternalCamera" + SourceName;
                CameraObject[Index] = CameraBody.AddComponent<Camera>();
                if (CameraObject[Index] == null)
                {
                    Debug.Log("ExternalCamera: Logical Error 1: CameraBody.AddComponent returned null! If you do not have Visual Enhancements installed, then this error can be safely ignored.");
                }
                CameraObject[Index].CopyFrom(GetCameraByName(SourceName));
                CameraObject[Index].enabled = true;
                CameraObject[Index].targetTexture = ScopeRenderTexture;

                //if (Index == 0)
                //{
                    //CameraObject[Index].enabled = false;
                //}
                if (Index == 2 || Index == 3)
                {
                    CameraObject[Index].cullingMask = (1 << 0) | (1 << 15) | (1 << 18) | (1 << 19) | (1 << 23);
                }
                if (Index != 0)
                {
                    CameraObject[Index].transform.position = CameraTransform.position;
                    CameraObject[Index].transform.forward = CameraTransform.forward;
                    CameraObject[Index].transform.rotation = CameraTransform.rotation;
                    CameraObject[Index].fieldOfView = FieldOfView;
                    CameraObject[Index].farClipPlane = 3e30f;
                }
                //Debug.Log("ExternalCamera: Debug: Camera[" + Index.ToString() + "]: " + CameraObject[Index].cullingMask.ToString());
            }
        }

        /*
         * Function name: UpdatePosition
         * Purpose: This will update the local position data from the parent part.
         */
        public void UpdatePosition(Transform Position)
        {
            this.CameraTransform = Position;
        }

        public Camera GetCamera(int Index)
        {
            return CameraObject[Index];
        }

        #endregion

    }
}
