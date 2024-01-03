using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics;
using System.Numerics;
using MathNet.Numerics.Data.Text;
using System.Linq;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace FFTLibrary
{
    public class Data
    {
        public double[] Time { get; set; }
        public List<double[]> Real { get; set; }
        public List<double[]> Imaginary { get; set; }
        public List<double[]> Magnitude { get; set; }

        public Data()
        {
            Time = new double[0];
            Real = new List<double[]>();
            Imaginary = new List<double[]>();
            Magnitude = new List<double[]>();
        }
    }
    public class FFT
    {
        public static bool Transform(Stream inputFile, double minfreq,double maxfreq, double Amplitude, out double NaturalFreq,out double NaturalFreq_A, out double Magnitude_avg, bool IsOutPutFile,string outputPath,string dateTimeString, string filename)
        {
            NaturalFreq = 0;
            NaturalFreq_A = 0;
            Magnitude_avg = 0;
            StreamReader reader = new StreamReader(inputFile, Encoding.UTF8);
            int lineNum = 1;
            Data data = new Data();
            List<string[]> lines = new List<string[]>();

            while (!reader.EndOfStream)
            {
                // 讀取一行 CSV 資料
                string line = reader.ReadLine();
                if (lineNum >= 7)
                {
                    string[] values = line.Split(',');
                    lines.Add(values);
                }
                lineNum++;
            }
            int rowCount = lines.Count;
            int columnCount = lines[0].Length;
            double[] realtemp = new double[0];
            double[] result = new double[3];
            double[] imaginarytemp = Enumerable.Repeat(0.0, rowCount).ToArray();
            data.Time= Array.ConvertAll(lines.Select(arr => arr[1]).ToArray(), Double.Parse);
            double freqStep = 1.0/data.Time[rowCount-1];
            double[] Hz = Enumerable.Range(0, rowCount).Select(i => (double)i * freqStep).ToArray();
            for (int i=0;i< columnCount-2; i++)
            {
                realtemp = Array.ConvertAll(lines.Select(arr => arr[i+2]).ToArray(), Double.Parse);
                if (realtemp.Max() > Amplitude || realtemp.Min() < -1.0 * Amplitude)
                    return false;
                else
                {
                    Fourier.Forward(realtemp, imaginarytemp, FourierOptions.Matlab);
                    data.Real.Add(realtemp);
                    data.Imaginary.Add(imaginarytemp);
                    data.Magnitude.Add(realtemp.Zip(imaginarytemp, (x, y) => Math.Sqrt((x * x + y * y)) / rowCount * 2.0).ToArray());
                    //特殊寫法，將data1.Imaginary的第i個元素都替換成 imaginarytemp[index]的內容
                    //data1.Imaginary = data1.Imaginary.Select((arr, index) => { arr[i] = imaginarytemp[index]; return arr; }).ToList();
                    result = FindFreq(data.Magnitude[i], Hz, minfreq, maxfreq);
                    NaturalFreq_A = result[0];
                    NaturalFreq = result[1];
                    Magnitude_avg = result[2];
                    //if (NaturalFreq ==0)
                    //    return false;
                }
            }

            //將FFT資料寫成csv
            if(IsOutPutFile)
            {
                using (var writer = new StreamWriter(outputPath+"\\"+ filename + "FFToutput.csv"))
                {
                    foreach (var row in data.Magnitude)
                    {
                        for (int i = 0; i < rowCount / 2; i++)
                        {
                            writer.WriteLine(string.Join(",", Hz[i], row[i]));
                        }
                    }
                }
            }
    
            reader.Close();
            return true;
        }
        public static double[] FindFreq(double[] Magnitude, double[] Hz, double minfreq, double maxfreq)
        {
            // 找出指定頻段的索引
            int startIndex = Array.IndexOf(Hz, Hz.FirstOrDefault(f => f >= minfreq));
            int endIndex = Array.IndexOf(Hz, Hz.LastOrDefault(f => f <= maxfreq));
            double[] result = new double[3];
            // 找出指定頻段的最大值
            double maxMagnitude = Magnitude.Skip(startIndex).Take(endIndex - startIndex + 1).Max();
            // 找出指定頻段的最大值
            double Magnitude_avg = Magnitude.Skip(startIndex).Take(endIndex - startIndex + 1).Average();
            result[0] = maxMagnitude;
            // 找出最大值所對應的頻率
               double maxMagnitudeHz = Hz[Array.IndexOf(Magnitude, maxMagnitude)];
            result[1]= maxMagnitudeHz;
            result[2] = Magnitude_avg;
            //Console.WriteLine($"The maximum magnitude in the range of {minfreq}Hz to {maxfreq}Hz is {maxMagnitude} at {maxMagnitudeHz}Hz");
            //return maxMagnitudeHz;
            return result;

        }
    }

}
