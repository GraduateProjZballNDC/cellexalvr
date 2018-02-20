﻿using UnityEngine;
/// <summary>
/// Represents the button that opens the menu for coloring graphs based on a previous selection.
/// </summary>
class CreateSelectionFromPreviousMenuButton : StationaryButton
{
    public GameObject menu;
    public GameObject buttons;

    protected override string Description
    {
        get { return "Show menu for selection a previous selection"; }
    }

    void Start()
    {
        menu = referenceManager.createSelectionFromPreviousSelectionMenu.gameObject;
        buttons = referenceManager.rightButtons;
        SetButtonActivated(false);
        CellExAlEvents.GraphsLoaded.AddListener(TurnOn);
        CellExAlEvents.GraphsUnloaded.AddListener(TurnOff);

    }

    private void Update()
    {
        if (!buttonActivated) return;
        device = SteamVR_Controller.Input((int)rightController.index);
        if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            buttons.SetActive(false);
            menu.SetActive(true);
            controllerInside = false;

        }
    }

    private void TurnOn()
    {
        SetButtonActivated(true);
    }

    private void TurnOff()
    {
        SetButtonActivated(false);
    }
}
