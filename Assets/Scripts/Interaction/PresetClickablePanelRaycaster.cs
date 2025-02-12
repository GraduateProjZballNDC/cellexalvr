﻿using UnityEngine;
using CellexalVR.General;
namespace CellexalVR.Interaction
{
    /// <summary>
    /// Raycasts from the right controller onto a group of <see cref="PresetClickableTextPanel"/>.
    /// </summary>
    public class PresetClickablePanelRaycaster : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        // Open XR 
        //private SteamVR_Controller.Device device;
        private UnityEngine.XR.Interaction.Toolkit.ActionBasedController rightController;
        private UnityEngine.XR.InputDevice device;
        private ClickablePanel lastHit = null;
        private bool hitDemoPanelLastFrame = false;
        private ControllerModelSwitcher controllerModelSwitcher;
        private int panelLayerMask;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            rightController = referenceManager.rightController;
            controllerModelSwitcher = referenceManager.controllerModelSwitcher;
            panelLayerMask = 1 << LayerMask.NameToLayer("SelectableLayer");
            CellexalEvents.RightTriggerClick.AddListener(OnTriggerClick);
        }

        private void Update()
        {
            Raycast();
        }

        private void Raycast(bool triggerClick = false)
        {
            var raycastingSource = referenceManager.rightLaser.transform;
            // Open XR
            //device = SteamVR_Controller.Input((int)rightController.index);
            var ray = new Ray(raycastingSource.position, referenceManager.rightController.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 10f, panelLayerMask))
            {
                // if we hit something this frame.
                if (!hitDemoPanelLastFrame)
                {
                    hitDemoPanelLastFrame = true;
                    SetLaserActivated(true);
                }
                var hitPanel = hit.collider.transform.gameObject.GetComponent<PresetClickableTextPanel>();
                if (hitPanel != null)
                {
                    if (lastHit != null && lastHit != hitPanel)
                    {
                        lastHit.SetHighlighted(false);
                    }
                    hitPanel.SetHighlighted(true);
                    //if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                    if (triggerClick)
                    {
                        hitPanel.Click();
                    }
                    lastHit = hitPanel;
                }
                else if (lastHit != null)
                {
                    // if we hit something this frame but it was not a clickablepanel and we hit a clickablepanel last frame.
                    lastHit.SetHighlighted(false);
                    lastHit = null;
                }
            }
            else if (lastHit != null)
            {
                // if we hit nothing this frame, but hit something last frame.
                lastHit.SetHighlighted(false);
                lastHit = null;
            }
            else if (hitDemoPanelLastFrame)
            {
                // if we hit nothing this frame and hit the panels last frame
                hitDemoPanelLastFrame = false;
                SetLaserActivated(false);
            }

        }
        private void OnTriggerClick()
        {
            Raycast(true);
        }



        private void SetLaserActivated(bool active)
        {
            if (controllerModelSwitcher.ActualModel != ControllerModelSwitcher.Model.Keyboard ||
                controllerModelSwitcher.ActualModel != ControllerModelSwitcher.Model.TwoLasers ||
                controllerModelSwitcher.ActualModel != ControllerModelSwitcher.Model.Menu)
            {
                //referenceManager.rightLaser.enabled = active;
                referenceManager.laserPointerController.Override = active;
                if (active)
                {
                    controllerModelSwitcher.SwitchToModel(ControllerModelSwitcher.Model.Menu);
                }
                else
                {
                    controllerModelSwitcher.SwitchToModel(ControllerModelSwitcher.Model.Normal);
                    //controllerModelSwitcher.ActivateDesiredTool();
                }
            }
        }
    }
}