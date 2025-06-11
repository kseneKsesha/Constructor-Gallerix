using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Constructor_Gallerix.Templates
{
    public class MasonryPanel : Panel
    {
        public int Columns { get; set; } = 3;
        public double Spacing { get; set; } = 10;

        protected override Size MeasureOverride(Size availableSize)
        {
            if (InternalChildren.Count == 0)
                return new Size(0, 0);

            if (double.IsInfinity(availableSize.Width))
                availableSize = new Size(2000, availableSize.Height);

            foreach (UIElement child in InternalChildren)
                child.Measure(new Size(availableSize.Width / Columns, double.PositiveInfinity));

            return new Size(availableSize.Width, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (InternalChildren.Count == 0)
                return finalSize;

            double columnWidth = (finalSize.Width - (Columns - 1) * Spacing) / Columns;
            double[] columnHeights = new double[Columns];
            List<Rect> childRects = new List<Rect>();

            foreach (UIElement child in InternalChildren)
            {
                int minColumn = GetMinColumn(columnHeights);
                double x = minColumn * (columnWidth + Spacing);
                double y = columnHeights[minColumn];

                Size desired = child.DesiredSize;
                double scale = columnWidth / desired.Width;
                double height = desired.Height * scale;

                Rect rect = new Rect(x, y, columnWidth, height);
                childRects.Add(rect);

                columnHeights[minColumn] += height + Spacing;
            }

            for (int i = 0; i < InternalChildren.Count; i++)
                InternalChildren[i].Arrange(childRects[i]);

            double finalHeight = GetMaxHeight(columnHeights);
            return new Size(finalSize.Width, finalHeight);
        }

        private int GetMinColumn(double[] heights)
        {
            int index = 0;
            double min = heights[0];
            for (int i = 1; i < heights.Length; i++)
            {
                if (heights[i] < min)
                {
                    min = heights[i];
                    index = i;
                }
            }
            return index;
        }

        private double GetMaxHeight(double[] heights)
        {
            double max = 0;
            foreach (var h in heights)
                if (h > max) max = h;
            return max;
        }
    }
}