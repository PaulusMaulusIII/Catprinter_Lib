﻿using System.Drawing;

namespace Catprinter.Utils
{
    public class ImageProcessing
    {
        public static Bitmap FloydSteinbergDither(Bitmap img)
        {
            int width = img.Width;
            int height = img.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color oldColor = img.GetPixel(x, y);
                    int oldGray = (oldColor.R + oldColor.G + oldColor.B) / 3;
                    int newGray = oldGray > 127 ? 255 : 0;
                    int error = oldGray - newGray;
                    img.SetPixel(x, y, Color.FromArgb(newGray, newGray, newGray));

                    AdjustPixel(img, x + 1, y, error * 7 / 16);
                    AdjustPixel(img, x - 1, y + 1, error * 3 / 16);
                    AdjustPixel(img, x, y + 1, error * 5 / 16);
                    AdjustPixel(img, x + 1, y + 1, error * 1 / 16);
                }
            }
            return img;
        }

        public static Bitmap AtkinsonDither(Bitmap img)
        {
            int width = img.Width;
            int height = img.Height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color oldColor = img.GetPixel(x, y);
                    int oldGray = (oldColor.R + oldColor.G + oldColor.B) / 3;
                    int newGray = oldGray > 127 ? 255 : 0;
                    int error = oldGray - newGray;
                    img.SetPixel(x, y, Color.FromArgb(newGray, newGray, newGray));

                    AdjustPixel(img, x + 1, y, error * 1 / 8);
                    AdjustPixel(img, x + 2, y, error * 1 / 8);
                    AdjustPixel(img, x - 1, y + 1, error * 1 / 8);
                    AdjustPixel(img, x, y + 1, error * 1 / 8);
                    AdjustPixel(img, x + 1, y + 1, error * 1 / 8);
                    AdjustPixel(img, x, y + 2, error * 1 / 8);
                }
            }
            return img;
        }

        public static Bitmap HalftoneDither(Bitmap img)
        {
            int side = 4;
            int jump = 4;
            int alpha = 3;
            int width = img.Width;
            int height = img.Height;

            int widthOutput = side * (int)Math.Ceiling((double)width / jump);
            int heightOutput = side * (int)Math.Ceiling((double)height / jump);
            Bitmap canvas = new Bitmap(widthOutput, heightOutput);

            for (int y = 0; y < height; y += jump)
            {
                for (int x = 0; x < width; x += jump)
                {
                    double intensity = 1 - SquareAvgValue(img, x, y, jump) / 255.0;
                    int radius = (int)(alpha * intensity * side / 2);
                    if (radius > 0)
                    {
                        using (Graphics g = Graphics.FromImage(canvas))
                        {
                            g.FillEllipse(Brushes.Black, x, y, radius * 2, radius * 2);
                        }
                    }
                }
            }
            return canvas;
        }

        private static void AdjustPixel(Bitmap img, int x, int y, int delta)
        {
            if (x < 0 || x >= img.Width || y < 0 || y >= img.Height)
                return;

            Color oldColor = img.GetPixel(x, y);
            int oldGray = (oldColor.R + oldColor.G + oldColor.B) / 3;
            int newGray = Math.Min(255, Math.Max(0, oldGray + delta));
            img.SetPixel(x, y, Color.FromArgb(newGray, newGray, newGray));
        }

        private static double SquareAvgValue(Bitmap img, int x, int y, int size)
        {
            int sum = 0;
            int count = 0;
            for (int i = y; i < y + size && i < img.Height; i++)
            {
                for (int j = x; j < x + size && j < img.Width; j++)
                {
                    Color color = img.GetPixel(j, i);
                    sum += (color.R + color.G + color.B) / 3;
                    count++;
                }
            }
            return count == 0 ? 0 : (double)sum / count;
        }

        private static Bitmap InvertImage(Bitmap img)
        {
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    Color color = img.GetPixel(x, y);
                    img.SetPixel(x, y, Color.FromArgb(255 - color.R, 255 - color.G, 255 - color.B));
                }
            }
            return img;
        }

        public static Bitmap ReadImg(string filename, int printWidth, string imgBinarizationAlgo, bool invert)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException($"The file {filename} does not exist.");
            }
            Bitmap img = new Bitmap(filename);
            int width = img.Width;
            int height = img.Height;
            double factor = (double)printWidth / width;
            Bitmap resized = new Bitmap(img, new Size(printWidth, (int)(height * factor)));
            for (int y = 0; y < resized.Height; y++)
            {
                for (int x = 0; x < resized.Width; x++)
                {
                    Color color = resized.GetPixel(x, y);
                    if (color.A == 0) 
                        resized.SetPixel(x, y, Color.White);
                }
            }

            switch (imgBinarizationAlgo.ToLower())
            {
                case "atkinson":
                    resized = AtkinsonDither(resized);
                    break;
                case "floyd-steinberg":
                    resized = FloydSteinbergDither(resized);
                    break;
                case "halftone":
                    resized = HalftoneDither(resized);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown image binarization algorithm: {imgBinarizationAlgo}");
            }

            if (invert)
            {
                resized = InvertImage(resized);
            }
            return resized;
        }
    }
}
