﻿using System.Collections.Generic;
using System.Drawing;

namespace Chummer.Backend.Options
{
    public interface IGroupLayoutProvider
    {

        object ComputeLayoutSpacing(Graphics rendergarget, List<LayoutLineInfo> contents, List<int> additonalConformTarget = null);
        LayoutRenderInfo PerformLayout(Graphics renderGraphics, List<LayoutLineInfo> contents, List<int> preComputedLayoutSpacing, object CachedCompute);
        LayoutOptionsContainer LayoutOptions { get; set; }
    }

    public class LayoutLineInfo
    {
        public Size ControlSize { get; set; }
        public Point ControlOffset { get; set; }
        public string LayoutString { get; set; }
    }

    public class LayoutRenderInfo
    {
        public List<Point> ControlLocations { get; set; }
        public List<TextRenderInfo> TextLocations { get; set; }
    }

    public class TextRenderInfo
    {
        public Point Location { get; set; }
        public Size Size { get; set; }
        public string Text { get; set; }
        public FontStyle Style { get; set; }
    }

    public class LayoutOptionsContainer
    {
        public int Linespacing { get; set; } = 6;
        public int ControlMargin { get; set; } = 3;
        public Font Font { get; set; }
    }
}