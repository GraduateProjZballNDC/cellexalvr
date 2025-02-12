﻿using CellexalVR.AnalysisLogic;
using CellexalVR.Spatial;
using UnityEngine;
using System.Collections;

namespace CellexalVR.Menu.Buttons.Slicing
{
    public class SpreadMeshPartsButton : CellexalButton
    {
        protected override string Description => "Spread/Gather mesh parts";

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Click()
        {
            AllenReferenceBrain.instance.Spread();
        }
    }
}
