﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace CellexalVR.AnalysisLogic.H5reader
{
    public class ProjectionObjectScript : MonoBehaviour
    {
        public enum projectionType
        {
            p3D,
            p2D_sep,
            p3D_sep,
            p2D
        }

        public projectionType type;
        public string name = "unnamed-projection";
        public Dictionary<string, string> paths;
        public GameObject AnchorPrefab;
        public H5readerAnnotater h5readerAnnotater;
        public TextMeshProUGUI seperatedText;
        private List<GameObject> instantiatedGameObjects;

        public Dictionary<projectionType, string[]> menu_setup;
        private TextMeshProUGUI nameTextMesh;

        void Start()
        {
            paths = new Dictionary<string, string>();
            nameTextMesh = transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
        }

        public void AddToPaths(string key, string value)
        {
            if (!paths.ContainsKey(key))
            {
                paths.Add(key, value);
                h5readerAnnotater.config.Add(key + "_" + name, value);
            }
            else
            {
                paths[key] = value;
                h5readerAnnotater.config[key + "_" + name] = value;
            }
            

        }

        public void RemoveFromPaths(string key)
        {
            if (paths.ContainsKey(key))
            {
                paths.Remove(key);
                h5readerAnnotater.config.Remove(key + "_" + name);
            }

        }

        public void SwitchToSeparate()
        {
         
            
            switch (type)
            {
                case projectionType.p3D:
                    type = projectionType.p3D_sep;
                    break;
                case projectionType.p2D_sep:
                    type = projectionType.p2D;
                    break;
                case projectionType.p3D_sep:
                    type = projectionType.p3D;
                    break;
                case projectionType.p2D:
                    type = projectionType.p2D_sep;
                    break;
            }

            if (seperatedText.text == "sep")
            {
                seperatedText.text = "unsep";
            }
            else if (seperatedText.text == "unsep")
            {
                seperatedText.text = "sep";
            }

            foreach (string key in paths.Keys)
                h5readerAnnotater.config.Remove(key + "_" + name);

            Init(type);
        }


        public void ChangeName(string name)
        {
            print(name);
            this.name = name;
            nameTextMesh.text = name;
        }

        public void OnDestroy()
        {
            foreach (string key in paths.Keys)
                h5readerAnnotater.config.Remove(key + "_" + name);

            h5readerAnnotater.projectionObjectScripts.Remove(this);
            Destroy(this.gameObject);
        }

        public void Init(projectionType type)
        {
            this.type = type;
            paths = new Dictionary<string, string>();
            if(instantiatedGameObjects != null)
            {
                foreach (GameObject g in instantiatedGameObjects)
                    Destroy(g);
            }
            instantiatedGameObjects = new List<GameObject>();

            


            if (type == projectionType.p2D_sep || type == projectionType.p3D_sep)
                seperatedText.text = "unsep";
            else
                seperatedText.text = "sep";

            menu_setup = new Dictionary<projectionType, string[]>
            {
                 { projectionType.p3D, new string[] { "X", "vel" } },
                 { projectionType.p2D_sep, new string[] { "X", "Y", "velX", "velY" } },
                 { projectionType.p3D_sep, new string[] { "X", "Y", "Z", "velX", "velY", "velZ" } },
                 { projectionType.p2D, new string[] { "X", "velX"} }
            };

            int offset = 0;
            GameObject go;

            foreach (string anchor in menu_setup[type])
            {
                go = Instantiate(AnchorPrefab, gameObject.transform, false);
                go.transform.localPosition += Vector3.up * -10 * offset;
                go.GetComponentInChildren<LineScript>().type = anchor;
                offset++;
                go.GetComponent<TextMeshProUGUI>().text = anchor;
                instantiatedGameObjects.Add(go);

            }

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
