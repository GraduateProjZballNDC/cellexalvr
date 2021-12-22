using CellexalVR.General;
using CellexalVR.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CellexalVR.Spatial
{

    public class SlideScroller : MonoBehaviour
    {
        private GeoMXImageHandler imageHandler;
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private InputActionReference touchPadClick;
        [SerializeField] private InputActionReference touchPadPos;


        [HideInInspector] public Dictionary<int, int> currentSlide = new Dictionary<int, int>();
        public string currentScanID;
        [HideInInspector] public string[] currentScanIDs;
        [HideInInspector] public string[] currentROIIDs;
        [HideInInspector] public string[] currentAOIIDs;
        [HideInInspector] public string[] currentIDs;
        [HideInInspector] public Dictionary<string, GeoMXSlide> currentSlides;
        [HideInInspector] public int currentType;


        private void Start()
        {
            imageHandler = GetComponent<GeoMXImageHandler>();
            touchPadClick.action.performed += OnTouchPadClick;
        }

        private void OnTouchPadClick(InputAction.CallbackContext context)
        {
            if (ReferenceManager.instance.controllerModelSwitcher.DesiredModel == ControllerModelSwitcher.Model.SelectionTool)
                return;
            Vector2 pos = touchPadPos.action.ReadValue<Vector2>();
            Transform rLaser = ReferenceManager.instance.rightLaser.transform;
            Physics.Raycast(rLaser.position, rLaser.forward, out RaycastHit hit);
            if (!hit.collider || !hit.collider.GetComponent<GeoMXSlide>())
                return;
            if (pos.x > 0.5f)
            {
                Scroll(1);
            }
            else if (pos.x < -0.5f)
            {
                Scroll(-1);
            }
            //else if (pos.y > 0.5f)
            //{
            //}

            //else if (pos.y < -0.5f)
            //{
            //}
        }

        public void ScrollTo(int toSlice)
        {
            int diff = toSlice - currentSlide[currentType];
            Scroll(diff);
        }

        public void Scroll(int val)
        {
            if (currentSlides.Count <= 6)
                return;
            if (val > 0)
            {
                for (int i = 0; i < val; i++)
                {
                    int index = mod(currentSlide[currentType], currentIDs.Length);
                    GeoMXSlide sliceToToggleOff = currentSlides[currentIDs[index]].GetComponent<GeoMXSlide>();
                    Vector3 inactivePos = new Vector3(imageHandler.inactivePosLeft.x, sliceToToggleOff.transform.localPosition.y, imageHandler.inactivePosLeft.z);
                    if (!sliceToToggleOff.detached)
                    {
                        sliceToToggleOff.Move(inactivePos);
                        sliceToToggleOff.Fade(false);
                    }
                    currentSlide[currentType] = mod(++currentSlide[currentType], currentIDs.Length);
                    Vector3 targetPos;
                    for (int j = 0; j < 6; j++)
                    {
                        GeoMXSlide s = currentSlides[currentIDs[mod(currentSlide[currentType] + j, currentIDs.Length)]].GetComponent<GeoMXSlide>();
                        s.gameObject.SetActive(true);
                        if (j == 5)
                        {
                            targetPos = new Vector3(imageHandler.inactivePosRight.x, s.transform.localPosition.y, imageHandler.inactivePosRight.z);
                            if (!s.detached)
                            {
                                s.transform.localPosition = targetPos;
                                s.Fade(true);
                            }
                        }
                        if (!s.detached)
                        {
                            targetPos = new Vector3(imageHandler.sliceCirclePositions[j].x, s.transform.localPosition.y, imageHandler.sliceCirclePositions[j].z);
                            s.Move(targetPos);
                        }
                    }
                }
            }
            else if (val < 0)
            {
                for (int i = val; i < 0; i++)
                {
                    int index = mod(currentSlide[currentType] + 5, currentIDs.Length);
                    GeoMXSlide sliceToToggleOff = currentSlides[currentIDs[index]].GetComponent<GeoMXSlide>();
                    Vector3 inactivePos = new Vector3(imageHandler.inactivePosRight.x, sliceToToggleOff.transform.localPosition.y, imageHandler.inactivePosRight.z);
                    if (!sliceToToggleOff.detached)
                    {
                        sliceToToggleOff.Move(inactivePos);
                        sliceToToggleOff.Fade(false);
                    }
                    currentSlide[currentType] = mod(--currentSlide[currentType], currentIDs.Length);
                    Vector3 targetPos;
                    for (int j = 5; j >= 0; j--)
                    {
                        GeoMXSlide s = currentSlides[currentIDs[mod(currentSlide[currentType] + j, currentIDs.Length)]].GetComponent<GeoMXSlide>();
                        s.gameObject.SetActive(true);
                        if (j == 0)
                        {
                            if (!s.detached)
                            {
                                targetPos = new Vector3(imageHandler.inactivePosLeft.x, s.transform.localPosition.y, imageHandler.inactivePosLeft.z);
                                s.transform.localPosition = targetPos;
                                s.Fade(true);
                            }
                        }
                        if (!s.detached)
                        {
                            targetPos = new Vector3(imageHandler.sliceCirclePositions[j].x, s.transform.localPosition.y, imageHandler.sliceCirclePositions[j].z);
                            s.Move(targetPos);
                        }
                    }
                }
            }
        }

        public static int mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SlideScroller))]
    public class SlideScrollerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SlideScroller myTarget = (SlideScroller)target;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Left"))
            {
                myTarget.Scroll(-1);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Right"))
            {
                myTarget.Scroll(1);
            }
            GUILayout.EndHorizontal();

            DrawDefaultInspector();
        }
    }
#endif
}


