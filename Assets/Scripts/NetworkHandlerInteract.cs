﻿using UnityEngine;
using VRTK;

/// <summary>
/// This class handles what happens when a network handler is interacted with.
/// </summary>
class NetworkHandlerInteract : VRTK_InteractableObject
{

    public override void OnInteractableObjectGrabbed(InteractableObjectEventArgs e)
    {

        // moving many triggers really pushes what unity is capable of
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (c.gameObject.name == "Ring")
            {
                ((MeshCollider)c).convex = true;
                break;
            }
        }
        GetComponent<NetworkHandler>().ToggleNetworkColliders(false);
        base.OnInteractableObjectGrabbed(e);
    }

    public override void OnInteractableObjectUngrabbed(InteractableObjectEventArgs e)
    {
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (c.gameObject.name == "Ring")
            {
                ((MeshCollider)c).convex = false;
                break;
            }
        }
        GetComponent<NetworkHandler>().ToggleNetworkColliders(true);
        base.OnInteractableObjectUngrabbed(e);
    }
}