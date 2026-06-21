using System;
using OpenCvSharp;
using MoonSharp.Interpreter;

namespace FlowEngine.Engine
{
    [MoonSharpUserData]
    public class MatWrapper : IDisposable
    {
        public Mat? Mat { get; private set; }

        public MatWrapper(Mat mat)
        {
            Mat = mat;
        }

        public static MatWrapper Create(Mat mat)
        {
            return new MatWrapper(mat);
        }

        public MatWrapper(int rows, int cols, int type)
        {
            Mat = new Mat(rows, cols, (MatType)type);
        }

        public MatWrapper(int rows, int cols, string typeStr)
        {
            Mat = new Mat(rows, cols, ParseType(typeStr));
        }

        private static MatType ParseType(string typeStr)
        {
            switch (typeStr.ToUpper())
            {
                case "8UC1": return MatType.CV_8UC1;
                case "8UC2": return MatType.CV_8UC2;
                case "8UC3": return MatType.CV_8UC3;
                case "8UC4": return MatType.CV_8UC4;
                case "16UC1": return MatType.CV_16UC1;
                case "16SC1": return MatType.CV_16SC1;
                case "32FC1": return MatType.CV_32FC1;
                case "32FC3": return MatType.CV_32FC3;
                case "64FC1": return MatType.CV_64FC1;
                default: return MatType.CV_8UC1;
            }
        }

        public int width => Mat?.Width ?? 0;
        public int height => Mat?.Height ?? 0;
        public int cols => Mat?.Cols ?? 0;
        public int rows => Mat?.Rows ?? 0;
        public int channels => Mat?.Channels() ?? 0;

        public MatWrapper? clone()
        {
            if (Mat == null) return null;
            return new MatWrapper(Mat.Clone());
        }

        public void release()
        {
            Mat?.Release();
        }

        public bool empty()
        {
            return Mat == null || Mat.Empty();
        }

        public void Dispose()
        {
            Mat?.Dispose();
        }
    }
}
