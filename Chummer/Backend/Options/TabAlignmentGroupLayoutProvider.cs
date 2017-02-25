﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Point = System.Drawing.Point;

namespace Chummer.Backend.Options
{
    /*
     * This class contains the logic used (at time of writing) for basic layout of (everything) when autogenerating
     * interfaces. (Only used by options at time of writing). This contains the layouting part of autogenerated interfaces.
     * It cooperates with OptionRender.cs to do the actual rendering (well, adding stuff to a control)
     * I'm not very versed in WPF, but it _should_ work with anything with uniform text size.
     *
     * It is named as so, because it provides layout, it uses tab to align stuff and thats it. Anybody feel free to refactor to a better name.
     * As it is still under development, it contains quite a few debug statements. Those _should_ be removed before release
     */
    public class TabAlignmentGroupLayoutProvider : IGroupLayoutProvider
    {
        private class LineInfo
        {
            public int Parts { get; set; }
            public int ControlIndex { get; set; } = -1;
        }
        //The layout procedure is rather simple (in theory).  It interprents multiple \t inside text and makes sure every
        //tab accros multiple lines share same end
        //Basically the same as normal text tab, but instead of having a defined lenght in spaces, it findes
        //the minimum possible accross all, without exeeding it
        //In other words, after every \t the next following character will align on all lines


        /// <summary>
        /// Various tweakable settings that can be changed
        /// </summary>
        public LayoutOptionsContainer LayoutOptions { get; set; } = new LayoutOptionsContainer();

        public object ComputeLayoutSpacing(Graphics rendergarget, List<LayoutLineInfo> contents, List<int> additonalConformTarget = null)
        {
            List<int> alignmentLenghts = additonalConformTarget ?? new List<int>();
            StringBuilder builder = new StringBuilder();
            List<LineInfo> controlIndex = new List<LineInfo>();
            List<CharacterRange> ranges = new List<CharacterRange>();
            List<Region> regions = new List<Region>();
            StringFormat format = new StringFormat();
            int count;
            foreach (LayoutLineInfo line in contents)
            {
                string[] alignmentPieces = line.LayoutString.Split(new[] { "\\t" }, StringSplitOptions.None);
                LineInfo currentLine = new LineInfo
                {
                    Parts = alignmentPieces.Length
                };
//Console.WriteLine("Working on big piece \"{0}\" {1}", line.LayoutString, regions.Count + ranges.Count);
                for (int i = 0; i < alignmentPieces.Length; i++)
                {
//                    Console.Write("Working on part \"{0}\"", alignmentPieces[i]);
                    if (alignmentPieces[i].Contains("{}"))
                    {
//                        Console.WriteLine(" double");
                        currentLine.ControlIndex = i + 1;
                        currentLine.Parts++;
                        string[] sides = alignmentPieces[i].Split(split, 2, StringSplitOptions.None);

                        ranges.Add(new CharacterRange(builder.Length, sides[0].Length));
                        builder.Append(sides[0]);
                        //builder.Append('\n');
                        UpdateRegions(builder, regions, ranges, format, rendergarget);

                        ranges.Add(new CharacterRange(builder.Length, sides[1].Length));
                        builder.Append(sides[1]);
                        //builder.Append('\n');
                        UpdateRegions(builder, regions, ranges, format, rendergarget);
                    }
                    else
                    {
//                        Console.WriteLine(" single");
                        ranges.Add(new CharacterRange(builder.Length, alignmentPieces[i].Length));
                        builder.Append(alignmentPieces[i]);
                        //builder.Append('\n');
                        UpdateRegions(builder, regions, ranges, format, rendergarget);
                    }

                }
                controlIndex.Add(currentLine);
            }


            UpdateRegions(builder, regions, ranges, format, rendergarget, force:true);




            //Keeps the mimumum size required for each tab.
            //Loop over every line in the layout and calculate how big elements need to be
            int regionCount = 0;
            for (int j = 0; j < contents.Count; j++)
            {
                LayoutLineInfo line = contents[j];
                string[] alignmentPieces = line.LayoutString.Split(new[] { "\\t" }, StringSplitOptions.None);
//                Console.WriteLine($"splitting {line.LayoutString} into [{string.Join(", ", alignmentPieces)}]");
//                Console.WriteLine("content[{0}]", j);
                //For every aligntment piece (piece of text with \t before/after) find out how large it needs to be
                //If a previous size requirement for that tab count is already found, take the biggest one
                //Otherwise store a new one
                for (int i = 0; i < alignmentPieces.Length; i++)
                {
//                    Console.WriteLine("Starting on {0}", regionCount);
//                    Console.WriteLine("Processing {0}", alignmentPieces[i]);
                    RectangleF rect = regions[regionCount++].GetBounds(rendergarget);
                    float size = rect.Width;
                    if (alignmentPieces[i].Contains("{}") /*controlIndex[j].ControlIndex == 1 */)
                    {
                        size += line.ControlSize.Width + regions[regionCount++].GetBounds(rendergarget).Width;
                    }
                    if (alignmentLenghts.Count > i)
                    {

                        alignmentLenghts[i] = Math.Max(alignmentLenghts[i], (int)size);
                    }
                    else
                    {
                        alignmentLenghts.Add((int)size);
                    }
                }
            }

//            Console.WriteLine("Cache count = {0}", regions.Count);
            return regions;
        }

        private void UpdateRegions(StringBuilder builder, List<Region> regions, List<CharacterRange> ranges, StringFormat format, Graphics target, bool force = false)
        {
            if (ranges.Count >= 32 || force)
            {
                format.SetMeasurableCharacterRanges(ranges.ToArray());
                var r = target.MeasureCharacterRanges(builder.ToString(), LayoutOptions.Font,
                    new RectangleF(0, 0, float.MaxValue, float.MaxValue), format);
                regions.AddRange(r);

                //Console.WriteLine("=======REGIONS=======");
                for (var index = 0; index < r.Length; index++)
                {
                    Region region = r[index];

                    //Console.WriteLine($"({region.GetBounds(target).Width / ranges[index].Length :0000.00})\t{ranges[index].First},{ranges[index].Length}\"{builder.ToString(ranges[index].First, ranges[index].Length)}\"\t{region.GetBounds(target)}");
                }

                //Console.WriteLine("=====================");

                builder.Clear();
                ranges.Clear();
            }
        }

        /// <summary>
        /// Transforms a list of LayoutLineInfo that contains a size of a control element and supporting text into a
        /// list of positions for the control elements and strings and positions for labels.
        /// Supporting text can be broken down into smaller labels to make space for control elements inside the text,
        /// or to align with other parts
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public LayoutRenderInfo PerformLayout(Graphics renderGraphics, List<LayoutLineInfo> contents,
            List<int> preComputedLayoutSpacing, object CachedCompute)
        {
            int cacheIndex = 0;
            List<Region> cache = (List<Region>) CachedCompute;

            Console.WriteLine("Cache count = {0}", cache.Count);
            //Create the container with all return information
            LayoutRenderInfo ret = new LayoutRenderInfo()
            {
                ControlLocations = new List<Point>(),
                TextLocations = new List<TextRenderInfo>()
            };





            //After previous loop alignmentLenghts contain the the size of each group.
            //The following lines convert this to pixel offsets for each group

            //First group should start at 0, not reservering a free space to house a duplicate for first group.
            List<int> alignmentLenghts = new List<int> {0};
            alignmentLenghts.AddRange(preComputedLayoutSpacing);
            Console.WriteLine(string.Join(", ", alignmentLenghts));

            //Then add the sum of all prievious elements to all elements, convering lenghts to starting indexes
            for (int i = 1; i < alignmentLenghts.Count; i++)
            {
                alignmentLenghts[i] += alignmentLenghts[i - 1];
//                Console.WriteLine($"[{i}] <- {alignmentLenghts[i]} added {alignmentLenghts[i - 1]}");
            }

            //LineTop defines the top of the current line, lineButtom keeps track of how far down it goes
            int lineTop = 0;
            int lineBottom = 0;

            //Perform the actual layout
            int regionCount = 0;
            for (int j = 0; j < contents.Count; j++)
            {
                int lineRight = 0;
                LayoutLineInfo line = contents[j];
                string[] alignmentPieces = line.LayoutString.Split(new[] {"\\t"}, StringSplitOptions.None);
                //Console.WriteLine($"splitting {line.LayoutString} into [{string.Join(", ", alignmentPieces)}]");
//                Console.WriteLine("content[{0}]", j);
                //For every aligntment piece (piece of text with \t before/after) find out how large it needs to be
                //If a previous size requirement for that tab count is already found, take the biggest one
                //Otherwise store a new one
                bool controlRendered = false;
                for (int i = 0; i < alignmentPieces.Length; i++)
                {
//                    Console.WriteLine("Starting on {1},{0}", regionCount, j);
//                    Console.WriteLine("Processing {0}", alignmentPieces[i]);
                    Size size = ToSize(cache[regionCount++].GetBounds(renderGraphics));

                    if (!controlRendered && alignmentPieces[i].Contains("{}"))
                    {
                        string[] sides = alignmentPieces[i].Split(split, 2, StringSplitOptions.None);
                        Size size2 = ToSize(cache[regionCount++].GetBounds(renderGraphics));
                        TextRenderInfo tri = new TextRenderInfo
                        {
                            Location = new Point(alignmentLenghts[i], lineTop),
                            Size = size,
                            Text = sides[0]
                        };
                        //Console.WriteLine($"Rendering tri \"{tri.Text}\" at {tri.Location.X},{tri.Location.Y} ({tri.Size.Width},{tri.Size.Height})");
                        ret.TextLocations.Add(new TextRenderInfo
                        {
                            Location =
                                new Point(
                                    alignmentLenghts[i] + size.Width + line.ControlSize.Width +
                                    LayoutOptions.ControlMargin * 2, lineTop),
                            Size = size2,
                            Text = sides[1]
                        });
                        ret.TextLocations.Add(tri);
                        ret.ControlLocations.Add(new Point(
                            alignmentLenghts[i] + size.Width + LayoutOptions.ControlMargin,
                            lineTop + line.ControlOffset.Y));

                        lineBottom = Math.Max(lineBottom,
                            lineTop + Math.Max(size.Height,
                                line.ControlSize.Height - Math.Min(0, line.ControlOffset.Y)));
                        controlRendered = true;
                    }
                    else
                    {
                        TextRenderInfo tri = new TextRenderInfo
                        {
                            Location = new Point(alignmentLenghts[i], lineTop),
                            Size = size,
                            Text = alignmentPieces[i]
                        };
                        //Console.WriteLine($"Rendering tri \"{tri.Text}\" at {tri.Location.X},{tri.Location.Y} ({tri.Size.Width},{tri.Size.Height})");
                        ret.TextLocations.Add(tri);
                        lineBottom = Math.Max(lineBottom, lineTop + size.Height);

                        lineRight = alignmentLenghts[i] + size.Width;
                    }
                }

                if (!controlRendered)
                {
                    ret.ControlLocations.Add(new Point(lineRight + LayoutOptions.ControlMargin,
                        lineTop + line.ControlOffset.Y));
                }

//                Console.WriteLine("EOL {0} -> {1}(+{2})", lineTop, lineBottom + LayoutOptions.Linespacing, (lineBottom + LayoutOptions.Linespacing) - lineTop);


                lineTop = lineBottom + LayoutOptions.Linespacing;
            }
            return ret;
        }

        private Size ToSize(RectangleF getBounds)
        {
            //Console.WriteLine("Rectangle of {0} {1}", getBounds.Width, getBounds.Height);
            return new Size((int)getBounds.Width, (int)getBounds.Height);
        }

        private readonly SizeF Big = new SizeF(int.MaxValue, int.MaxValue);
        private string[] split = new[] {"{}"};
        private Size ElementSize(Graphics g, string textMaybeEmbeddedControl, Size controlSize, Point controlOffset)
        {
            if(string.IsNullOrWhiteSpace(textMaybeEmbeddedControl)) return Size.Empty;

            //Either calculate the size of text, or black magic to calculate the size of control and 2 pieces of text to suround it
            if (textMaybeEmbeddedControl.Contains("{}"))
            {
                string[] sides = textMaybeEmbeddedControl.Split(split, 2, StringSplitOptions.None);

                SizeF s1, s2;
                s1 = g.MeasureString(sides[0], LayoutOptions.Font, Big, StringFormat.GenericTypographic);
                s2 = g.MeasureString(sides[1], LayoutOptions.Font, Big, StringFormat.GenericTypographic);

                //TODO: this should be how far the total element goes outside the upper left cornor of the text. Any that the control goes over should be ignored.
                //Probably confused because i don't quite see what controlOffset.X means
                float height = Math.Max(s1.Height, controlSize.Height - Math.Min(0, controlOffset.Y));
                float width = controlSize.Width + s1.Width + s2.Width; //Probably sane way
                Console.WriteLine($"Calculated width = {width} from {controlSize.Width} +{s1.Width} + {s2.Width} \"{textMaybeEmbeddedControl}\"");
                return new Size((int)width, (int)height);
            }
            else
            {
                SizeF s = g.MeasureString(textMaybeEmbeddedControl, LayoutOptions.Font, Big, StringFormat.GenericTypographic);
                //s.Width -= 5;
                return new Size((int)s.Width, (int)s.Height);
            }
        }

        
    }
}