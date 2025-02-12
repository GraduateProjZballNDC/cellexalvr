using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using CellexalVR.General;
using System;

namespace CellexalVR.Interaction
{
    /// <summary>
    /// Responsible for changing the controller model and the activated tool.
    /// </summary>
    public class ControllerModelSwitcher : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        // OpenXR
        //public SteamVR_RenderModel renderModel;
        public GameObject rightControllerBody;
        public GameObject leftControllerBody;
        public Mesh normalControllerMesh;
        //public Texture normalControllerTexture;
        public Mesh menuControllerMesh;
        //public Texture menuControllerTexture;
        public Mesh minimizeMesh;
        [SerializeField] private GameObject minimizeTool;
        public Material normalMaterial;
        public Material selectionToolHandlerMaterial;
        public Material leftControllerMaterial;

        public enum Model { Normal, SelectionTool, Menu, Minimizer, DeleteTool, HelpTool, Keyboard, TwoLasers, DrawTool, WebBrowser };
        // what model we actually want
        public Model DesiredModel { get; set; }
        // what model is actually displayed, useful for when we want to change the model temporarily
        // for example: the user has activated the selection tool, so DesiredModel = SelectionTool and actualModel = SelectionTool
        // the user then moves the controller into the menu. DesiredModel is still SelectionTool, but actualModel will now be Menu
        public Model ActualModel;
        public bool meshesSetSuccessful;

        //private SelectionToolHandler selectionToolHandler;
        private SelectionToolCollider selectionToolCollider;
        private GameObject deleteTool;
        private GameObject minimizer;
        private GameObject drawTool;
        private KeyboardSwitch keyboard;
        private GameObject webBrowser;
        private MeshFilter rightControllerBodyMeshFilter;
        private Renderer rightControllerBodyRenderer;
        private MeshFilter leftControllerBodyMeshFilter;
        private Renderer leftControllerBodyRenderer;
        private Color desiredColor;
        private LaserPointerController laserPointerController;
        private XRRayInteractor rightLaser;
        private XRRayInteractor leftLaser;
        private int selectionToolMeshIndex = 0;
        // The help tool is a bit of an exception, it can be active while another tool is also active, like the keyboard.
        // Otherwise you can't point the helptool towards the keyboard.
        public bool HelpToolShouldStayActivated { get; set; }

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Awake()
        {
            //selectionToolHandler = referenceManager.selectionToolHandler;
            selectionToolCollider = referenceManager.selectionToolCollider;
            drawTool = referenceManager.drawTool.gameObject;
            keyboard = referenceManager.keyboardSwitch;
            webBrowser = referenceManager.webBrowser;
            deleteTool = referenceManager.deleteTool;
            minimizer = referenceManager.minimizeTool.gameObject;
            DesiredModel = Model.Normal;
            laserPointerController = referenceManager.laserPointerController;
            rightLaser = referenceManager.rightLaser;
            leftLaser = referenceManager.leftLaser;

            if (CrossSceneInformation.Spectator)
                gameObject.SetActive(false);
            //if (rightControllerBody.activeSelf)
            //{
            //SetMeshes();
            //}
            //else
            //{
            // OpenXR
            //SteamVR_Events.RenderModelLoaded.Listen(OnControllerLoaded);
            //}
        }

        // OpenXR
        // Used when starting the program to know when steamvr has loaded the model and applied a meshfilter and meshrenderer for us to use.
        //void OnControllerLoaded(SteamVR_RenderModel renderModel, bool success)
        //{
        //    if (!success) return;
        //    if (!CrossSceneInformation.Spectator)
        //    {
        //        //Valve.VR.ETrackedDeviceProperty.Prop_RenderModelName_String
        //        //SetMeshes();
        //        //print(renderModel.renderModelName);
        //    }
        //}

        public void SetMeshes()
        {
            rightControllerBodyMeshFilter = rightControllerBody.GetComponent<MeshFilter>();
            rightControllerBodyMeshFilter.mesh = normalControllerMesh;
            rightControllerBodyRenderer = rightControllerBody.GetComponent<Renderer>();
            rightControllerBodyRenderer.sharedMaterial = normalMaterial;
            leftControllerBodyMeshFilter = leftControllerBody.GetComponent<MeshFilter>();
            leftControllerBodyMeshFilter.mesh = normalControllerMesh;
            leftControllerBodyRenderer = leftControllerBody.GetComponent<Renderer>();
            leftControllerBodyRenderer.material = normalMaterial;
            meshesSetSuccessful = true;
            //var leftBody = leftControllerBody.GetComponent<Renderer>();
            //leftBody.material = leftControllerMaterial;
        }

        /// <summary>
        /// Used when starting the program.
        /// It takes some time for steamvr and vrtk to set everything up, and for some time
        /// these variables will be null because the components are not yet added to the gameobjects.
        /// </summary>
        /// <returns></returns>
        internal bool Ready()
        {
            // Open XR
            return true;
            if (CrossSceneInformation.Spectator)
                return true;
            return rightControllerBody.GetComponent<MeshFilter>() != null && rightControllerBody.GetComponent<Renderer>() != null
                 && leftControllerBody.GetComponent<MeshFilter>() != null && leftControllerBody.GetComponent<Renderer>() != null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name.Equals("Menu Selecter Collider"))
            {
                if (rightControllerBodyMeshFilter == null) return;
                //SwitchToModel(Model.Menu);
                deleteTool.SetActive(false);
                minimizer.SetActive(false);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name.Equals("Menu Selecter Collider"))
            {
                if (rightControllerBodyMeshFilter == null) return;
                ActivateDesiredTool();
            }
        }

        /// <summary>
        /// Switches the right controller's model.
        /// </summary>
        public void SwitchToModel(Model model)
        {
            // open xr
            if (!meshesSetSuccessful)
            {
                //SetMeshes();
            }
            ActualModel = model;
            switch (model)
            {
                case Model.Normal:
                case Model.HelpTool:
                case Model.DrawTool:
                case Model.DeleteTool:
                    //rightControllerBodyMeshFilter.mesh = normalControllerMesh;
                    //rightControllerBodyRenderer.material = normalMaterial;
                    break;

                case Model.Keyboard:
                    //keyboard.SetKeyboardVisible(true);
                    //rightLaser.enabled = true;
                    laserPointerController.origin.localRotation = Quaternion.identity;
                    break;

                case Model.WebBrowser:
                    webBrowser.GetComponent<WebManager>().SetBrowserActive(true);
                    //webBrowser.GetComponent<WebManager>().SetVisible(true);
                    //rightLaser.enabled = true;
                    // OpenXR
                    laserPointerController.ToggleLaser(true);
                    //rightLaser.tracerVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOn;
                    laserPointerController.origin.localRotation = Quaternion.identity;
                    break;

                case Model.TwoLasers:
                    laserPointerController.ToggleLaser(true);
                    //rightLaser.tracerVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOn;
                    laserPointerController.origin.localRotation = Quaternion.identity;
                    break;

                case Model.Menu:
                    //print("switched to menu");
                    drawTool.SetActive(false);
                    deleteTool.SetActive(false);
                    minimizer.SetActive(false);
                    minimizeTool.SetActive(false);
                    selectionToolCollider.SetSelectionToolEnabled(false);
                    //rightLaser.tracerVisibility = VRTK_BasePointerRenderer.VisibilityStates.AlwaysOn;
                    laserPointerController.ToggleLaser(true);
                    //rightControllerBodyMeshFilter.mesh = normalControllerMesh;
                    //rightControllerBodyRenderer.sharedMaterial = normalMaterial;
                    break;

                case Model.SelectionTool:
                    //rightControllerBodyMeshFilter.mesh = normalControllerMesh;
                    //rightControllerBodyRenderer.sharedMaterial = selectionToolHandlerMaterial;
                    //rightControllerBodyRenderer.sharedMaterial.color = new Color(desiredColor.r, desiredColor.g, desiredColor.b, 0.5f);
                    break;

                case Model.Minimizer:
                    //rightControllerBodyMeshFilter.mesh = minimizeMesh;
                    minimizeTool.SetActive(true);
                    break;

            }
        }

        /// <summary>
        /// Activates the current tool and changes the controller's model to that tool and deactivates previously active tool.
        /// </summary>
        public void ActivateDesiredTool()
        {
            // These models can have the help tool activated, so if we are not switching to one of them, the help tool should go away
            if (DesiredModel != Model.HelpTool && DesiredModel != Model.Keyboard)
            {
                HelpToolShouldStayActivated = false;
            }
            // Deactivate all tools that should not be active.
            if (DesiredModel != Model.SelectionTool && selectionToolCollider != null)
            {
                selectionToolCollider.SetSelectionToolEnabled(false);
            }
            if (DesiredModel != Model.DeleteTool && deleteTool != null)
            {
                deleteTool.SetActive(false);
            }
            if (DesiredModel != Model.Minimizer && minimizer != null)
            {
                minimizer.SetActive(false);
            }
            if (DesiredModel != Model.HelpTool && !HelpToolShouldStayActivated)
            {
                //helpTool.SetToolActivated(false);
            }
            // if we are switching from the keyboard to the help tool, the keyboard should stay activated.
            if (DesiredModel != Model.Keyboard && DesiredModel != Model.HelpTool)
            {
                //laserPointerController.ToggleLaser(false);
                //leftLaser.enabled = false;
                //keyboard.SetKeyboardVisible(false);
                //rightLaser.enabled = false;
                //referenceManager.multiuserMessageSender.SendMessageActivateKeyboard(false);
            }
            if (DesiredModel != Model.TwoLasers)
            {
                laserPointerController.ToggleLaser(false);
                leftLaser.enabled = false;
            }
            if (DesiredModel != Model.DrawTool)
            {
                drawTool.SetActive(false);
            }
            switch (DesiredModel)
            {
                case Model.SelectionTool:
                    selectionToolCollider.SetSelectionToolEnabled(true);
                    break;
                case Model.DeleteTool:
                    deleteTool.SetActive(true);
                    break;
                case Model.Minimizer:
                    minimizer.SetActive(true);
                    break;
                //case Model.HelpTool:
                //    helpTool.SetToolActivated(true);
                //    rightLaser.enabled = true;
                //    break;
                case Model.Keyboard:
                    keyboard.SetKeyboardVisible(true);
                    laserPointerController.ToggleLaser(true);
                    //rightLaser.enabled = true;
                    break;
                case Model.WebBrowser:
                    webBrowser.GetComponent<WebManager>().SetBrowserActive(true);
                    //webBrowser.GetComponent<WebManager>().SetVisible(true);
                    laserPointerController.ToggleLaser(true);
                    break;
                case Model.TwoLasers:
                    laserPointerController.ToggleLaser(true);
                    leftLaser.enabled = true;
                    break;
                case Model.DrawTool:
                    drawTool.SetActive(true);
                    break;
            }
            SwitchToDesiredModel();
            CellexalEvents.ModelChanged.Invoke();
        }

        /// <summary>
        /// Turns off the active tool and sets our desired model to the normal model.
        /// </summary>
        /// <param name="inMenu"> True if the controller is in the menu and we should temporarily change into the menu model, false otherwise. </param>
        public void TurnOffActiveTool(bool inMenu)
        {
            selectionToolCollider.SetSelectionToolEnabled(false);
            deleteTool.SetActive(false);
            minimizer.SetActive(false);
            //if (!HelpToolShouldStayActivated)
            //{
            //helpTool.SetToolActivated(false);
            DesiredModel = Model.Normal;
            //}
            //else
            //{
            //    DesiredModel = Model.HelpTool;
            //}
            laserPointerController.ToggleLaser(false);
            //leftLaser.enabled = false;
            keyboard.SetKeyboardVisible(false);
            //referenceManager.multiuserMessageSender.SendMessageActivateKeyboard(false);
            drawTool.SetActive(false);
            //webBrowser.GetComponent<WebManager>().SetBrowserActive(false);
            webBrowser.GetComponent<WebManager>().SetVisible(false);
            if (inMenu)
            {
                SwitchToModel(Model.Menu);
            }
            else
            {
                SwitchToModel(Model.Normal);
            }
        }

        /// <summary>
        /// Switches to the desired model. Does not activate or deactivate any tool.
        /// </summary>
        public void SwitchToDesiredModel()
        {
            SwitchToModel(DesiredModel);
        }

        /// <summary>
        /// Used by the selectiontoolhandler. Changes the current model's color.
        /// </summary>
        /// <param name="color"> The new color. </param>
        public void SwitchControllerModelColor(Color color)
        {
            desiredColor = color;

            if (ActualModel == Model.SelectionTool)
            {
                //rightControllerBodyRenderer.sharedMaterial.color = new Color(desiredColor.r, desiredColor.g, desiredColor.b, 0.5f);
            }
        }

        public void SwitchSelectionToolMesh(bool dir)
        {
            if (ActualModel == Model.SelectionTool)
            {
                selectionToolCollider.CurrentMeshIndex += dir ? 1 : -1;
                ActivateDesiredTool();
            }
        }
    }
}