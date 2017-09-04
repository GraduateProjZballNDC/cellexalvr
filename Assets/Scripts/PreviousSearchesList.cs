﻿using UnityEngine;

/// <summary>
/// This class represents the list of the 10 previous searches of genes.
/// </summary>
public class PreviousSearchesList : MonoBehaviour
{

    public CellManager cellManager;
    public PreviousSearchesListNode topListNode;
    public SteamVR_TrackedController rightController;
    public SteamVR_Controller.Device device;
    public Material normalMaterial;
    public Material highlightedMaterial;
    public Texture searchLockNormalTexture;
    public Texture searchLockNormalHighlightedTexture;
    public Texture searchLockLockedTexture;
    public Texture searchLockLockedHighlightedTexture;
    public Texture correlatedGenesButtonTexture;
    public Texture correlatedGenesButtonHighlightedTexture;
    private Transform raycastingSource;
    private Ray ray;
    private RaycastHit hit;
    private LayerMask layer;
    private PreviousSearchesListNode listNode;
    private PreviousSearchesListNode lastHitListNode;
    private PreviousSearchesLock searchLock;
    private PreviousSearchesLock lastHitLock;
    private CorrelatedGenesButton correlatedGenesButton;
    private CorrelatedGenesButton lastCorrelatedGenesButton;
    private CorrelatedGenesListNode correlatedGenesListNode;
    private CorrelatedGenesListNode lastCorrelatedGenesListNode;
    private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;

    void Update()
    {
        raycastingSource = rightController.transform;
        device = SteamVR_Controller.Input((int)rightController.controllerIndex);
        // this method is probably responsible for too much. oh well.
        ray = new Ray(raycastingSource.position, raycastingSource.forward);
        if (Physics.Raycast(ray, out hit))
        {
            // we may hit something that is not of any use to us.
            // so we check if we hit anything interesting.
            listNode = hit.transform.gameObject.GetComponent<PreviousSearchesListNode>();
            searchLock = hit.transform.gameObject.GetComponent<PreviousSearchesLock>();
            correlatedGenesButton = hit.transform.gameObject.GetComponent<CorrelatedGenesButton>();
            correlatedGenesListNode = hit.transform.gameObject.GetComponent<CorrelatedGenesListNode>();
            if (listNode != null)
            {
                // handle the list node
                if (listNode != lastHitListNode)
                {
                    if (lastHitListNode != null)
                        lastHitListNode.SetMaterial(normalMaterial);
                    lastHitListNode = listNode;
                    listNode.SetMaterial(highlightedMaterial);
                }
                if (device.GetPressDown(triggerButton))
                {
                    if (listNode.GeneName != "")
                    {
                        cellManager.ColorGraphsByPreviousExpression(listNode.GeneName);
                    }
                }
            }
            else if (searchLock != null)
            {
                // handle the lock
                if (searchLock != lastHitLock)
                {
                    if (lastHitLock != null)
                    {
                        if (lastHitLock.Locked)
                            lastHitLock.SetTexture(searchLockLockedTexture);
                        else
                            lastHitLock.SetTexture(searchLockNormalTexture);

                    }
                    lastHitLock = searchLock;
                    if (searchLock.Locked)
                        searchLock.SetTexture(searchLockLockedHighlightedTexture);
                    else
                        searchLock.SetTexture(searchLockNormalHighlightedTexture);

                }
                if (device.GetPressDown(triggerButton))
                {
                    searchLock.ToggleSearchNodeLock();
                    if (searchLock.Locked)
                        searchLock.SetTexture(searchLockLockedHighlightedTexture);
                    else
                        searchLock.SetTexture(searchLockNormalHighlightedTexture);
                }
            }
            else if (correlatedGenesButton != null)
            {
                // handle the calculate correlated genes button
                if (lastCorrelatedGenesButton != correlatedGenesButton)
                {
                    if (lastCorrelatedGenesButton != null)
                        lastCorrelatedGenesButton.SetTexture(correlatedGenesButtonTexture);
                    lastCorrelatedGenesButton = correlatedGenesButton;
                    correlatedGenesButton.SetTexture(correlatedGenesButtonHighlightedTexture);
                }

                if (device.GetPressDown(triggerButton))
                {
                    correlatedGenesButton.CalculateCorrelatedGenes();
                }
            }
            else if (correlatedGenesListNode != null)
            {
                // handle the correlated gene button
                if (lastCorrelatedGenesListNode != correlatedGenesListNode)
                {
                    if (lastCorrelatedGenesListNode != null)
                        lastCorrelatedGenesListNode.SetMaterial(normalMaterial);
                    lastCorrelatedGenesListNode = correlatedGenesListNode;
                    correlatedGenesListNode.SetMaterial(highlightedMaterial);
                }

                if (device.GetPressDown(triggerButton))
                {
                    cellManager.ColorGraphsByGene(correlatedGenesListNode.GeneName);
                }
            }
        }
        else
        {
            // the raycast hit nothing
            listNode = null;
            searchLock = null;
            correlatedGenesButton = null;
            correlatedGenesListNode = null;
        }
        // when the raycaster leaves an object we must un-highlight it
        if (listNode == null && lastHitListNode != null)
        {
            lastHitListNode.SetMaterial(normalMaterial);
            lastHitListNode = null;
        }
        else if (searchLock == null && lastHitLock != null)
        {
            if (lastHitLock.Locked)
                lastHitLock.SetTexture(searchLockLockedTexture);
            else
                lastHitLock.SetTexture(searchLockNormalTexture);
            lastHitLock = null;
        }
        else if (correlatedGenesButton == null && lastCorrelatedGenesButton != null)
        {
            lastCorrelatedGenesButton.SetTexture(correlatedGenesButtonTexture);
            lastCorrelatedGenesButton = null;
        }
        else if (correlatedGenesListNode == null && lastCorrelatedGenesListNode != null)
        {
            lastCorrelatedGenesListNode.SetMaterial(normalMaterial);
            lastCorrelatedGenesListNode = null;
        }
    }

    /// <summary>
    /// Clears the list.
    /// </summary>
    public void ClearList()
    {
        foreach (PreviousSearchesListNode node in GetComponentsInChildren<PreviousSearchesListNode>())
        {
            node.Locked = false;
            node.GeneName = "";
        }
        foreach (PreviousSearchesLock lockButton in GetComponentsInChildren<PreviousSearchesLock>())
        {
            lockButton.Locked = false;
            lockButton.SetTexture(searchLockNormalTexture);
        }
    }
}
