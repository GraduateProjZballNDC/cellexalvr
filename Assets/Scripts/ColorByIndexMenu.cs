﻿using UnityEngine;

/// <summary>
/// Represents the sub menu that pops up when the <see cref="ColorByIndexButton"/> is pressed.
/// </summary>
public class ColorByIndexMenu : DynamicButtonMenu
{
    protected Color[] colors = new Color[0];
    protected override Color[] Colors
    {
        get
        {
            return colors;
        }
    }

    /// <summary>
    /// Create the buttons that represent the different indices.
    /// </summary>
    /// <param name="names">The name if the facs measurements.</param>
    public override void CreateButtons(string[] names)
    {
        base.CreateButtons(names);

        for (int i = 0; i < buttons.Count; ++i)
        {
            var b = buttons[i].GetComponent<ColorByIndexButton>();
            b.referenceManager = referenceManager;
            b.SetIndex(names[i]);
        }
    }
}
