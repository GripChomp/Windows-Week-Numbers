using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WeekNumberTrayOverlay
{
    public class Retro95Effects
    {
        private System.Windows.Forms.Timer animationTimer;
        private float animationProgress = 0f;
        private readonly OverlayForm parentForm;
        private readonly List<Sparkle> sparkles = new List<Sparkle>();
        private readonly Random random = new Random();
        private bool isHovering = false;

        public Retro95Effects(OverlayForm form)
        {
            parentForm = form;
            animationTimer = new System.Windows.Forms.Timer
            {
                Interval = 16 // ~60fps
            };
            animationTimer.Tick += AnimationTimer_Tick;
            
            // Create initial sparkles
            for (int i = 0; i < 5; i++)
            {
                sparkles.Add(CreateRandomSparkle());
            }
        }

        public void StartHoverAnimation()
        {
            isHovering = true;
            animationProgress = 0f;
            animationTimer.Start();
        }

        public void StopHoverAnimation()
        {
            isHovering = false;
            animationProgress = 0f;
            animationTimer.Start(); // Start the reverse animation
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (isHovering)
            {
                // Forward animation (0 to 1)
                animationProgress += 0.02f; // Adjust for speed (1.4 seconds = ~0.012, but slightly faster looks better)
                if (animationProgress >= 1.0f)
                {
                    animationProgress = 1.0f;
                    animationTimer.Stop();
                }
            }
            else
            {
                // Reverse animation (1 to 0)
                animationProgress -= 0.04f; // Faster reverse animation
                if (animationProgress <= 0f)
                {
                    animationProgress = 0f;
                    animationTimer.Stop();
                }
            }

            // Update sparkles
            for (int i = 0; i < sparkles.Count; i++)
            {
                sparkles[i].Update();
                if (sparkles[i].Alpha <= 0)
                {
                    sparkles[i] = CreateRandomSparkle();
                }
            }

            parentForm.Invalidate(); // Force redraw
        }

        public void DrawRetro95Border(Graphics g, Rectangle bounds, int borderWidth)
        {
            // Draw pixelated border
            using (Pen borderPen = new Pen(Color.Black, borderWidth))
            {
                // Pixel-like border effect
                borderPen.Alignment = PenAlignment.Inset;
                g.DrawRectangle(borderPen, 0, 0, bounds.Width - 1, bounds.Height - 1);
            }
        }

        public void DrawRetro95Background(Graphics g, Rectangle bounds)
        {
            // Base background (off-white)
            using (Brush bgBrush = new SolidBrush(ThemeManager.GetBackgroundColor()))
            {
                g.FillRectangle(bgBrush, bounds);
            }

            if (animationProgress > 0)
            {
                // Calculate fill height based on animation progress
                int fillHeight = (int)(bounds.Height * animationProgress);
                
                // Draw the animated fill from bottom to top
                using (Brush hoverBrush = new SolidBrush(ThemeManager.GetHoverColor()))
                {
                    g.FillRectangle(hoverBrush, 
                        bounds.X, 
                        bounds.Bottom - fillHeight, 
                        bounds.Width, 
                        fillHeight);
                }

                // Draw sparkles if we're hovering and animation is in progress
                if (isHovering && animationProgress > 0.2f && animationProgress < 0.9f)
                {
                    foreach (var sparkle in sparkles)
                    {
                        sparkle.Draw(g);
                    }
                }
            }
        }

        private Sparkle CreateRandomSparkle()
        {
            int x = random.Next(parentForm.ClientSize.Width - 10);
            int y = random.Next(parentForm.ClientSize.Height - 10);
            int size = random.Next(2, 5);
            int lifespan = random.Next(20, 60);
            return new Sparkle(x, y, size, lifespan);
        }

        // Pixel-style text rendering
        public void DrawPixelText(Graphics g, string text, Font font, Rectangle bounds)
        {
            Color textColor = animationProgress > 0.5f ? 
                ThemeManager.GetHoverTextColor() : 
                ThemeManager.GetTextColor();
            
            using (Brush textBrush = new SolidBrush(textColor))
            {
                // Measure string to center it
                SizeF textSize = g.MeasureString(text, font);
                float x = (bounds.Width - textSize.Width) / 2;
                float y = (bounds.Height - textSize.Height) / 2;
                
                // Draw pixelated text
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                g.DrawString(text, font, textBrush, x, y);
            }
        }

        // Sparkle class for retro animation
        private class Sparkle
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Size { get; set; }
            public int Lifespan { get; set; }
            public int Age { get; set; }
            public int Alpha => 255 * (Lifespan - Age) / Lifespan;

            public Sparkle(int x, int y, int size, int lifespan)
            {
                X = x;
                Y = y;
                Size = size;
                Lifespan = lifespan;
                Age = 0;
            }

            public void Update()
            {
                Age++;
                // Optional: Add movement to sparkles
                Y -= 1; // Move up slowly
            }

            public void Draw(Graphics g)
            {
                if (Age < Lifespan)
                {
                    // Draw a simple pixel sparkle
                    using (Brush sparkBrush = new SolidBrush(Color.FromArgb(Alpha, 255, 255, 255)))
                    {
                        g.FillRectangle(sparkBrush, X, Y, Size, Size);
                    }
                }
            }
        }
    }
} 