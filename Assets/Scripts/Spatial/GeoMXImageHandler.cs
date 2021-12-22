﻿using CellexalVR.General;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using DG.Tweening;
using TMPro;

namespace CellexalVR.Spatial
{

    public class GeoMXImageHandler : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public struct Cell
        {
            public int id;
            public string AOIImageID;
            public string ROIImageID;
            public string ScanImageID;

            public Cell(int id, string AOIImageID, string ROIImageID, string ScanImageID)
            {
                this.id = id;
                this.AOIImageID = AOIImageID;
                this.ROIImageID = ROIImageID;
                this.ScanImageID = ScanImageID;
            }
        }
        public Vector3 center;
        [HideInInspector] public SlideScroller slideScroller;
        [HideInInspector] public Vector3[] sliceCirclePositions = new Vector3[6];
        [HideInInspector] public Vector3 inactivePosLeft;
        [HideInInspector] public Vector3 inactivePosRight;
        [HideInInspector] public Dictionary<string, GeoMXSlide> roiSlides = new Dictionary<string, GeoMXSlide>();
        [HideInInspector] public GeoMXScanSlide selectedScan;
        [HideInInspector] public GeoMXROISlide selectedROI;
        private string imagePath;
        private Dictionary<string, GeoMXSlide> aoiSlides = new Dictionary<string, GeoMXSlide>();
        private Dictionary<string, GeoMXSlide> scanSlides = new Dictionary<string, GeoMXSlide>();
        private Dictionary<string, HashSet<string>> scanDict = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, HashSet<string>> roiDict = new Dictionary<string, HashSet<string>>();
        [SerializeField] private float radius;
        [SerializeField] private GeoMXScanSlide scanPrefab;
        [SerializeField] private GeoMXROISlide roiPrefab;
        [SerializeField] private GeoMXAOISlide aoiPrefab;
        [SerializeField] private TextMeshPro textMesh;
        private Dictionary<int, Cell> _cells = new Dictionary<int, Cell>();

        private void Start()
        {
            double angleStep = (-1f * Mathf.PI) / (float)(7);
            double angle;
            inactivePosLeft = new Vector3(Mathf.Cos(-Mathf.PI) * radius, 1.1f, Mathf.Sin(-Mathf.PI) * radius);
            for (int i = 0; i < 6; i++)
            {
                angle = -Mathf.PI + angleStep + (float)i * angleStep;
                Vector3 pos = new Vector3(Mathf.Cos((float)angle) * radius, 1.1f, Mathf.Sin((float)angle) * radius);
                sliceCirclePositions[i] = pos;
            }

            inactivePosRight = new Vector3(Mathf.Cos(0) * radius, 1.1f, Mathf.Sin(0) * radius);

            slideScroller = GetComponent<SlideScroller>();

            CellexalEvents.GraphsLoaded.AddListener(ReadData);
            CellexalEvents.SelectionConfirmed.AddListener(SpawnROIFromSelection);
        }

        public void ReadData()
        {
            imagePath = $"{Directory.GetCurrentDirectory()}\\Data\\{CellexalUser.DataSourceFolder}\\Images";
            if (!Directory.Exists(imagePath))
                return;
            using (StreamReader sr = new StreamReader($"{Directory.GetCurrentDirectory()}\\Data\\{CellexalUser.DataSourceFolder}\\images.csv"))
            {
                string header = sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string[] words = sr.ReadLine().Split(',');
                    int.TryParse(words[0], out int cellID);
                    string AOIImageID = words[1];
                    string ROIImageID = words[2];
                    if (!roiDict.ContainsKey(ROIImageID))
                    {
                        roiDict[ROIImageID] = new HashSet<string>();
                    }
                    roiDict[ROIImageID].Add(AOIImageID);
                    string ScanImageID = words[3];
                    if (!scanDict.ContainsKey(ScanImageID))
                    {
                        scanDict[ScanImageID] = new HashSet<string>();
                    }
                    scanDict[ScanImageID].Add(ROIImageID);
                    _cells[cellID] = new Cell(cellID, AOIImageID, ROIImageID, ScanImageID);
                }
            }
            SpawnAllScanImages();
        }

        public void ColorByNumericalAttribute(string attribute)
        {
            referenceManager.cellManager.ColorByNumericalAttribute(attribute);
        }

        private void UnSelectScan(string scanID)
        {
            GeoMXScanSlide slide = scanSlides[scanID].GetComponent<GeoMXScanSlide>();
            slide.UnHighlight();
            string[] roiIDs = slide.rois;
            foreach (string roiID in roiIDs)
            {
                Destroy(roiSlides[roiID].gameObject);
            }
            roiSlides.Clear();
        }
        private void UnSelectROI(string roiID)
        {
            GeoMXROISlide slide = roiSlides[roiID].GetComponent<GeoMXROISlide>();
            slide.UnHighlight();
            string[] aoiIDs = slide.aoiIDs;
            foreach (string aoiID in aoiIDs)
            {
                Destroy(aoiSlides[aoiID].gameObject);
            }
        }
        public void SpawnAOIImages(string scanID, string[] aoiIDs, string roiID)
        {
            if (selectedROI != null && roiID != selectedROI.roiID)
            {
                UnSelectROI(selectedROI.roiID);
            }
            selectedROI = roiSlides[roiID].GetComponent<GeoMXROISlide>();
            StartCoroutine(SpawnAOIImagesCoroutine(scanID, aoiIDs));
        }

        private IEnumerator SpawnAOIImagesCoroutine(string scanID, string[] aoiIDs)
        {
            foreach (KeyValuePair<string, GeoMXSlide> kvp in roiSlides)
            {
                Vector3 targetPos = kvp.Value.transform.localPosition;
                targetPos.y = 2.2f;
                kvp.Value.Move(targetPos);
            }
            foreach (KeyValuePair<string, GeoMXSlide> kvp in scanSlides)
            {
                Vector3 targetPos = kvp.Value.transform.localPosition;
                targetPos.y = 3.3f;
                kvp.Value.Move(targetPos);
            }
            for (int i = 0; i < aoiIDs.Length; i++)
            {
                string path = $"{imagePath}\\{scanID}\\{aoiIDs[i]}.png";
                if (File.Exists(path))
                {
                    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture($"file://{path}"))
                    {
                        uwr.downloadHandler = new DownloadHandlerTexture(true);
                        yield return uwr.SendWebRequest();
                        if (uwr.result != UnityWebRequest.Result.Success)
                        {
                            print(uwr.error);
                        }
                        else
                        {
                            GeoMXAOISlide aoi = Instantiate(aoiPrefab, transform);
                            aoi.imageHandler = this;
                            Texture2D aoiTexture = DownloadHandlerTexture.GetContent(uwr);
                            aoi.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", aoiTexture);
                            float ratio = (float)aoiTexture.width / (float)aoiTexture.height;
                            aoi.transform.localScale = new Vector3(1f * ratio, 1f, 1f);
                            aoi.originalScale = aoi.transform.localScale;
                            if (i < 6)
                            {
                                aoi.transform.localPosition = sliceCirclePositions[i];
                                //Vector3 center = new Vector3(0, aoi.transform.localPosition.y, 0);
                                aoi.transform.LookAt(2 * aoi.transform.position - center);
                            }

                            else
                            {
                                aoi.transform.localPosition = inactivePosRight;
                                aoi.gameObject.SetActive(false);
                            }
                            aoiSlides[aoiIDs[i]] = aoi;
                            aoi.index = i;
                            aoi.displayName = aoiIDs[i];
                            aoi.type = 2;
                        }
                    }
                }
                else
                {
                    print($"Could not find image {path}");
                }
            }
            slideScroller.currentIDs = aoiIDs;
            slideScroller.currentAOIIDs = aoiIDs;
            slideScroller.currentSlides = aoiSlides;
            slideScroller.currentSlide[2] = 0;
            slideScroller.currentType = 2;

        }

        public void SpawnROIImages(string scanID, string[] roiIDs)
        {
            if (selectedROI != null)
            {
                UnSelectROI(selectedROI.roiID);
            }
            if (selectedScan != null && scanID != selectedScan.scanID)
            {
                UnSelectScan(selectedScan.scanID);
            }
            foreach (KeyValuePair<string, GeoMXSlide> kvp in scanSlides)
            {
                Vector3 targetPos = kvp.Value.transform.localPosition;
                targetPos.y = 2.2f;
                kvp.Value.Move(targetPos);
            }
            selectedScan = scanSlides[scanID].GetComponent<GeoMXScanSlide>();

            StartCoroutine(SpawnROIImagesCoroutine(scanID, roiIDs));
        }

        private IEnumerator SpawnROIImagesCoroutine(string scanID, string[] roiIDs)
        {
            CellexalEvents.LoadingImages.Invoke();
            ReferenceManager.instance.graphManager.ResetGraphsColor();
            for (int i = 0; i < roiIDs.Length; i++)
            {
                string path = $"{imagePath}\\{scanID}\\{roiIDs[i]} - Segments.png";
                if (File.Exists(path))
                {
                    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture($"file://{path}"))
                    {
                        uwr.downloadHandler = new DownloadHandlerTexture(true);
                        yield return uwr.SendWebRequest();
                        if (uwr.result != UnityWebRequest.Result.Success)
                        {
                            print(uwr.error);
                        }
                        else
                        {
                            GeoMXROISlide roi = Instantiate(roiPrefab, transform);
                            roi.scanID = scanID;
                            roi.roiID = roiIDs[i];
                            roi.index = i;
                            roi.displayName = roi.roiID;
                            roi.aoiIDs = roiDict[roi.roiID].ToArray();
                            roi.imageHandler = this;
                            Texture2D roiTexture = DownloadHandlerTexture.GetContent(uwr);
                            roi.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", roiTexture);
                            float ratio = (float)roiTexture.width / (float)roiTexture.height;
                            roi.transform.localScale = new Vector3(Math.Min(1f * ratio, 1f), Math.Min(1f / ratio, 1f), 1f);
                            roi.originalScale = roi.transform.localScale;
                            if (i < 6)
                            {
                                roi.transform.localPosition = sliceCirclePositions[i];
                                //Vector3 center = new Vector3(0, roi.transform.localPosition.y, 0);
                                roi.transform.LookAt(2 * roi.transform.position - center);
                            }
                            else
                            {
                                roi.transform.localPosition = inactivePosRight;
                                roi.gameObject.SetActive(false);
                            }
                            roiSlides[roiIDs[i]] = roi;
                            foreach (string aoi in roi.aoiIDs)
                            {
                                var cellID = GetCellFromAoiID(aoi).id;
                                var graph = referenceManager.graphManager.Graphs[0];
                                var gPoint = graph.FindGraphPoint(cellID.ToString());
                                Color c = Color.blue;
                                referenceManager.selectionManager.AddGraphpointToSelection(gPoint, 0, false, c);
                            }
                            ShowName($"Loading {i} of {roiIDs.Length}");
                            roi.type = 1;
                        }
                    }
                }

                else
                {
                    print($"Could not find image {path}");
                }
            }
            CellexalEvents.ImagesLoaded.Invoke();
            ResetDisplayName();
            slideScroller.currentScanID = scanID;
            slideScroller.currentIDs = roiIDs;
            slideScroller.currentROIIDs = roiIDs;
            slideScroller.currentSlides = roiSlides;
            slideScroller.currentSlide[1] = 0;
            slideScroller.currentType = 1;
        }

        public IEnumerator SpawnROIImagesFromCells(Cell[] cells)
        {
            CellexalEvents.LoadingImages.Invoke();
            HashSet<string> uniqueImages = new HashSet<string>();
            int i = 0;
            foreach (Cell c in cells)
            {
                string path = $"{imagePath}\\{c.ScanImageID}\\{c.ROIImageID} - Segments.png";
                if (!File.Exists(path))
                {
                    print($"Could not find image {path}");
                    continue;
                }
                if (uniqueImages.Contains(path))
                {
                    continue;
                }

                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture($"file://{path}"))
                {
                    uwr.downloadHandler = new DownloadHandlerTexture(true);
                    yield return uwr.SendWebRequest();
                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        print(uwr.error);
                    }
                    else
                    {
                        GeoMXROISlide roi = Instantiate(roiPrefab, transform);
                        roi.scanID = c.ScanImageID;
                        roi.roiID = c.ROIImageID;
                        roi.aoiIDs = roiDict[roi.roiID].ToArray();
                        roi.imageHandler = this;
                        Texture2D roiTexture = DownloadHandlerTexture.GetContent(uwr);
                        roi.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", roiTexture);
                        if (i < 6)
                        {
                            roi.transform.localPosition = sliceCirclePositions[i];
                            //Vector3 center = new Vector3(0, roi.transform.localPosition.y, 0);
                            roi.transform.LookAt(2 * roi.transform.position - center);
                        }
                        else
                        {
                            roi.transform.localPosition = inactivePosLeft;
                            roi.gameObject.SetActive(false);
                        }
                        float ratio = (float)roiTexture.width / (float)roiTexture.height;
                        roi.transform.localScale = new Vector3(1f * ratio, 1f, 1f);
                        roiSlides[c.ROIImageID] = roi;
                        roi.type = 1;
                        uniqueImages.Add(path);
                    }
                    i++;
                }
                slideScroller.currentSlides = roiSlides;
                slideScroller.currentSlide[1] = 0;
                slideScroller.currentType = 1;
                CellexalEvents.ImagesLoaded.Invoke();
            }
        }

        public void SpawnROIFromSelection()
        {
            if (selectedROI != null)
            {
                UnSelectROI(selectedROI.roiID);
            }
            if (selectedScan != null)
            {
                UnSelectScan(selectedScan.scanID);
            }
            Cell[] cells = referenceManager.selectionManager.GetCurrentSelection().Select(x => _cells[int.Parse(x.Label)]).ToArray();

            StartCoroutine(SpawnROIImagesFromCells(cells));
            return;

        }

        public void SpawnAllScanImages()
        {
            StartCoroutine(SpawnScanImagesCoroutine(scanDict.Keys.ToArray()));
        }

        public void SpawnScanImage(string scanID = "")
        {
            StartCoroutine(SpawnScanImagesCoroutine(new string[0]));
        }

        private IEnumerator SpawnScanImagesCoroutine(string[] scanIDs)
        {
            if (scanIDs.Length == 0)
            {
                Cell c = _cells[0];
                scanIDs = new string[1] { c.ScanImageID };
            }
            CellexalEvents.LoadingImages.Invoke();
            int j = 0;
            for (int i = 0; i < scanIDs.Length; i++)
            {
                string path = $"{imagePath}\\{scanIDs[i]}\\{scanIDs[i]}.png";
                if (File.Exists(path))
                {
                    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture($"file://{path}"))
                    {
                        uwr.downloadHandler = new DownloadHandlerTexture(true);
                        yield return uwr.SendWebRequest();
                        if (uwr.result != UnityWebRequest.Result.Success)
                        {
                            print(uwr.error);
                        }
                        else
                        {
                            GeoMXScanSlide scan = Instantiate(scanPrefab, transform);
                            scan.transform.parent = transform;
                            scan.scanID = scanIDs[i];
                            scan.displayName = scanIDs[i];
                            scan.index = i;
                            scan.rois = scanDict[scanIDs[i]].ToArray();
                            scan.imageHandler = this;
                            scan.transform.localPosition = new Vector3(0, 0, radius);
                            //Vector3 center = new Vector3(0, scan.transform.localPosition.y, 0);
                            scan.transform.LookAt(2 * scan.transform.position - center);
                            Texture2D scanTexture = DownloadHandlerTexture.GetContent(uwr);
                            //scanTexture.LoadImage(File.ReadAllBytes(path));
                            scan.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", scanTexture);
                            float ratio = (float)scanTexture.width / (float)scanTexture.height;
                            scan.transform.localScale = new Vector3(1f * ratio, 1f, 1f);
                            scan.originalScale = scan.transform.localScale;
                            scanSlides[scanIDs[i]] = scan;
                            if (j < 6)
                            {
                                scan.transform.localPosition = sliceCirclePositions[j];
                                scan.transform.LookAt(2 * scan.transform.position - center);
                            }
                            else
                            {
                                scan.transform.localPosition = inactivePosLeft;
                                scan.gameObject.SetActive(false);
                            }
                            j++;
                            ShowName($"Loading {j} of {scanIDs.Length}");
                            scan.type = 0;
                        }
                    }
                }
                else
                {
                    print($"Could not find image {path}");
                }
            }
            CellexalEvents.ImagesLoaded.Invoke();
            ResetDisplayName();
            slideScroller.currentIDs = scanIDs;
            slideScroller.currentSlides = scanSlides;
            slideScroller.currentScanIDs = scanIDs;
            slideScroller.currentSlide[0] = 0;
            slideScroller.currentType = 0;
        }

        private Cell GetCellFromAoiID(string aoiID)
        {
            KeyValuePair<int, Cell>[] target = _cells.Where(c => c.Value.AOIImageID == aoiID).ToArray();
            return target[0].Value;
        }

        public void ShowName(string name)
        {
            textMesh.text = name;
        }

        public void ResetDisplayName()
        {
            textMesh.text = "";
        }

        public void SetScroller(int type)
        {
            if (type == 0)
            {
                slideScroller.currentSlides = scanSlides;
                slideScroller.currentType = 0;
                slideScroller.currentIDs = slideScroller.currentScanIDs;
            }
            else if (type == 1)
            {
                slideScroller.currentSlides = roiSlides;
                slideScroller.currentType = 1;
                slideScroller.currentIDs = slideScroller.currentROIIDs;
            }
            else
            {
                slideScroller.currentSlides = aoiSlides;
                slideScroller.currentType = 2;
                slideScroller.currentIDs = slideScroller.currentAOIIDs;
            }
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(GeoMXImageHandler))]
    public class SliceManagerEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            GeoMXImageHandler myTarget = (GeoMXImageHandler)target;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Read Image Data"))
            {
                myTarget.ReadData();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn A Scan Image"))
            {
                myTarget.SpawnScanImage();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn All Scan Images"))
            {
                myTarget.SpawnAllScanImages();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn From Selection"))
            {
                myTarget.SpawnROIFromSelection();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Color By Age"))
            {
                myTarget.ColorByNumericalAttribute("Age");
            }
            GUILayout.EndHorizontal();

            DrawDefaultInspector();
        }
    }
#endif
}