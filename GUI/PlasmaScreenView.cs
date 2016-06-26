using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.IO;

namespace WildBlueIndustries
{
    public delegate void ShowImageDelegate(Texture selectedImage, string textureFilePath);

    public class PlasmaScreenView : Window<PlasmaScreenView>
    {
        public ShowImageDelegate showImageDelegate;
        public Texture2D previewImage;
        public string screeshotFolderPath;
        public Transform cameraTransform;
        public Part part;
        public int cameraIndex;
        public bool enableRandomImages;
        public float screenSwitchTime;
        public string aspectRatio;

        protected ExternalCamera externalCamera;
        protected string[] imagePaths;
        protected string[] fileNames;
        protected string[] viewOptions = { "Screenshots", "Camera" };
        protected int viewOptionIndex;
        protected int selectedIndex;
        protected int prevSelectedIndex = -1;
        List<WBICamera> cameras = new List<WBICamera>();

        private Vector2 _scrollPos;

        public PlasmaScreenView() :
        base("Select An Image", 900, 600)
        {
            Resizable = false;
            _scrollPos = new Vector2(0, 0);
        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            if (string.IsNullOrEmpty(screeshotFolderPath))
                screeshotFolderPath = KSPUtil.ApplicationRootPath.Replace("\\", "/") + "Screenshots/";

            imagePaths = Directory.GetFiles(screeshotFolderPath);
            List<string> names = new List<string>();
            foreach (string pictureName in imagePaths)
            {
                names.Add(pictureName.Replace(screeshotFolderPath, ""));
            }
            fileNames = names.ToArray();

            if (HighLogic.LoadedSceneIsFlight)
            {
                cameras.Clear();
                foreach (Part part in this.part.vessel.parts)
                {
                    WBICamera camera = part.FindModuleImplementing<WBICamera>();
                    if (camera != null)
                        cameras.Add(camera);
                }
            }
        }

        public void GetRandomImage()
        {
            int imageIndex = UnityEngine.Random.Range(0, imagePaths.Length);
            Texture2D randomImage = new Texture2D(1, 1);
            WWW www = new WWW("file://" + imagePaths[imageIndex]);

            www.LoadImageIntoTexture(randomImage);

            if (showImageDelegate != null)
                showImageDelegate(randomImage, imagePaths[imageIndex]);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();

//            drawCameraSelectors();

            if (string.IsNullOrEmpty(aspectRatio) == false)
                GUILayout.Label("Aspect Ratio: " + aspectRatio);

            enableRandomImages = GUILayout.Toggle(enableRandomImages, "Enable Random Images");
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, new GUILayoutOption[] { GUILayout.Width(375) });
            if (viewOptionIndex == 0)
                selectedIndex = GUILayout.SelectionGrid(selectedIndex, fileNames, 1);
//            else
//                drawCameraControls();

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            if (viewOptionIndex == 0)
                drawScreenshotPreview();
//            else
//                drawCameraView();

            drawOkCancelButtons();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        protected void drawCameraView()
        {
            if (previewImage != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(previewImage, new GUILayoutOption[] { GUILayout.Width(525), GUILayout.Height(400) });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        protected void drawScreenshotPreview()
        {
            if (selectedIndex != prevSelectedIndex)
            {
                prevSelectedIndex = selectedIndex;
                previewImage = new Texture2D(1, 1);
                WWW www = new WWW("file://" + imagePaths[selectedIndex]);

                www.LoadImageIntoTexture(previewImage);
            }

            if (previewImage != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(previewImage, new GUILayoutOption[] { GUILayout.Width(525), GUILayout.Height(400) });
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

        }

        protected void drawCameraControls()
        {
            GUILayout.Label("TODO: camera rotate and zoom controls here.");
        }

        protected void drawOkCancelButtons()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                if (showImageDelegate != null)
                    showImageDelegate(previewImage, imagePaths[selectedIndex]);
                SetVisible(false);
            }

            if (GUILayout.Button("Cancel"))
            {
                SetVisible(false);
            }
            GUILayout.EndHorizontal();
        }

        protected void drawCameraSelectors()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (cameras.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", new GUILayoutOption[] {GUILayout.Width(30) }))
                    {
                        cameraIndex -= 1;
                        if (cameraIndex < 0)
                            cameraIndex = cameras.Count - 1;
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(cameras[cameraIndex].cameraName);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(30) }))
                    {
                        cameraIndex += 1;
                        if (cameraIndex >= cameras.Count)
                            cameraIndex = 0;
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No cameras on vessel.");
                }
            }
            else
            {
                GUILayout.Label("Cameras only available in flight.");
            }

            viewOptionIndex = GUILayout.SelectionGrid(viewOptionIndex, viewOptions, 2);
            if (viewOptionIndex == 1)
            {
                if (externalCamera == null)
                {
                    externalCamera = new ExternalCamera(cameras[cameraIndex].GetCameraTransform());
                    previewImage = new Texture2D(externalCamera.CameraWidth, externalCamera.CameraHeight);
                    externalCamera.UpdateTexture(previewImage);
                }
                selectedIndex = 0;
            }

            else
            {
                externalCamera = null;
            }
        }
    }
}
