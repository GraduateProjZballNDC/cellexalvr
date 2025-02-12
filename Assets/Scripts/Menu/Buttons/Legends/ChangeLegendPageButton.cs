﻿using CellexalVR.AnalysisObjects;

namespace CellexalVR.Menu.Buttons.Legends
{
    public class ChangeLegendPageButton : CellexalButton
    {
        public GroupingLegend parentLegend;
        public bool forward;

        protected override string Description => "Change page " + (forward ? "forward" : "backward");

        public override void Click()
        {
            parentLegend.ChangePage(forward);
            if (parentLegend.name == "Selection Legend")
            {
                referenceManager.multiuserMessageSender.SendMessageSelectionLegendChangePage(forward);
            }
            else
            {
                referenceManager.multiuserMessageSender.SendMessageAttributeLegendChangePage(forward);
            }
        }
    }
}
