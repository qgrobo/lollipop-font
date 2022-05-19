using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace LollipopFont
{
    internal class Program
    {
        /// <summary>
        /// The text to draw in lollipop font.
        /// </summary>
        private const string word = "Lollipop";

        /// <summary>
        /// The path to directory for generated images.
        /// </summary>
        private const string workingDirectory = "../../out";

        /// <summary>
        /// The amount of generated images.
        /// </summary>
        private const int frames = 20;

        /// <summary>
        /// The size of output frame.
        /// </summary>
        private static readonly Size frameSize = new Size(1920, 1080);

        /// <summary>
        /// The size of lollipop font.
        /// </summary>
        private const int fontSize = 288;

        /// <summary>
        /// The position of font baseline.
        /// </summary>
        private const int horizon = 700;

        /// <summary>
        /// The random seed for color generator.
        /// </summary>
        private const int seed = 54;

        /// <summary>
        /// Dynamic diameter of circles.
        /// </summary>
        private static float nutDiameter = 0;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            if (!Directory.Exists(workingDirectory))
                Directory.CreateDirectory(workingDirectory);

            for (int i = 0; i < frames; ++i)
            {
                nutDiameter = 35.0f - (i % 20);
                Bitmap image1 = DrawWord(word);
                Bitmap bitmap = DrawReflection(image1, frameSize.Width, frameSize.Height);

                bitmap.Save(string.Format("{0}/{1:d4}.png", workingDirectory, i + 1));
            }
        }

        /// <summary>
        /// Draws the word in lollipop font with reflection.
        /// </summary>
        /// <param name="word">Symbols to draw.</param>
        /// <returns>Painted bitmap.</returns>
        private static Bitmap DrawWord(string word)
        {
            int w = word.Length * fontSize;
            int h = fontSize * 3;
            int margin = 5;

            Bitmap bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            graphics.Clear(Color.Black);

            FontFamily fontFamily = new FontFamily("Arial");
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            Rectangle rectangle = new Rectangle(0, fontSize, w, fontSize);

            GraphicsPath path = new GraphicsPath();
            path.AddString(word, fontFamily, (int)FontStyle.Bold, fontSize, rectangle, stringFormat);

            GraphicsPath path2 = new GraphicsPath();
            path2.AddString(word, fontFamily, (int)FontStyle.Bold, fontSize, rectangle, stringFormat);

            VisitPoints(path, path2, graphics, AddEllipseToPath);

            Random r = new Random(seed);
            int half = 15; // of pen amount
            int doubleStroke = 3 * 2; // the double width of stroke is increment of real pen width
            List<Color> colors = GetColors(half, r);
            Pen widest = new Pen(colors[0], half * 2 * doubleStroke);

            for (int i = 0; i < half * 2; ++i)
            {
                Color c = colors[i];
                Pen pen = new Pen(c, (half * 2 - i) * doubleStroke);
                pen.LineJoin = LineJoin.Round;
                if (i == 0)
                {
                    widest = pen;
                }
                graphics.DrawPath(pen, path2);
            }

            LinearGradientBrush brush = new LinearGradientBrush(
                rectangle,
                Color.White,
                Color.Gray,
                LinearGradientMode.Vertical);
            graphics.FillPath(brush, path);

            VisitPoints(path, null, graphics, DrawAndFillEllipse);

            path2.Widen(widest);
            bitmap = Crop(bitmap, path2, margin);

            return bitmap;
        }

        /// <summary>
        /// Crops image by path size with margins.
        /// </summary>
        /// <param name="bitmap">Cropping image.</param>
        /// <param name="path">Path to calculate a size.</param>
        /// <param name="margin">Margins to add to path size.</param>
        /// <returns>Cropped bitmap.</returns>
        private static Bitmap Crop(Bitmap bitmap, GraphicsPath path, int margin)
        {
            RectangleF bounds = path.GetBounds();
            RectangleF bounds2 = new RectangleF(
                bounds.X - margin,
                bounds.Y - margin,
                bounds.Width + margin * 2,
                bounds.Height + margin * 2);
            return bitmap.Clone(bounds2, bitmap.PixelFormat);
        }

        /// <summary>
        /// Mixes to colors.
        /// </summary>
        /// <param name="color">The first color to mix.</param>
        /// <param name="backColor">The secornd color to mix.</param>
        /// <param name="amount">The ratio between colors.</param>
        /// <returns>Resulting color.</returns>
        private static Color Blend(Color color, Color backColor, double amount)
        {
            byte r = (byte)(color.R * amount + backColor.R * (1 - amount));
            byte g = (byte)(color.G * amount + backColor.G * (1 - amount));
            byte b = (byte)(color.B * amount + backColor.B * (1 - amount));
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Generates a list of colors based on the spectrum.
        /// </summary>
        /// <returns>The list of generated colors.</returns>
        private static List<Color> GetColors(int half, Random r)
        {
            int start = 0;
            int range = 255;
            List<Color> colors = new List<Color>();

            int count = half * 2;
            for (int i = 0; i < count; ++i)
            {
                Color c;
                if (i < half)
                {
                    c = Color.FromArgb(start + r.Next(range), start + r.Next(range), start + r.Next(range));
                    colors.Add(c);
                }
                else
                {
                    Color o = colors[i % half];
                    c = Color.FromArgb(255 - o.R, 255 - o.G, 255 - o.B);
                    colors.Add(c);
                }
            }

            List<Color> palette = new List<Color>();
            Bitmap image1 = new Bitmap("../../resources/palette.png");
            int w = image1.Width;
            int index = r.Next(w);
            int step = (int)(w / count);

            for (int i = 0; i < count; ++i)
            {
                index += step;
                index %= w - 1;
                Color c = image1.GetPixel(index, 0);
                palette.Add(c);
            }

            List<Color> result = new List<Color>();
            int interval = 4;
            for (int i = 0; i < count; ++i)
            {
                Color c;
                if (i % interval == 0)
                {
                    c = Blend(palette[i], colors[i], 0.7);
                }
                else if (i % interval == 1)
                {
                    c = Blend(palette[i], colors[i], 0.4);
                }
                else if (i % interval == 2)
                {
                    c = Blend(palette[i], colors[i], 0.2);
                }
                else
                {
                    c = Blend(palette[i], colors[i], 0.1);
                }
                result.Add(c);
            }

            return result;
        }

        /// <summary>
        /// Calculates the distance between two points.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The secord point.</param>
        /// <returns>Calculated distance.</returns>
        private static double Distance(PointF p1, PointF p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        /// <summary>
        /// Visits all points of contour without nuts.
        /// </summary>
        /// <param name="path">Contour without nuts.</param>
        /// <param name="path">Contour with nuts.</param>
        /// <param name="graphics">GDI+ device context wrapper.</param>
        /// <param name="handler">The delegate function.</param>
        private static void VisitPoints(
            GraphicsPath path,
            GraphicsPath path2,
            Graphics graphics,
            Action<RectangleF, Graphics, GraphicsPath> handler)
        {
            PointF[] points = path.PathPoints;
            PointF prev = new PointF(0, 0);
            float diameter = nutDiameter;
            for (int i = 0; i < path.PointCount; ++i)
            {
                PointF point = points[i];
                if (i % 5 == 0 && Distance(point, prev) > 12)
                {
                    RectangleF rectangle = new RectangleF(
                        point.X - diameter / 2,
                        point.Y - diameter / 2,
                        diameter,
                        diameter);
                    handler(rectangle, graphics, path2);
                }
                prev = point;
            }
        }

        /// <summary>
        /// Draws the nut.
        /// </summary>
        /// <param name="rectangle">Position and size of nut.</param>
        /// <param name="graphics">GDI+ device context wrapper.</param>
        /// <param name="path">Ignored.</param>
        private static void DrawAndFillEllipse(RectangleF rectangle, Graphics graphics, GraphicsPath path)
        {
            graphics.FillEllipse(Brushes.White, rectangle);
            graphics.DrawEllipse(Pens.LightGray, rectangle);
        }

        /// <summary>
        /// Adds the nut to path.
        /// </summary>
        /// <param name="rectangle">Position and size of nut.</param>
        /// <param name="graphics">GDI+ device context wrapper.</param>
        /// <param name="path">Contour of text.</param>
        private static void AddEllipseToPath(RectangleF rectangle, Graphics graphics, GraphicsPath path)
        {
            path.AddEllipse(rectangle);
        }

        /// <summary>
        /// Draws an inverted zommed by Y axis image with a transparency gradient.
        /// </summary>
        /// <param name="image1"></param>
        /// <param name="w">Width.</param>
        /// <param name="h">Height.</param>
        /// <param name="horizon">Position.</param>
        /// <returns>Painted bitmap.</returns>
        public static Bitmap DrawReflection(Bitmap image1, int w, int h)
        {
            Bitmap bitmap = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.Clear(Color.Black);

            float bw = image1.Width;
            float bh = image1.Height;

            graphics.DrawImage(image1, new RectangleF(
                w / 2 - bw / 2,
                horizon - bh,
                bw,
                bh));

            graphics.DrawImage(image1, new RectangleF(
                w / 2 - bw / 2,
                horizon + bh / 2,
                bw,
                -bh / 2));

            RectangleF rectangle = new RectangleF(
                w / 2 - bw / 2,
                horizon,
                bw,
                bh / 2);

            LinearGradientBrush brush = new LinearGradientBrush(
                rectangle,
                Color.FromArgb(150, 0, 0, 0),
                Color.FromArgb(255, 0, 0, 0),
                LinearGradientMode.Vertical);
            graphics.FillRectangle(brush, rectangle);

            return bitmap;
        }
    }
}
