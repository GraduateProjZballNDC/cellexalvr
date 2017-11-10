﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

/// <summary>
/// A generator for heatmaps.
/// </summary>
public class HeatmapGenerator : MonoBehaviour
{
    public ReferenceManager referenceManager;
    public GameObject heatmapPrefab;
    public ErrorMessageController errorMessageController;

    private SelectionToolHandler selectionToolHandler;
    private StatusDisplay status;
    private StatusDisplay statusDisplayHUD;
    private StatusDisplay statusDisplayFar;
    private ArrayList data;
    private Thread t;
    private SteamVR_Controller.Device device;
    private GameObject hourglass;
    private int heatmapID = 1;
    private Vector3 heatmapPosition;
    private List<Heatmap> heatmapList = new List<Heatmap>();

    void Start()
    {
        t = null;
        hourglass = GameObject.Find("WaitingForHeatboardHourglass");
        hourglass.SetActive(false);
        heatmapPosition = heatmapPrefab.transform.position;
        selectionToolHandler = referenceManager.selectionToolHandler;
        status = referenceManager.statusDisplay;
        statusDisplayHUD = referenceManager.statusDisplayHUD;
        statusDisplayFar = referenceManager.statusDisplayFar;
    }

    internal void DeleteHeatmaps()
    {
        foreach (Heatmap h in heatmapList)
        {
            if (h != null)
            {
                Destroy(h.gameObject);
            }
        }
        heatmapList.Clear();
    }

    public void CreateHeatmap()
    {
        // name the heatmap "heatmap_X". Where X is some number.
        string heatmapName = "heatmap_" + (selectionToolHandler.fileCreationCtr - 1);
        CellExAlLog.Log("Creating heatmap");
        StartCoroutine(GenerateHeatmapRoutine(heatmapName));
    }

    public Heatmap FindHeatmap(string heatmapName)
    {
        foreach (Heatmap hm in heatmapList)
        {
            if (hm.HeatmapName == heatmapName)
            {
                return hm;
            }
        }
        return null;
    }

    /// <summary>
    /// Coroutine for creating a heatmap.
    /// </summary>
    IEnumerator GenerateHeatmapRoutine(string heatmapName)
    {
        if (selectionToolHandler.selectionConfirmed)
        {
            // make a deep copy of the arraylist
            List<GraphPoint> selection = selectionToolHandler.GetLastSelection();
            Dictionary<Cell, int> colors = new Dictionary<Cell, int>();
            foreach (GraphPoint g in selection)
            {
                colors[g.Cell] = g.CurrentGroup;
            }

            // Check if more than one cell is selected
            if (selection.Count < 1)
            {
                CellExAlLog.Log("can not create heatmap with less than 1 graphpoints, aborting");
                yield break;
            }
            //Color c1 = ((GraphPoint)selection[0]).GetComponent<Renderer>().material.color;
            //bool colorFound = false;
            //for (int i = 1; i < selection.Count; ++i)
            //{
            //    Color c2 = ((GraphPoint)selection[i]).GetComponent<Renderer>().material.color;
            //    if (!((c1.r == c2.r) && (c1.g == c2.g) && (c1.b == c2.b)))
            //    {
            //        colorFound = true;
            //        break;
            //    }
            //}
            //if (!colorFound)
            //{
            //    // Generate error message if less than two colors are selected
            //    errorMessageController.DisplayErrorMessage(3);
            //    CellExAlLog.Log("Can not create heatmap with only one grouping color, aborting");
            //    yield break;
            //}

            int statusId = status.AddStatus("R script generating heatmap");
            int statusIdHUD = statusDisplayHUD.AddStatus("R script generating heatmap");
            int statusIdFar = statusDisplayFar.AddStatus("R script generating heatmap");
            // Start generation of new heatmap in R
            string home = Directory.GetCurrentDirectory();
            int fileCreationCtr = selectionToolHandler.fileCreationCtr - 1;
            string args = home + " " + selectionToolHandler.DataDir + " " + fileCreationCtr + " " + CellExAlUser.UserSpecificFolder;

            string rScriptFilePath = Application.streamingAssetsPath + @"\R\make_heatmap.R";
            string heatmapDirectory = home + @"\Images";
            if (!Directory.Exists(heatmapDirectory))
            {
                CellExAlLog.Log("Creating directory " + CellExAlLog.FixFilePath(heatmapDirectory));
                Directory.CreateDirectory(heatmapDirectory);
            }
            CellExAlLog.Log("Running R script " + CellExAlLog.FixFilePath(rScriptFilePath) + " with the arguments \"" + args + "\"");
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            t = new Thread(() => RScriptRunner.RunFromCmd(rScriptFilePath, args));
            t.Start();
            // Show hourglass
            hourglass.SetActive(true);

            while (t.IsAlive)
            {
                yield return null;
            }
            stopwatch.Stop();
            CellExAlLog.Log("Heatmap R script finished in " + stopwatch.Elapsed.ToString());
            status.RemoveStatus(statusId);
            statusDisplayHUD.RemoveStatus(statusIdHUD);
            statusDisplayFar.RemoveStatus(statusIdFar);

            string newHeatmapFilePath = heatmapDirectory + @"\" + heatmapName + ".png";
            //File.Delete(newHeatmapFilePath);
            //File.Move(heatmapFilePath + @"\heatmap.png", newHeatmapFilePath);

            var heatmap = Instantiate(heatmapPrefab).GetComponent<Heatmap>();
            heatmap.transform.parent = transform;
            heatmap.transform.localPosition = heatmapPosition;
            // save colors before.
            heatmap.SetVars(colors);
            heatmapList.Add(heatmap);

            hourglass.SetActive(false);

            heatmap.UpdateImage(newHeatmapFilePath);
            heatmap.GetComponent<AudioSource>().Play();
            heatmap.name = heatmapName;
            heatmapID++;
        }
    }
}
