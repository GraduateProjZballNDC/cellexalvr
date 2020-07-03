﻿using UnityEngine;
using System.Collections;
using CellexalVR.General;
using System.Collections.Generic;
using CellexalVR.Interaction;
using CellexalVR.Menu.Buttons;
using TMPro;
using UnityEngine.UI;

namespace CellexalVR.Menu.SubMenus
{
    public class MenuWithoutTabs : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public bool Active { get; set; }

        protected List<CellexalButton> cellexalButtons = new List<CellexalButton>();

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        public virtual CellexalButton FindButton(string name)
        {
            var button = cellexalButtons.Find(x => x.name == name);
            return button;
        }

        public virtual void SetMenuActive(bool toggle)
        {
            Active = toggle;
            foreach (MeshRenderer rend in GetComponentsInChildren<MeshRenderer>(true))
            {
                rend.enabled = toggle;
            }

            foreach (Image image in GetComponentsInChildren<Image>(true))
            {
                image.enabled = toggle;
            }

            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
            {
                canvas.enabled = toggle;
            }
        }
    }
}