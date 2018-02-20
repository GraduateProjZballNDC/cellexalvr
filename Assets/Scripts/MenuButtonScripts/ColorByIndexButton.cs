﻿using UnityEngine;

/// <summary>
/// Represents a button that colors all graphs according to an index.
/// </summary>
public class ColorByIndexButton : SolidButton
{
    public TextMesh description;

    private CellManager cellManager;
    private string indexName;

    protected override void Start()
    {
        base.Start();
        cellManager = referenceManager.cellManager;
    }

    void Update()
    {
        device = SteamVR_Controller.Input((int)rightController.index);
        if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            cellManager.ColorByIndex(indexName);
        }
    }

    /// <summary>
    /// Sets which index this button should show when pressed.
    /// </summary>
    /// <param name="indexName"> The name of the index. </param>
    public void SetIndex(string indexName)
    {
        //color = network.GetComponent<Renderer>().material.color;
        //GetComponent<Renderer>().material.color = color;
        color = GetComponent<Renderer>().material.color;
        this.indexName = indexName;
        description.text = indexName;
    }
}
