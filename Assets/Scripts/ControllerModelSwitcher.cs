using UnityEngine;
using System.Collections;

/// <summary>
/// This class is responsible for changing the controller model.
/// </summary>
public class ControllerModelSwitcher : MonoBehaviour
{
    public SteamVR_RenderModel renderModel;
    public GameObject controllerBody;
    public Mesh normalControllerMesh;
    public Texture normalControllerTexture;
    public Mesh menuControllerMesh;
    public Texture menuControllerTexture;
    public SelectionToolHandler selectionToolHandler;
    public Mesh selectionToolMesh;
    public Mesh deleteToolMesh;
    public Material normalMaterial;
    public Material selectionToolHandlerMaterial;
    public GameObject fire;
    public GameObject deleter;
    public SelectionToolButton selectionToolButton;
    public enum Model { Normal, SelectionTool, Menu, DeleteTool };
    public Model DesiredModel { get; set; }
    private Model actualModel;
    private bool selectionToolEnabled = false;
    private MeshFilter controllerBodyMeshFilter;
    private Renderer controllerBodyRenderer;
    private Color desiredColor;

    void Awake()
    {
        DesiredModel = Model.Normal;
        if (controllerBody.activeSelf == false)
            SteamVR_Events.RenderModelLoaded.Listen(OnControllerLoaded);
        else
        {
            controllerBodyMeshFilter = controllerBody.GetComponent<MeshFilter>();
            controllerBodyRenderer = controllerBody.GetComponent<Renderer>();
        }
    }

    void OnControllerLoaded(SteamVR_RenderModel renderModel, bool success)
    {
        if (!success) return;
        controllerBodyMeshFilter = controllerBody.GetComponent<MeshFilter>();
        controllerBodyRenderer = controllerBody.GetComponent<Renderer>();
    }

    internal bool Ready()
    {
        return controllerBodyMeshFilter != null && controllerBodyRenderer != null;
    }

    void OnTriggerEnter(Collider other)
    {
        //print("ontriggerenter " + other.gameObject.name);
        if (other.gameObject.CompareTag("Controller"))
        {
            //print ("ontriggerenter " + other.gameObject.name);
            if (controllerBodyMeshFilter == null) return;
            SwitchToModel(Model.Menu);
            fire.SetActive(false);
            deleter.SetActive(false);

        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Controller"))
        {
            if (controllerBodyMeshFilter == null) return;
            SwitchToModel(DesiredModel);
            if (DesiredModel == Model.DeleteTool)
                deleter.SetActive(true);
        }
    }

    /// <summary>
    /// Should be called when a button that changes the tool is pressed.
    /// </summary>
    public void ToolSwitched()
    {
        selectionToolEnabled = false;
    }

    /// <summary>
    /// Switches the right controller's model.
    /// </summary>
    public void SwitchToModel(Model model)
    {
        //print ("switching to " + model);
        actualModel = model;
        switch (model)
        {
            case Model.Normal:
                controllerBodyMeshFilter.mesh = normalControllerMesh;
                controllerBodyRenderer.material = normalMaterial;
                break;

            case Model.Menu:
                controllerBodyMeshFilter.mesh = menuControllerMesh;
                controllerBodyRenderer.material = normalMaterial;
                break;

            case Model.SelectionTool:
                controllerBodyMeshFilter.mesh = selectionToolMesh;
                controllerBodyRenderer.material = selectionToolHandlerMaterial;
                controllerBodyRenderer.material.color = desiredColor;
                break;
            case Model.DeleteTool:
                controllerBodyMeshFilter.mesh = deleteToolMesh;
                break;
        }
    }

    public void TurnOffActiveTool()
    {

        selectionToolEnabled = false;
        selectionToolHandler.SetSelectionToolEnabled(false, true);
        fire.SetActive(false);
        deleter.SetActive(false);
        DesiredModel = Model.Normal;
        SwitchToModel(Model.Normal);
    }

    public void SwitchToDesiredModel()
    {
        SwitchToModel(DesiredModel);
    }

    public void SwitchControllerModelColor(Color color)
    {
        desiredColor = color;

        if (actualModel == Model.SelectionTool)
            controllerBodyRenderer.material.color = desiredColor;
    }
}
