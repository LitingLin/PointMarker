using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PointMarker
{
    static class PointFileOperation
    {
        private static string versionString = "version:";
        private static string numberOfPointsString = "n_points:";
        private static string beginSeparatorString = "{";
        private static string endSeparatorString = "}";
        
        public static async Task<IList<Point>> Read(Uri filePath)
        {
            string fileString;
            using (StreamReader reader = new StreamReader(filePath.AbsolutePath))
            {
                fileString = await reader.ReadToEndAsync();
            }

            string[] words = fileString.Split(null as string[], StringSplitOptions.RemoveEmptyEntries);

            int currentWordsIndex = 0;
            if (words[currentWordsIndex] != versionString)
                throw new FileFormatException(filePath);

            currentWordsIndex++;

            if (int.Parse(words[currentWordsIndex]) != 1)
                throw new FileFormatException(filePath);

            currentWordsIndex++;

            if (words[currentWordsIndex]!=numberOfPointsString)
                throw new FileFormatException(filePath);

            currentWordsIndex++;

            int numberOfPoints = int.Parse(words[currentWordsIndex]);
            IList<Point> points = new List<Point>(numberOfPoints);

            currentWordsIndex++;

            if (words[currentWordsIndex]!=beginSeparatorString)
                throw new FileFormatException(filePath);

            currentWordsIndex++;

            for (int i = 0; i != numberOfPoints; i++)
            {
                Point point = new Point(double.Parse(words[currentWordsIndex]), double.Parse(words[currentWordsIndex+1]));
                points.Add(point);
                currentWordsIndex += 2;
            }
            
            if (words[currentWordsIndex]!=endSeparatorString)
                throw new FileFormatException(filePath);

            return points;
        }

        public static async Task Write(IList<Point> points, Uri filePath)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("{0} 1\n", versionString)
                .AppendFormat("{0} {1}\n", numberOfPointsString, points.Count)
                .AppendLine(beginSeparatorString);
            foreach (var point in points)
            {
                stringBuilder.AppendFormat("{0} {1}\n", point.X, point.Y);
            }
            stringBuilder.AppendLine(endSeparatorString);

            using (StreamWriter writer = new StreamWriter(filePath.AbsolutePath))
            {
                await writer.WriteAsync(stringBuilder.ToString());
            }
        }
    }
}
