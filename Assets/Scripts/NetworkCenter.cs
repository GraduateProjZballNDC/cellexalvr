﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using VRTK.GrabAttachMechanics;
using VRTK.SecondaryControllerGrabActions;

/// <summary>
/// Represents the center of a network. It handles the enlarging when it is pressed.
/// </summary>
public class NetworkCenter : MonoBehaviour
{
    public GameObject replacementPrefab;
    public GameObject edgePrefab;
    public GameObject arcDescriptionPrefab;
    public GameObject simpleArcDescriptionPrefab;
    public List<Color> combinedArcsColors;
    public NetworkHandler Handler { get; set; }
    public string NetworkCenterName;

    private ControllerModelSwitcher controllerModelSwitcher;
    // The network will pop up above the pedestal gameobject when it's enlarged.
    private GameObject pedestal;
    private SteamVR_Controller.Device device;
    private bool controllerInside = false;
    private Vector3 oldLocalPosition;
    private Vector3 oldScale;
    private Quaternion oldRotation;
    private Transform oldParent;
    public bool Enlarged { get; private set; }
    private bool enlarge = false;
    private int numColliders = 0;
    private bool isReplacement = false;
    private List<NetworkNode> nodes = new List<NetworkNode>();
    [HideInInspector]
    public NetworkCenter replacing;
    private List<Arc> arcs = new List<Arc>();
    private List<CombinedArc> combinedArcs = new List<CombinedArc>();
    private SteamVR_TrackedObject rightController;
    private NetworkGenerator networkGenerator;
    private GameManager gameManager;

    void Start()
    {
        var referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
        pedestal = GameObject.Find("Pedestal");
        rightController = referenceManager.rightController;
        networkGenerator = referenceManager.networkGenerator;
        controllerModelSwitcher = referenceManager.controllerModelSwitcher;
        gameManager = referenceManager.gameManager;
    }

    void FixedUpdate()
    {
        // moving kinematic rigidbodies
        if (enlarge)
        {
            enlarge = false;
            if (!isReplacement && gameObject.name != "Enlarged Network")
            {
                gameManager.InformEnlargeNetwork(Handler.NetworkHandlerName, NetworkCenterName);
                EnlargeNetwork();
            }
            else if (isReplacement && gameObject.name == "EmptyNetworkPrefab 1(Clone)")
            {
                gameManager.InformBringBackNetwork(Handler.NetworkHandlerName, replacing.NetworkCenterName);
                BringBackOriginal();
            }
        }
    }

    void Update()
    {
        // handle input
        device = SteamVR_Controller.Input((int)rightController.index);
        if (controllerInside && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            controllerInside = false;
            numColliders = 0;
            enlarge = true;
        }
        if (gameObject.transform.hasChanged)
        {
            foreach (Arc a in arcs)
            {
                Vector3 midPoint1 = (a.t1.position + a.t2.position) / 2f;
                Vector3 midPoint2 = (a.t3.position + a.t4.position) / 2f;
                a.renderer.SetPositions(new Vector3[] { midPoint1, midPoint2 });
            }

            foreach (CombinedArc a in combinedArcs)
            {
                if (a.center1 != this)
                    a.renderer.SetPositions(new Vector3[] { transform.position, a.center2.position });
                else
                    a.renderer.SetPositions(new Vector3[] { transform.position, a.center1.position });
            }
        }

        var interactableObject = GetComponent<VRTK_InteractableObject>();
        if (interactableObject)
        {
            if (interactableObject.enabled)
            {
                gameManager.InformMoveNetworkCenter(Handler.NetworkHandlerName, NetworkCenterName, transform.position, transform.rotation, transform.localScale);
            }
        }
    }

    public void AddNode(NetworkNode newNode)
    {
        nodes.Add(newNode);
    }

    public void ApplyLayout()
    {
        //     StartCoroutine(ApplyLayoutCoroutine());
        // }
        // private IEnumerator ApplyLayoutCoroutine()
        // {
        //     transform.localScale *= 10f;

        float desiredSpringLength = 0.07f;
        float maximumForce = 0.03f;
        int iterations = 100;
        //int groupIterations = 100;
        float springConstant = 0.15f;
        float nonAdjecentNeighborConstant = 0.0003f;
        // start by giving all vertices a random position
        var rand = new System.Random();
        foreach (var node in nodes)
        {
            node.transform.localPosition = new Vector3((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f);
        }

        Dictionary<NetworkNode, Vector3> forces = new Dictionary<NetworkNode, Vector3>(nodes.Count);

        for (int i = 0; i < iterations; ++i)
        {

            // set all forces on all vertices to zero
            foreach (var node in nodes)
            {
                forces[node] = Vector3.zero;
            }

            // foreach (var node in nodes)
            // {
            //
            //     // add a slight bit of gravity towards the center
            //     forces[node] -= node.transform.localPosition * 0.0002f * nodes.Count;
            // }

            // calculate how much force is applied to each end of each spring
            foreach (var node in nodes)
            {
                foreach (var neighbour in node.neighbours)
                {
                    var diff = (neighbour.transform.localPosition - node.transform.localPosition);
                    var dir = diff.normalized;
                    // the springs should be longer if there are many neighbours, spreading out crowded areas
                    var appropriateSpringLength = desiredSpringLength /* *Mathf.Log(node.neighbours.Count + 1, 2)*/;
                    var appliedForce = diff * Mathf.Log(diff.magnitude / appropriateSpringLength) / node.neighbours.Count;
                    //if (appliedForce.magnitude < minimumForce)
                    //    continue;
                    forces[node] += appliedForce * springConstant;
                }
            }

            // move all nonadjecent nodes away from eachother
            foreach (var node in nodes)
            {
                foreach (var nonNeighbour in nodes)
                {
                    if (node == nonNeighbour || node.neighbours.Contains(nonNeighbour))
                        continue;
                    var distance = Vector3.Distance(node.transform.localPosition, nonNeighbour.transform.localPosition);
                    if (distance > 0.1f)
                        continue;
                    var dir = (nonNeighbour.transform.localPosition - node.transform.localPosition);
                    var appliedForce = dir.normalized / (distance * distance * nodes.Count);
                    //if (appliedForce.magnitude > maximumForce)
                    //    appliedForce = appliedForce.normalized * maximumForce;
                    //  if (appliedForce.magnitude < minimumForce)
                    //    continue;
                    forces[node] -= appliedForce * nonAdjecentNeighborConstant;
                }
            }

            //   foreach (var node1 in nodes)
            //   {
            //       foreach (var neighbour1 in node1.neighbours)
            //       {
            //           foreach (var node2 in nodes)
            //           {
            //               if (node1 == node2 || neighbour1 == node2)
            //                   continue;
            //
            //               foreach (var neighbour2 in node2.neighbours)
            //               {
            //                   if (node1 == neighbour2 || neighbour1 == neighbour2)
            //                       continue;
            //
            //                   float angle1  =Vector3.Angle()
            //               }
            //           }
            //       }
            //   }



            // move all vertices according to the force affecting them
            foreach (var force in forces)
            {
                var node = force.Key;
                node.transform.localPosition += force.Value;
                // move all vertices that are outside the circle to the edge
                if (node.transform.localPosition.magnitude > 0.4f)
                {
                    node.transform.localPosition = node.transform.localPosition.normalized * 0.4f;
                }
            }

            //   do
            //       yield return null;
            //   while (!Input.GetKey(KeyCode.T));
        }
        Handler.layoutApplied = true;
    }

    private class Spring<T>
    {
        public T item1;
        public T item2;


        public Spring(T item1, T item2)
        {
            this.item1 = item1;
            this.item2 = item2;

        }

        public override bool Equals(object obj)
        {
            Spring<T> other = obj as Spring<T>;
            if (other == null)
                return false;
            return item1.Equals(other.item1) && item2.Equals(other.item2) || item1.Equals(other.item2) && item2.Equals(other.item1);
        }

        public override int GetHashCode()
        {
            return item1.GetHashCode() + item2.GetHashCode();
        }
    }

    private Vector3 MeanPosition(List<NetworkNode> nodes)
    {
        Vector3 result = Vector3.zero;
        foreach (var node in nodes)
        {
            result += node.transform.localPosition;
        }
        result /= nodes.Count;
        return result;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Menu Controller Collider"))
        {
            controllerInside = true;
            numColliders++;
            // i think this sometimes gets called before start
            // so let's make sure that the controllermodelswitcher is set
            if (controllerModelSwitcher != null)
                controllerModelSwitcher.SwitchToModel(ControllerModelSwitcher.Model.Menu);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Menu Controller Collider"))
        {
            numColliders--;
        }
        // We might collide with the network nodes' colliders. So OnTriggerExit is called a little too often,
        // so we must make sure we have exited all colliders.
        if (numColliders == 0)
        {
            controllerInside = false;
            controllerModelSwitcher.SwitchToDesiredModel();
        }
    }

    /// <summary>
    /// Hides the large sphere around the network if the network is enlarged. 
    /// The sphere should be hidden if the network is enlarged.
    /// </summary>
    internal void HideSphereIfEnlarged()
    {
        if (Enlarged)
        {
            GetComponent<Renderer>().enabled = false;
            GetComponent<Collider>().enabled = false;
        }
    }

    /// <summary>
    /// Called when the controller is inside the network and the trigger is pressed. Enlarges the network and seperates it from the skeleton and makes it movable by the user.
    /// </summary>
    public void EnlargeNetwork()
    {
        StartCoroutine(EnlargeNetworkCoroutine());
    }

    private IEnumerator EnlargeNetworkCoroutine()
    {
        this.name = "Enlarged Network";
        Enlarged = true;
        GetComponent<Renderer>().enabled = false;
        GetComponent<Collider>().enabled = false;
        var rigidbody = gameObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = gameObject.AddComponent<Rigidbody>();
        }
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.angularDrag = float.PositiveInfinity;
        var interactableObject = gameObject.AddComponent<NetworkCenterInteract>();
        interactableObject.isGrabbable = true;
        interactableObject.isUsable = false;
        var grabAttach = gameObject.AddComponent<VRTK_FixedJointGrabAttach>();
        var scalescript = gameObject.AddComponent<VRTK_AxisScaleGrabAction>();
        scalescript.uniformScaling = true;
        interactableObject.grabAttachMechanicScript = grabAttach;
        interactableObject.secondaryGrabActionScript = scalescript;

        grabAttach.precisionGrab = true;
        grabAttach.breakForce = float.PositiveInfinity;

        // save the old variables
        oldParent = transform.parent;
        oldLocalPosition = transform.localPosition;
        oldScale = transform.localScale;
        oldRotation = transform.rotation;

        transform.parent = null;
        transform.position = pedestal.transform.position + new Vector3(0, 1, 0);
        transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        transform.rotation = pedestal.transform.rotation;
        transform.Rotate(-90f, 0, 0);

        // instantiate a replacement in our place
        var replacement = Instantiate(replacementPrefab);
        replacement.transform.parent = oldParent;
        replacement.transform.localPosition = oldLocalPosition;
        replacement.transform.rotation = oldRotation;
        replacement.transform.localScale = oldScale;
        replacement.GetComponent<Renderer>().material.color = GetComponent<Renderer>().material.color;

        // make sure the replacement knows its place in the world
        var replacementScript = replacement.GetComponent<NetworkCenter>();
        replacementScript.isReplacement = true;
        replacementScript.replacing = this;
        replacementScript.Handler = Handler;

        // wait 1 frame before turning on the colliders, otherwise they all get triggered if
        // the controller is inside them
        yield return null;
        // turn on the colliders on the nodes so they can be highlighted
        foreach (BoxCollider b in GetComponentsInChildren<BoxCollider>())
        {
            b.enabled = true;
        }

        // tell our network handler that we have a replacement
        Handler.Replacements.Add(replacementScript);
    }

    /// <summary>
    /// If this network is enlarged, bring it back to the convex hull, if it is a replacement, destroy it and bring back the original 
    /// </summary>
    public void BringBackOriginal()
    {
        this.name = "Original Network";
        if (isReplacement)
        {
            replacing.BringBackOriginal();
            Handler.Replacements.Remove(this);
            // destroying without this also caused crashes
            Destroy(GetComponent<Collider>());
            Destroy(GetComponent<Renderer>());
            //rightController.gameObject.GetComponentInChildren<VRTK_InteractTouch>().ForceStopTouching();
            gameObject.SetActive(false);
            // calling Destroy without the time delay caused the program to crash pretty reliably
            Destroy(gameObject);
        }
        else
        {
            Enlarged = false;
            StartCoroutine(BringBackOriginalCoroutine());
        }
    }

    private IEnumerator BringBackOriginalCoroutine()
    {
        // the ForceStopInteracting waits until the end of the frame before it stops interacting
        // so we also have to wait one frame until proceeding
        gameObject.GetComponent<VRTK_InteractableObject>().ForceStopInteracting();
        yield return null;
        // now we can do things
        GetComponent<Renderer>().enabled = true;
        GetComponent<Collider>().enabled = true;
        transform.parent = oldParent;
        transform.localPosition = oldLocalPosition;
        transform.rotation = oldRotation;
        transform.localScale = oldScale;
        // this network will now be part of the convex hull which already has a rigidbody and these scripts
        Destroy(gameObject.GetComponent<NetworkCenterInteract>());
        Destroy(gameObject.GetComponent<VRTK_AxisScaleGrabAction>());
        Destroy(gameObject.GetComponent<VRTK_InteractableObject>());
        Destroy(gameObject.GetComponent<Rigidbody>());

        if (transform.localScale.x > 5)
        {
            Debug.Log("DECREASE OBJ IN SKY");
            networkGenerator.objectsInSky--;
        }
        // we must wait one more frame here or VRTK_InteractTouch gets a bunch of null exceptions.
        // probably because it is still using these colliders
        yield return null;
        // Disable the network nodes' colliders
        foreach (Transform child in transform)
        {
            var node = child.GetComponent<NetworkNode>();
            if (node)
            {
                node.BringBack();
                node.GetComponent<BoxCollider>().enabled = false;
            }
        }
    }

    /// <summary>
    /// An arc is a line between two identical pairs of genes in two different networks
    /// </summary>
    private struct Arc
    {
        public LineRenderer renderer;
        public NetworkCenter center1, center2;
        // t1 and t2 are the genes' transforms in the first network
        // t3 and t4 are the genes' transforms in the second network
        public Transform t1, t2, t3, t4;
        // the gameobject that represents the arc
        public GameObject gameObject;

        public Arc(LineRenderer renderer, NetworkCenter center1, NetworkCenter center2, Transform t1, Transform t2, Transform t3, Transform t4, GameObject gameObject)
        {
            this.renderer = renderer;
            this.center1 = center1;
            this.center2 = center2;
            this.t1 = t1;
            this.t2 = t2;
            this.t3 = t3;
            this.t4 = t4;
            this.gameObject = gameObject;
        }
    }

    /// <summary>
    /// A combined arc is a line between two networks that represents the number of normal arcs between those networks.
    /// </summary>
    private struct CombinedArc
    {
        public LineRenderer renderer;
        public Transform center1, center2;
        public GameObject gameObject;
        public int nArcsCombined;

        public CombinedArc(LineRenderer renderer, Transform center1, Transform center2, int nArcsCombined, GameObject gameObject)
        {
            this.renderer = renderer;
            this.center1 = center1;
            this.center2 = center2;
            this.nArcsCombined = nArcsCombined;
            this.gameObject = gameObject;
        }
    }

    /// <summary>
    /// Adds an arc between two pairs of genes.
    /// </summary>
    /// <param name="n1"> The first gene in the first pair </param>
    /// <param name="n2"> The second gene in the first pair </param>
    /// <param name="n3"> The first gene in the second pair </param>
    /// <param name="n4"> The second gene in the second pair </param>
    internal void AddArc(NetworkNode n1, NetworkNode n2, NetworkNode n3, NetworkNode n4)
    {
        GameObject edge = Instantiate(edgePrefab);
        LineRenderer renderer = edge.GetComponent<LineRenderer>();
        edge.transform.parent = transform.parent;
        Vector3 midPoint1 = (n1.transform.position + n2.transform.position) / 2f;
        Vector3 midPoint2 = (n3.transform.position + n4.transform.position) / 2f;
        renderer.useWorldSpace = true;
        renderer.SetPositions(new Vector3[] { midPoint1, midPoint2 });

        Arc newArc = new Arc(renderer, n1.Center, n3.Center, n1.transform, n2.transform, n3.transform, n4.transform, edge);
        arcs.Add(newArc);
        n3.Center.arcs.Add(newArc);

        GameObject arcText = Instantiate(arcDescriptionPrefab);
        arcText.transform.parent = edge.transform;
        arcText.transform.position = (midPoint1 + midPoint2) / 2f;
        arcText.GetComponent<TextRotator>().SetTransforms(n1.transform, n2.transform, n3.transform, n4.transform);
        arcText.GetComponent<TextMesh>().text = n1.geneName.text + " <-> " + n2.geneName.text;
    }

    /// <summary>
    /// Shows or hides all normal arcs connected to this network.
    /// </summary>
    /// <param name="toggleToState"> The state to toggle to, true for visible false for invisible. </param>
    public void SetArcsVisible(bool toggleToState)
    {
        foreach (Arc arc in arcs)
        {
            arc.gameObject.SetActive(toggleToState);
        }
    }

    /// <summary>
    /// Shows or hides all combined arcs connected to this network.
    /// </summary>
    /// <param name="toggleToState"> The state to toggle to, true for visible false for invisible. </param>
    public void SetCombinedArcsVisible(bool toggleToState)
    {
        foreach (CombinedArc arc in combinedArcs)
        {
            arc.gameObject.SetActive(toggleToState);
        }
    }

    /// <summary>
    /// Creates combined arcs.
    /// A combined arc is a colored line that represents the number of normal arcs that go from this network to another.
    /// </summary>
    /// <returns> The maximum number of arcs that were combined to one. </returns>
    public int CreateCombinedArcs()
    {
        if (combinedArcs.Count > 0)
        {
            return 0;
        }

        Dictionary<NetworkCenter, int> nArcs = new Dictionary<NetworkCenter, int>();
        foreach (Arc arc in arcs)
        {
            if (arc.center1 != this)
            {
                if (nArcs.ContainsKey(arc.center1))
                    nArcs[arc.center1]++;
                else
                    nArcs[arc.center1] = 1;
            }
            else
            {
                if (nArcs.ContainsKey(arc.center2))
                    nArcs[arc.center2]++;
                else
                    nArcs[arc.center2] = 1;
            }
        }
        var max = 0;
        foreach (KeyValuePair<NetworkCenter, int> pair in nArcs)
        {
            if (pair.Key.combinedArcs.Count == 0)
            {
                if (pair.Value > max)
                    max = pair.Value;
                GameObject edge = Instantiate(edgePrefab);
                LineRenderer renderer = edge.GetComponent<LineRenderer>();
                edge.transform.parent = transform.parent;
                renderer.useWorldSpace = true;
                renderer.SetPositions(new Vector3[] { transform.position, pair.Key.transform.position });

                GameObject arcText = Instantiate(simpleArcDescriptionPrefab);
                arcText.transform.parent = edge.transform;
                arcText.transform.position = (transform.position + pair.Key.transform.position) / 2f;
                arcText.transform.localScale = arcText.transform.localScale * 2f;
                arcText.GetComponent<SimpleTextRotator>().SetTransforms(transform, pair.Key.transform);
                arcText.GetComponent<TextMesh>().text = "" + pair.Value;
                CombinedArc newArc = new CombinedArc(renderer, transform, pair.Key.transform, pair.Value, edge);
                combinedArcs.Add(newArc);
            }
        }
        return max;
    }

    /// <summary>
    /// Colors the combined arcs based on the their combined amount of arcs
    /// </summary>
    /// <param name="max"> The number of arcs that were combined at most. </param>
    internal void ColorCombinedArcs(int max)
    {
        foreach (CombinedArc arc in combinedArcs)
        {
            var colorIndex = (int)(Mathf.Floor(((float)(arc.nArcsCombined - 1) / max) * combinedArcsColors.Count));
            arc.renderer.startColor = combinedArcsColors[colorIndex];
            arc.renderer.endColor = combinedArcsColors[colorIndex];
        }
    }
}
