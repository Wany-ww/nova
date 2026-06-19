using System;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
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
