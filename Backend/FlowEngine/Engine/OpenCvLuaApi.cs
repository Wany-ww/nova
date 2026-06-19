using System;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using MoonSharp.Interpreter;

namespace FlowEngine.Engine
{
    public static class OpenCvLuaApi
    {
        private static readonly ConcurrentDictionary<string, ImageWindow> _imageWindows = 
            new ConcurrentDictionary<string, ImageWindow>();

        static OpenCvLuaApi()
        {
            UserData.RegisterType<MatWrapper>();
        }

        private static double GetNum(DynValue val, double @default = 0.0)
        {
            return val.CastToNumber() ?? @default;
        }

        public static void Register(Script script)
        {
            var cv = new Table(script);

            // cv.Mat creator
            cv["Mat"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count == 0)
                    return new MatWrapper(new Mat());
                
                if (args.Count == 1)
                {
                    if (args[0].Type == DataType.String)
                    {
                        return new MatWrapper(Cv2.ImRead(args[0].String));
                    }
                    else if (args[0].Type == DataType.UserData)
                    {
                        var other = args[0].UserData.Object as MatWrapper;
                        if (other != null && other.Mat != null)
                            return new MatWrapper(other.Mat.Clone());
                    }
                }
                
                if (args.Count >= 3)
                {
                    int rows = (int)GetNum(args[0]);
                    int cols = (int)GetNum(args[1]);
                    if (args[2].Type == DataType.String)
                    {
                        return new MatWrapper(rows, cols, args[2].String);
                    }
                    else
                    {
                        int typeVal = (int)GetNum(args[2]);
                        return new MatWrapper(rows, cols, typeVal);
                    }
                }
                throw new ScriptRuntimeException("Invalid arguments for cv.Mat");
            });

            // ImShow & WaitKey
            cv["imshow"] = (Action<string, MatWrapper>)((winname, mat) =>
            {
                if (mat?.Mat == null) throw new ScriptRuntimeException("Mat is null");
                
                var bitmapSource = ConvertMatToBitmapSource(mat.Mat);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWin = System.Windows.Application.Current.MainWindow as MainWindow;
                    string? dockedDir = mainWin?.GetDockedDirection(winname);
                    if (mainWin != null && dockedDir != null)
                    {
                        mainWin.DockImageWindow(winname, bitmapSource, dockedDir);
                    }
                    else if (_imageWindows.TryGetValue(winname, out var window) && window.IsLoaded)
                    {
                        window.UpdateImage(winname, bitmapSource);
                    }
                    else
                    {
                        ShowFloatingWindow(winname, bitmapSource);
                    }
                });
            });

            cv["waitKey"] = (Func<int, int>)(delay =>
            {
                if (delay > 0)
                {
                    System.Threading.Thread.Sleep(delay);
                }
                else
                {
                    System.Threading.Thread.Sleep(1);
                }
                return -1;
            });

            // ImRead & ImWrite
            cv["imread"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count == 0) throw new ScriptRuntimeException("imread requires a filename");
                string filename = args[0].CastToString();
                int flags = args.Count > 1 ? (int)GetNum(args[1], (double)ImreadModes.Color) : (int)ImreadModes.Color;
                return new MatWrapper(Cv2.ImRead(filename, (ImreadModes)flags));
            });

            cv["imwrite"] = (Func<string, MatWrapper, bool>)((filename, mat) =>
            {
                if (mat?.Mat == null) throw new ScriptRuntimeException("Mat is null");
                return Cv2.ImWrite(filename, mat.Mat);
            });

            // ImEncode & ImDecode
            cv["imencode"] = (Func<string, MatWrapper, Table>)((ext, mat) =>
            {
                if (mat?.Mat == null) throw new ScriptRuntimeException("Mat is null");
                byte[]? buffer = null;
                if (Cv2.ImEncode(ext, mat.Mat, out buffer))
                {
                    var table = new Table(script);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        table[i + 1] = (int)buffer[i];
                    }
                    return table;
                }
                throw new ScriptRuntimeException("Failed to encode Mat to " + ext);
            });

            cv["imdecode"] = (Func<Table, MatWrapper>)(bytes =>
            {
                if (bytes == null) throw new ScriptRuntimeException("Bytes table is null");
                var byteList = new System.Collections.Generic.List<byte>();
                foreach (var pair in bytes.Pairs)
                {
                    if (pair.Value.Type == DataType.Number)
                    {
                        byteList.Add((byte)pair.Value.Number);
                    }
                }
                byte[] data = byteList.ToArray();
                if (data.Length == 0) throw new ScriptRuntimeException("Empty bytes array for imdecode");
                Mat decoded = Cv2.ImDecode(data, ImreadModes.Color);
                if (decoded == null || decoded.Empty())
                {
                    throw new ScriptRuntimeException("Failed to decode Mat from bytes");
                }
                return new MatWrapper(decoded);
            });


            // CvtColor & Threshold & Canny & Resize
            cv["cvtColor"] = (Func<MatWrapper, int, MatWrapper>)((src, code) =>
            {
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                var dst = new Mat();
                Cv2.CvtColor(src.Mat, dst, (ColorConversionCodes)code);
                return new MatWrapper(dst);
            });

            cv["threshold"] = (Func<CallbackArguments, Table>)(args =>
            {
                if (args.Count < 4) throw new ScriptRuntimeException("threshold requires src, thresh, maxval, type");
                var src = args[0].UserData.Object as MatWrapper;
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                double thresh = GetNum(args[1]);
                double maxval = GetNum(args[2]);
                int type = (int)GetNum(args[3]);
                var dst = new Mat();
                double retval = Cv2.Threshold(src.Mat, dst, thresh, maxval, (ThresholdTypes)type);
                
                var ret = new Table(script);
                ret[1] = retval;
                ret[2] = new MatWrapper(dst);
                return ret;
            });

            cv["Canny"] = (Func<MatWrapper, double, double, MatWrapper>)((src, t1, t2) =>
            {
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                var dst = new Mat();
                Cv2.Canny(src.Mat, dst, t1, t2);
                return new MatWrapper(dst);
            });

            cv["resize"] = (Func<MatWrapper, int, int, MatWrapper>)((src, w, h) =>
            {
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                var dst = new Mat();
                Cv2.Resize(src.Mat, dst, new Size(w, h));
                return new MatWrapper(dst);
            });

            // Drawing Functions
            cv["rectangle"] = (Action<CallbackArguments>)(args =>
            {
                if (args.Count < 5) throw new ScriptRuntimeException("rectangle requires img, x1, y1, x2, y2");
                var img = args[0].UserData.Object as MatWrapper;
                if (img?.Mat == null) throw new ScriptRuntimeException("Mat is null");
                int x1 = (int)GetNum(args[1]);
                int y1 = (int)GetNum(args[2]);
                int x2 = (int)GetNum(args[3]);
                int y2 = (int)GetNum(args[4]);
                
                Scalar scalar = Scalar.White;
                int thickness = 1;
                
                if (args.Count >= 8)
                {
                    double r = GetNum(args[5]);
                    double g = GetNum(args[6]);
                    double b = GetNum(args[7]);
                    scalar = new Scalar(b, g, r);
                    if (args.Count >= 9) thickness = (int)GetNum(args[8]);
                }
                else if (args.Count >= 6 && args[5].Type == DataType.Table)
                {
                    scalar = TableToScalar(args[5].Table);
                    if (args.Count >= 7) thickness = (int)GetNum(args[6]);
                }
                
                Cv2.Rectangle(img.Mat, new Point(x1, y1), new Point(x2, y2), scalar, thickness);
            });

            cv["circle"] = (Action<CallbackArguments>)(args =>
            {
                if (args.Count < 4) throw new ScriptRuntimeException("circle requires img, cx, cy, radius");
                var img = args[0].UserData.Object as MatWrapper;
                if (img?.Mat == null) throw new ScriptRuntimeException("Mat is null");
                int cx = (int)GetNum(args[1]);
                int cy = (int)GetNum(args[2]);
                int radius = (int)GetNum(args[3]);
                
                Scalar scalar = Scalar.White;
                int thickness = 1;
                
                if (args.Count >= 7)
                {
                    double r = GetNum(args[4]);
                    double g = GetNum(args[5]);
                    double b = GetNum(args[6]);
                    scalar = new Scalar(b, g, r);
                    if (args.Count >= 8) thickness = (int)GetNum(args[7]);
                }
                else if (args.Count >= 5 && args[4].Type == DataType.Table)
                {
                    scalar = TableToScalar(args[4].Table);
                    if (args.Count >= 6) thickness = (int)GetNum(args[5]);
                }
                
                Cv2.Circle(img.Mat, new Point(cx, cy), radius, scalar, thickness);
            });

            cv["line"] = (Action<CallbackArguments>)(args =>
            {
                if (args.Count < 5) throw new ScriptRuntimeException("line requires img, x1, y1, x2, y2");
                var img = args[0].UserData.Object as MatWrapper;
                if (img?.Mat == null) throw new ScriptRuntimeException("Mat is null");
                int x1 = (int)GetNum(args[1]);
                int y1 = (int)GetNum(args[2]);
                int x2 = (int)GetNum(args[3]);
                int y2 = (int)GetNum(args[4]);
                
                Scalar scalar = Scalar.White;
                int thickness = 1;
                
                if (args.Count >= 8)
                {
                    double r = GetNum(args[5]);
                    double g = GetNum(args[6]);
                    double b = GetNum(args[7]);
                    scalar = new Scalar(b, g, r);
                    if (args.Count >= 9) thickness = (int)GetNum(args[8]);
                }
                else if (args.Count >= 6 && args[5].Type == DataType.Table)
                {
                    scalar = TableToScalar(args[5].Table);
                    if (args.Count >= 7) thickness = (int)GetNum(args[6]);
                }
                
                Cv2.Line(img.Mat, new Point(x1, y1), new Point(x2, y2), scalar, thickness);
            });

            cv["putText"] = (Action<CallbackArguments>)(args =>
            {
                if (args.Count < 4) throw new ScriptRuntimeException("putText requires img, text, x, y");
                var img = args[0].UserData.Object as MatWrapper;
                if (img?.Mat == null) throw new ScriptRuntimeException("Mat is null");
                string text = args[1].CastToString();
                int x = (int)GetNum(args[2]);
                int y = (int)GetNum(args[3]);
                
                double fontScale = 1.0;
                Scalar scalar = Scalar.White;
                int thickness = 1;
                HersheyFonts fontFace = HersheyFonts.HersheySimplex;

                if (args.Count >= 5) fontScale = GetNum(args[4], 1.0);
                if (args.Count >= 8)
                {
                    double r = GetNum(args[5]);
                    double g = GetNum(args[6]);
                    double b = GetNum(args[7]);
                    scalar = new Scalar(b, g, r);
                    if (args.Count >= 9) thickness = (int)GetNum(args[8]);
                }
                else if (args.Count >= 6 && args[5].Type == DataType.Table)
                {
                    scalar = TableToScalar(args[5].Table);
                    if (args.Count >= 7) thickness = (int)GetNum(args[6]);
                }

                Cv2.PutText(img.Mat, text, new Point(x, y), fontFace, fontScale, scalar, thickness);
            });

            // OpenCV Type Constants
            int val8UC1 = (int)MatType.CV_8UC1;
            int val8UC2 = (int)MatType.CV_8UC2;
            int val8UC3 = (int)MatType.CV_8UC3;
            int val8UC4 = (int)MatType.CV_8UC4;
            int val16UC1 = (int)MatType.CV_16UC1;
            int val32FC1 = (int)MatType.CV_32FC1;
            int val32FC3 = (int)MatType.CV_32FC3;

            cv["CV_8UC1"] = val8UC1;
            cv["CV_8UC2"] = val8UC2;
            cv["CV_8UC3"] = val8UC3;
            cv["CV_8UC4"] = val8UC4;
            cv["CV_16UC1"] = val16UC1;
            cv["CV_32FC1"] = val32FC1;
            cv["CV_32FC3"] = val32FC3;

            cv["8UC1"] = val8UC1;
            cv["8UC2"] = val8UC2;
            cv["8UC3"] = val8UC3;
            cv["8UC4"] = val8UC4;
            cv["_8UC1"] = val8UC1;
            cv["_8UC2"] = val8UC2;
            cv["_8UC3"] = val8UC3;
            cv["_8UC4"] = val8UC4;

            // Other Constants
            cv["COLOR_BGR2GRAY"] = (int)ColorConversionCodes.BGR2GRAY;
            cv["COLOR_GRAY2BGR"] = (int)ColorConversionCodes.GRAY2BGR;
            cv["COLOR_BGR2HSV"] = (int)ColorConversionCodes.BGR2HSV;
            cv["COLOR_HSV2BGR"] = (int)ColorConversionCodes.HSV2BGR;
            cv["THRESH_BINARY"] = (int)ThresholdTypes.Binary;
            cv["THRESH_BINARY_INV"] = (int)ThresholdTypes.BinaryInv;
            cv["THRESH_OTSU"] = (int)ThresholdTypes.Otsu;

            // Structuring Element Constants
            cv["MORPH_RECT"] = (int)MorphShapes.Rect;
            cv["MORPH_CROSS"] = (int)MorphShapes.Cross;
            cv["MORPH_ELLIPSE"] = (int)MorphShapes.Ellipse;

            // Retrieval Modes for findContours
            cv["RETR_EXTERNAL"] = (int)RetrievalModes.External;
            cv["RETR_LIST"] = (int)RetrievalModes.List;
            cv["RETR_CCOMP"] = (int)RetrievalModes.CComp;
            cv["RETR_TREE"] = (int)RetrievalModes.Tree;

            // Contour Approximation Modes for findContours
            cv["CHAIN_APPROX_NONE"] = (int)ContourApproximationModes.ApproxNone;
            cv["CHAIN_APPROX_SIMPLE"] = (int)ContourApproximationModes.ApproxSimple;

            // Template Match Modes for matchTemplate
            cv["TM_SQDIFF"] = (int)TemplateMatchModes.SqDiff;
            cv["TM_SQDIFF_NORMED"] = (int)TemplateMatchModes.SqDiffNormed;
            cv["TM_CCORR"] = (int)TemplateMatchModes.CCorr;
            cv["TM_CCORR_NORMED"] = (int)TemplateMatchModes.CCorrNormed;
            cv["TM_CCOEFF"] = (int)TemplateMatchModes.CCoeff;
            cv["TM_CCOEFF_NORMED"] = (int)TemplateMatchModes.CCoeffNormed;

            // Blur & Smooth APIs
            cv["GaussianBlur"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 4) throw new ScriptRuntimeException("GaussianBlur requires src, ksize_w, ksize_h, sigmaX");
                var src = args[0].UserData.Object as MatWrapper;
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                int kw = (int)GetNum(args[1]);
                int kh = (int)GetNum(args[2]);
                double sigmaX = GetNum(args[3]);
                double sigmaY = args.Count > 4 ? GetNum(args[4]) : 0.0;
                int borderType = args.Count > 5 ? (int)GetNum(args[5], (double)BorderTypes.Reflect101) : (int)BorderTypes.Reflect101;
                
                var dst = new Mat();
                Cv2.GaussianBlur(src.Mat, dst, new Size(kw, kh), sigmaX, sigmaY, (BorderTypes)borderType);
                return new MatWrapper(dst);
            });

            cv["medianBlur"] = (Func<MatWrapper, int, MatWrapper>)((src, ksize) =>
            {
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                var dst = new Mat();
                Cv2.MedianBlur(src.Mat, dst, ksize);
                return new MatWrapper(dst);
            });

            // Morphological Operations
            cv["getStructuringElement"] = (Func<int, int, int, MatWrapper>)((shape, kw, kh) =>
            {
                return new MatWrapper(Cv2.GetStructuringElement((MorphShapes)shape, new Size(kw, kh)));
            });

            cv["erode"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 2) throw new ScriptRuntimeException("erode requires src, element");
                var src = args[0].UserData.Object as MatWrapper;
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                var elem = args[1].UserData.Object as MatWrapper;
                if (elem?.Mat == null) throw new ScriptRuntimeException("Structuring element is null");
                Mat kernel = elem.Mat;
                
                int iterations = args.Count > 2 ? (int)GetNum(args[2], 1.0) : 1;
                var dst = new Mat();
                Cv2.Erode(src.Mat, dst, kernel, null, iterations);
                return new MatWrapper(dst);
            });

            cv["dilate"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 2) throw new ScriptRuntimeException("dilate requires src, element");
                var src = args[0].UserData.Object as MatWrapper;
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                var elem = args[1].UserData.Object as MatWrapper;
                if (elem?.Mat == null) throw new ScriptRuntimeException("Structuring element is null");
                Mat kernel = elem.Mat;
                
                int iterations = args.Count > 2 ? (int)GetNum(args[2], 1.0) : 1;
                var dst = new Mat();
                Cv2.Dilate(src.Mat, dst, kernel, null, iterations);
                return new MatWrapper(dst);
            });

            // Geometric Transformations (Rotation & Affine Warp)
            cv["getRotationMatrix2D"] = (Func<double, double, double, double, MatWrapper>)((cx, cy, angle, scale) =>
            {
                return new MatWrapper(Cv2.GetRotationMatrix2D(new Point2f((float)cx, (float)cy), angle, scale));
            });

            cv["warpAffine"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 4) throw new ScriptRuntimeException("warpAffine requires src, M, dsize_w, dsize_h");
                var src = args[0].UserData.Object as MatWrapper;
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                var m = args[1].UserData.Object as MatWrapper;
                if (m?.Mat == null) throw new ScriptRuntimeException("Matrix M is null");
                int dw = (int)GetNum(args[2]);
                int dh = (int)GetNum(args[3]);
                
                int flags = args.Count > 4 ? (int)GetNum(args[4], (double)InterpolationFlags.Linear) : (int)InterpolationFlags.Linear;
                int borderMode = args.Count > 5 ? (int)GetNum(args[5], (double)BorderTypes.Constant) : (int)BorderTypes.Constant;
                
                var dst = new Mat();
                Cv2.WarpAffine(src.Mat, dst, m.Mat, new Size(dw, dh), (InterpolationFlags)flags, (BorderTypes)borderMode);
                return new MatWrapper(dst);
            });

            // Logical/Bitwise Operations
            cv["bitwise_and"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 2) throw new ScriptRuntimeException("bitwise_and requires src1, src2");
                var src1 = args[0].UserData.Object as MatWrapper;
                if (src1?.Mat == null) throw new ScriptRuntimeException("Source1 Mat is null");
                var src2 = args[1].UserData.Object as MatWrapper;
                if (src2?.Mat == null) throw new ScriptRuntimeException("Source2 Mat is null");
                
                var maskWrapper = args.Count > 2 ? args[2].UserData?.Object as MatWrapper : null;
                var dst = new Mat();
                if (maskWrapper != null)
                {
                    if (maskWrapper.Mat == null) throw new ScriptRuntimeException("Mask Mat is null");
                    Cv2.BitwiseAnd(src1.Mat, src2.Mat, dst, maskWrapper.Mat);
                }
                else
                {
                    Cv2.BitwiseAnd(src1.Mat, src2.Mat, dst);
                }
                return new MatWrapper(dst);
            });

            cv["bitwise_or"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 2) throw new ScriptRuntimeException("bitwise_or requires src1, src2");
                var src1 = args[0].UserData.Object as MatWrapper;
                if (src1?.Mat == null) throw new ScriptRuntimeException("Source1 Mat is null");
                var src2 = args[1].UserData.Object as MatWrapper;
                if (src2?.Mat == null) throw new ScriptRuntimeException("Source2 Mat is null");
                
                var maskWrapper = args.Count > 2 ? args[2].UserData?.Object as MatWrapper : null;
                var dst = new Mat();
                if (maskWrapper != null)
                {
                    if (maskWrapper.Mat == null) throw new ScriptRuntimeException("Mask Mat is null");
                    Cv2.BitwiseOr(src1.Mat, src2.Mat, dst, maskWrapper.Mat);
                }
                else
                {
                    Cv2.BitwiseOr(src1.Mat, src2.Mat, dst);
                }
                return new MatWrapper(dst);
            });

            cv["bitwise_xor"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 2) throw new ScriptRuntimeException("bitwise_xor requires src1, src2");
                var src1 = args[0].UserData.Object as MatWrapper;
                if (src1?.Mat == null) throw new ScriptRuntimeException("Source1 Mat is null");
                var src2 = args[1].UserData.Object as MatWrapper;
                if (src2?.Mat == null) throw new ScriptRuntimeException("Source2 Mat is null");
                
                var maskWrapper = args.Count > 2 ? args[2].UserData?.Object as MatWrapper : null;
                var dst = new Mat();
                if (maskWrapper != null)
                {
                    if (maskWrapper.Mat == null) throw new ScriptRuntimeException("Mask Mat is null");
                    Cv2.BitwiseXor(src1.Mat, src2.Mat, dst, maskWrapper.Mat);
                }
                else
                {
                    Cv2.BitwiseXor(src1.Mat, src2.Mat, dst);
                }
                return new MatWrapper(dst);
            });

            cv["bitwise_not"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 1) throw new ScriptRuntimeException("bitwise_not requires src");
                var src = args[0].UserData.Object as MatWrapper;
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                
                var maskWrapper = args.Count > 1 ? args[1].UserData?.Object as MatWrapper : null;
                var dst = new Mat();
                if (maskWrapper != null)
                {
                    if (maskWrapper.Mat == null) throw new ScriptRuntimeException("Mask Mat is null");
                    Cv2.BitwiseNot(src.Mat, dst, maskWrapper.Mat);
                }
                else
                {
                    Cv2.BitwiseNot(src.Mat, dst);
                }
                return new MatWrapper(dst);
            });

            // Channel Processing
            cv["split"] = (Func<MatWrapper, Table>)(src =>
            {
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                var mats = Cv2.Split(src.Mat);
                var table = new Table(script);
                for (int i = 0; i < mats.Length; i++)
                {
                    table[i + 1] = new MatWrapper(mats[i]);
                }
                return table;
            });

            cv["merge"] = (Func<Table, MatWrapper>)(table =>
            {
                if (table == null) throw new ScriptRuntimeException("Table is null");
                var list = new List<Mat>();
                foreach (var pair in table.Pairs)
                {
                    var wrapper = pair.Value.UserData?.Object as MatWrapper;
                    if (wrapper?.Mat != null)
                    {
                        list.Add(wrapper.Mat);
                    }
                }
                var dst = new Mat();
                Cv2.Merge(list.ToArray(), dst);
                return new MatWrapper(dst);
            });

            // Template Matching
            cv["matchTemplate"] = (Func<CallbackArguments, MatWrapper>)(args =>
            {
                if (args.Count < 3) throw new ScriptRuntimeException("matchTemplate requires image, templ, method");
                var image = args[0].UserData.Object as MatWrapper;
                if (image?.Mat == null) throw new ScriptRuntimeException("Image Mat is null");
                var templ = args[1].UserData.Object as MatWrapper;
                if (templ?.Mat == null) throw new ScriptRuntimeException("Template Mat is null");
                int method = (int)GetNum(args[2]);
                
                var dst = new Mat();
                Cv2.MatchTemplate(image.Mat, templ.Mat, dst, (TemplateMatchModes)method);
                return new MatWrapper(dst);
            });

            cv["minMaxLoc"] = (Func<MatWrapper, Table>)(src =>
            {
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                double minVal, maxVal;
                Point minLoc, maxLoc;
                Cv2.MinMaxLoc(src.Mat, out minVal, out maxVal, out minLoc, out maxLoc);
                
                var ret = new Table(script);
                ret["minVal"] = minVal;
                ret["maxVal"] = maxVal;
                
                var minLocTable = new Table(script);
                minLocTable["x"] = minLoc.X;
                minLocTable["y"] = minLoc.Y;
                ret["minLoc"] = minLocTable;

                var maxLocTable = new Table(script);
                maxLocTable["x"] = maxLoc.X;
                maxLocTable["y"] = maxLoc.Y;
                ret["maxLoc"] = maxLocTable;

                return ret;
            });

            // Shape Analysis & Contour APIs
            cv["findContours"] = (Func<CallbackArguments, Table>)(args =>
            {
                if (args.Count < 3) throw new ScriptRuntimeException("findContours requires src, mode, method");
                var src = args[0].UserData.Object as MatWrapper;
                if (src?.Mat == null) throw new ScriptRuntimeException("Source Mat is null");
                int mode = (int)GetNum(args[1]);
                int method = (int)GetNum(args[2]);

                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(src.Mat, out contours, out hierarchy, (RetrievalModes)mode, (ContourApproximationModes)method);

                var outerTable = new Table(script);
                for (int i = 0; i < contours.Length; i++)
                {
                    var contourTable = new Table(script);
                    for (int j = 0; j < contours[i].Length; j++)
                    {
                        var ptTable = new Table(script);
                        ptTable["x"] = contours[i][j].X;
                        ptTable["y"] = contours[i][j].Y;
                        contourTable[j + 1] = ptTable;
                    }
                    outerTable[i + 1] = contourTable;
                }
                return outerTable;
            });

            cv["drawContours"] = (Action<CallbackArguments>)(args =>
            {
                if (args.Count < 5) throw new ScriptRuntimeException("drawContours requires img, contours, contourIdx, color, thickness");
                var img = args[0].UserData.Object as MatWrapper;
                if (img?.Mat == null) throw new ScriptRuntimeException("Mat is null");
                var contoursTable = args[1].Table;
                if (contoursTable == null) throw new ScriptRuntimeException("Contours table is null");
                int contourIdx = (int)GetNum(args[2]);
                
                Scalar scalar = Scalar.White;
                if (args[3].Type == DataType.Table)
                {
                    scalar = TableToScalar(args[3].Table);
                }
                int thickness = (int)GetNum(args[4]);

                var contoursList = new List<Point[]>();
                foreach (var pair in contoursTable.Pairs)
                {
                    var contourTable = pair.Value.Table;
                    if (contourTable != null)
                    {
                        var pts = new List<Point>();
                        foreach (var ptPair in contourTable.Pairs)
                        {
                            var ptTable = ptPair.Value.Table;
                            if (ptTable != null)
                            {
                                int px = (int)GetNum(ptTable.Get("x"));
                                int py = (int)GetNum(ptTable.Get("y"));
                                pts.Add(new Point(px, py));
                            }
                        }
                        contoursList.Add(pts.ToArray());
                    }
                }

                Cv2.DrawContours(img.Mat, contoursList.ToArray(), contourIdx, scalar, thickness);
            });

            cv["boundingRect"] = (Func<Table, Table>)(contourTable =>
            {
                if (contourTable == null) throw new ScriptRuntimeException("Contour table is null");
                var pts = new List<Point>();
                foreach (var pair in contourTable.Pairs)
                {
                    var ptTable = pair.Value.Table;
                    if (ptTable != null)
                    {
                        int px = (int)GetNum(ptTable.Get("x"));
                        int py = (int)GetNum(ptTable.Get("y"));
                        pts.Add(new Point(px, py));
                    }
                }
                var rect = Cv2.BoundingRect(pts);
                var ret = new Table(script);
                ret["x"] = rect.X;
                ret["y"] = rect.Y;
                ret["width"] = rect.Width;
                ret["height"] = rect.Height;
                return ret;
            });

            script.Globals["cv"] = cv;
        }

        private static Scalar TableToScalar(Table t)
        {
            double r = GetNum(t.Get(1));
            double g = GetNum(t.Get(2));
            double b = GetNum(t.Get(3));
            double a = t.Get(4).Type != DataType.Nil ? GetNum(t.Get(4), 255.0) : 255.0;
            return new Scalar(b, g, r, a);
        }

        private static BitmapSource ConvertMatToBitmapSource(Mat mat)
        {
            byte[]? buffer = null;
            if (Cv2.ImEncode(".bmp", mat, out buffer))
            {
                using (var ms = new MemoryStream(buffer))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Allow crossing thread boundaries
                    return bitmap;
                }
            }
            throw new Exception("Failed to encode Mat to BMP");
        }

        public static void CloseAllImageWindows()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var windows = _imageWindows.Values.ToList();
                foreach (var win in windows)
                {
                    try
                    {
                        win.Close();
                    }
                    catch
                    {
                        // Suppress: throw/logCallback/MessageBox not needed when closing all windows during cleanup/shutdown
                    }
                }
                _imageWindows.Clear();
            });
        }

        public static System.Collections.Generic.List<ImageWindow> GetActiveImageWindows()
        {
            return _imageWindows.Values.Distinct().ToList();
        }

        public static void RegisterImageWindow(string winname, ImageWindow window)
        {
            _imageWindows[winname] = window;
        }

        public static void UnregisterImageWindow(string winname)
        {
            _imageWindows.TryRemove(winname, out _);
        }

        public static void ShowFloatingWindow(string winname, BitmapSource bitmapSource)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var newWindow = new ImageWindow();
                newWindow.Title = winname;
                newWindow.TitleTextBlock.Text = winname;
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    newWindow.Owner = System.Windows.Application.Current.MainWindow;
                }
                
                double imgW = bitmapSource.PixelWidth;
                double imgH = bitmapSource.PixelHeight;
                
                double maxW = System.Windows.SystemParameters.PrimaryScreenWidth * 0.8;
                double maxH = System.Windows.SystemParameters.PrimaryScreenHeight * 0.8;
                
                double winW = imgW + 20;
                double winH = imgH + 32 + 20;
                
                if (winW > maxW || winH > maxH)
                {
                    double ratio = Math.Min(maxW / winW, maxH / winH);
                    winW *= ratio;
                    winH *= ratio;
                }
                
                newWindow.Width = Math.Max(winW, 300);
                newWindow.Height = Math.Max(winH, 200);
                
                // Add the initial image tab
                newWindow.AddImageTab(winname, bitmapSource);
                

                _imageWindows[winname] = newWindow;
                newWindow.Show();
            });
        }

        public static void RemoveImage(string title)
        {
            _imageWindows.TryRemove(title, out _);
        }
    }
}
