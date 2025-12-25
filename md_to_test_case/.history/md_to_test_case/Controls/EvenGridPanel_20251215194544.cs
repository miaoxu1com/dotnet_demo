using System;
using Avalonia;
using Avalonia.Controls;

namespace md_to_test_case.Controls
{
    public class EvenGridPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            int n = Children.Count;
            if (n == 0)
                return new Size();

            double W = double.IsInfinity(availableSize.Width) ? 100 : availableSize.Width;
            double H = double.IsInfinity(availableSize.Height) ? 100 : availableSize.Height;

            double aspect = W / Math.Max(1e-6, H);
            int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(n * aspect)));
            int rows = (int)Math.Ceiling((double)n / cols);

            double cellW = W / cols;
            double cellH = H / rows;

            Size childConstraint = new Size(cellW, cellH);
            foreach (var child in Children)
            {
                child.Measure(childConstraint);
            }

            return new Size(W, H);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int n = Children.Count;
            if (n == 0)
                return finalSize;

            double W = finalSize.Width;
            double H = finalSize.Height;

            double aspect = W / Math.Max(1e-6, H);
            int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(n * aspect)));
            int rows = (int)Math.Ceiling((double)n / cols);

            double cellW = W / cols;
            double cellH = H / rows;

            for (int i = 0; i < n; i++)
            {
                var child = Children[i];
                int row = i / cols;
                int col = i % cols;
                var rect = new Rect(col * cellW, row * cellH, cellW, cellH);
                child.Arrange(rect);
            }

            return finalSize;
        }
    }
}
