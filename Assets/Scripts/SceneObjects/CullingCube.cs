﻿using System.Collections.Generic;
using System.Linq;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.Filters;
using CellexalVR.General;
using UnityEngine;

namespace Assets.Scripts.SceneObjects
{
    public class CullingCube : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public int boxNr;
        public GameObject attachOnSideArea;

        private Material material;
        private FilterManager filterManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            filterManager = referenceManager.filterManager;
            CellexalEvents.LegendAttached.AddListener(() => attachOnSideArea.SetActive(false));
            foreach (List<GameObject> lodGroup in referenceManager.graphManager.Graphs.SelectMany(graph => graph.lodGroupClusters.Values))
            {
                material = lodGroup[0].GetComponent<Renderer>().sharedMaterial;
                material.SetFloat("_CullingActive", 1f);
            }
        }

        private void Update()
        {
            if (referenceManager.graphGenerator.isCreating) return;
            foreach (List<GameObject> lodGroup in referenceManager.graphManager.Graphs.SelectMany(graph => graph.lodGroupClusters.Values))
            {
                material = lodGroup[0].GetComponent<Renderer>().sharedMaterial;
                material.SetMatrix(boxNr == 1 ? "_BoxMatrix" : "_BoxMatrix2", transform.worldToLocalMatrix);
            }
        }

        public void InverseCulling(bool invert)
        {
            float value = invert ? -1 : 1;
            foreach (List<GameObject> lodGroup in referenceManager.graphManager.Graphs.SelectMany(graph => graph.lodGroupClusters.Values))
            {
                material = lodGroup[0].GetComponent<Renderer>().sharedMaterial;
                material.SetFloat("_Culling", value);
            }
        }

        public void ActivateFilter()
        {
            if (filterManager.currentFilter == null)
                return;
            Graph g = referenceManager.graphManager.originalGraphs.Find(x => !x.GraphName.Contains("Slice"));
            foreach (Cell c in referenceManager.cellManager.GetCells())
            {
                Graph.GraphPoint gp = g.FindGraphPoint(c.Label);
                filterManager.ActivateCullingFilter();
                referenceManager.filterManager.AddCellToEval(gp, referenceManager.selectionToolCollider.CurrentColorIndex);
            }
        }
    }
}