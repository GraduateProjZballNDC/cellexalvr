using UnityEngine;
///<summary>
/// Represents a button used for toggling the keyboard.
///</summary>
public class KeyboardButton : CellexalButton
{
    public Sprite gray;
    public Sprite original;

    private KeyboardSwitch keyboard;
    private ControllerModelSwitcher controllerModelSwitcher;
    private bool activateKeyboard = false;

    protected override string Description
    {
        get { return "Toggle keyboard"; }
    }

    protected override void Awake()
    {
        base.Awake();
        CellexalEvents.GraphsLoaded.AddListener(TurnOn);
        CellexalEvents.GraphsUnloaded.AddListener(TurnOff);
    }

    private void Start()
    {
        keyboard = referenceManager.keyboard;
        controllerModelSwitcher = referenceManager.controllerModelSwitcher;
        SetButtonActivated(false);

    }

    void Update()
    {
        if (!buttonActivated) return;
        device = SteamVR_Controller.Input((int)rightController.index);
        if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            activateKeyboard = !keyboard.KeyboardActive;
            keyboard.SetKeyboardVisible(activateKeyboard);
            if (activateKeyboard)
            {
                controllerModelSwitcher.DesiredModel = ControllerModelSwitcher.Model.Keyboard;
                controllerModelSwitcher.ActivateDesiredTool();
            }
            else
            {
                controllerModelSwitcher.TurnOffActiveTool(true);
            }
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
