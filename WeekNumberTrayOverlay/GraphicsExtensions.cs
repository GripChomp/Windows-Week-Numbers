using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WeekNumberTrayOverlay
{
    public static class GraphicsExtensions
    {
        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, float x, float y, float width, float height, float radius)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            using (GraphicsPath path = RoundedRect(x, y, width, height, radius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, float x, float y, float width, float height, float radius)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            using (GraphicsPath path = RoundedRect(x, y, width, height, radius))
            {
                graphics.FillPath(brush, path);
            }
        }

        private static GraphicsPath RoundedRect(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2;

            RectangleF arcRect = new RectangleF(x, y, diameter, diameter);
            
            // Top left corner
            path.AddArc(arcRect, 180, 90);

            // Top right corner
            arcRect.X = x + width - diameter;
            path.AddArc(arcRect, 270, 90);

            // Bottom right corner
            arcRect.Y = y + height - diameter;
            path.AddArc(arcRect, 0, 90);

            // Bottom left corner
            arcRect.X = x;
            path.AddArc(arcRect, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
} 