﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using TMPro;

namespace CellexalVR.Interaction
{
    public class AnnotationTextPanel : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        private List<Cell> cells;

        private LineRenderer line;
        private TextMeshPro textMesh;

        private void Start()
        {
            line = GetComponentInChildren<LineRenderer>();
            textMesh = GetComponentInChildren<TextMeshPro>();
        }

        public void SetCells(List<Cell> cells)
        {
            this.cells = new List<Cell>(cells);

            if (line == null)
            {
                line = GetComponentInChildren<LineRenderer>();
            }
            if (textMesh == null)
            {
                textMesh = GetComponentInChildren<TextMeshPro>();
            }
            Graph graph = GetComponentInParent<Graph>();
            Graph.GraphPoint gp = graph.FindGraphPoint(cells[0].Label);
            Vector3 position = gp.Position;
            referenceManager.selectionManager.AddGraphpointToSelection(gp);
            transform.localPosition = position;
            Vector3 dir = (Vector3.zero - transform.localPosition).normalized;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.zero - (0.2f * dir));
            textMesh.transform.localPosition = Vector3.zero - (0.2f * dir);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("GameController"))
            {
                referenceManager.cellManager.HighlightCells(cells.ToArray(), true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("GameController"))
            {
                referenceManager.cellManager.HighlightCells(cells.ToArray(), false);
            }
        }
    }
}