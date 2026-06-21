using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using MoonSharp.Interpreter;

namespace FlowEngine.Engine
{
    internal class CameraViewer
    {
        private ScrollViewer? _imageViewer;
        private Image? _imageControl;

        public void Show(string title, int width, int height)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _imageControl = new Image
                {
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    Width = width,
                    Height = height
                };

                _imageViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = _imageControl
                };

                GuiManager.ShowFloatingGuiDialog(title, _imageViewer);
            });
        }

        public void Update(Mat rgbMat)
        {
            if (_imageControl == null) return;
            var bitmap = OpenCvLuaApi.ConvertMatToBitmapSource(rgbMat);
            bitmap.Freeze();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_imageControl != null)
                {
                    _imageControl.Source = bitmap;
                }
            }));
        }

        public void Close(string title)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                GuiManager.ShowDialog(title, false);
                _imageViewer = null;
                _imageControl = null;
            });
        }
    }

    [MoonSharpUserData]
    public class LuaCamera : IDisposable
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        static LuaCamera()
        {
            try
            {
                string path = @"c:\Users\eunha\Documents\Developments\nova2\Reference";
                if (!Directory.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reference");
                }
                SetDllDirectory(path);
            }
            catch (Exception ex)
            {
                throw new TypeInitializationException(typeof(LuaCamera).FullName, ex);
            }
        }

        #region Native DLL Imports

        private static class DcLib
        {
            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_Init();

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_End();

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_GetMaxCards();

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_Open(int card_num, int channel, int Height, int Width, int Frames, int BitsPerClk);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_Close(int card_num, int channel);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_Start(int card_num, int channel);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_Stop(int card_num, int channel);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_GetFrame(int card_num, int channel, int blocked, ref IntPtr buf, ref ushort length, ref uint eof_length);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_CancelGetFrame(int card_num, int channel);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_SetMipiCsiTestCamEn(int card_num, int channel, int enable);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_SetMipiInfo(int card_num, int channel, int VirtualChannel, int DataType);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern float DC_SetHwI2cFreq(int card_num, float freq_khz);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_HwI2cWrite(int card_num, int length, byte start, byte last, byte[] buf);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_GetPmuHandle(int card_num, out IntPtr pHandle);

            [DllImport("dclib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DC_ReleasePmuHandle(int card_num, IntPtr hHandle);
        }

        private static class DgLib
        {
            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_Init();

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_End();

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_Open(int card_num, int channel, int Height, int Width, int Frames, int BitsPerClk);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_Close(int card_num, int channel);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_Start(int card_num, int channel);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_Stop(int card_num, int channel);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_GetFrame(int card_num, int channel, int blocked, ref IntPtr buf, ref ushort length, ref uint eof_length);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_CancelGetFrame(int card_num, int channel);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetTestCamEn(int card_num, int channel, int enable);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetMipiTestCamEn(int card_num, int channel, int enable);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetMipiDefault(int card_num, int channel, int val);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetIsmIo(int card_num, int ch, int io, int val);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetIsmDefault(int card_num);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetIsmIoVolt(int card_num, float volt);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetIsmFreq(int card_num, int ch, float freq);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetIsmReset(int card_num, int rst);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_SetI2cFreq(int card_num, float freq);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_I2cWrite(int card_num, int length, byte start, byte last, byte[] buf);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_GetPmuHandle(int card_num, out IntPtr pHandle);

            [DllImport("dglib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int DG_ReleasePmuHandle(int card_num, IntPtr hHandle);
        }

        private static class PmuLib
        {
            [DllImport("pmulib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int PMU_Init(IntPtr hHandle);

            [DllImport("pmulib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int PMU_End(IntPtr hHandle);

            [DllImport("pmulib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int PMU_SetGndSw(IntPtr hHandle, int onoff);

            [DllImport("pmulib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int PMU_SetPwr(IntPtr hHandle, int p_ch, float volt, float cut_A);

            [DllImport("pmulib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int PMU_SetPwrSw(IntPtr hHandle, int p_ch, int hl_off);

            [DllImport("pmulib.dll", CallingConvention = CallingConvention.StdCall)]
            public static extern int PMU_DoPwrMon(IntPtr hHandle, int onoff, uint src_load_flags);
        }

        #endregion

        #region Fields & Properties

        public string Name { get; }
        public string Type { get; }
        public int Index { get; }
        public int Bayer { get; private set; } = 0;
        public int Width { get; private set; } = 640;
        public int Height { get; private set; } = 480;
        public int BitDepth { get; private set; } = 8;
        public int Pitch { get; private set; } = 640;

        public bool IsRunning { get; private set; } = false;
        public bool IsInitialized { get; private set; } = false;

        private IntPtr _pmuHandle = IntPtr.Zero;
        private readonly Queue<Mat> _raw8Queue = new Queue<Mat>();
        private readonly Queue<Mat> _raw16Queue = new Queue<Mat>();
        private readonly Queue<Mat> _rgbQueue = new Queue<Mat>();
        private readonly object _queueLock = new object();

        private Thread? _captureThread;
        private CancellationTokenSource? _cts;

        private CameraViewer? _viewer;

        private VideoCapture? _usbCap;
        private readonly List<string> _simImageFiles = new List<string>();
        private int _simImageIndex = 0;

        private const int MaxQueueSize = 30;

        #endregion

        public LuaCamera(string name, string type, int index)
        {
            Name = name.ToLower();
            Type = type.ToLower();
            Index = index;
        }

        public static void RegisterCameraApis(Script script)
        {
            UserData.RegisterType<LuaCamera>();
            var cameraTable = new Table(script);
            cameraTable["create"] = (Func<string, string, int, LuaCamera>)((name, type, index) => new LuaCamera(name, type, index));
            script.Globals["camera"] = cameraTable;
        }

        private void EnqueueFrame(Queue<Mat> queue, Mat frame)
        {
            lock (_queueLock)
            {
                queue.Enqueue(frame.Clone());
                if (queue.Count > MaxQueueSize)
                {
                    var old = queue.Dequeue();
                    old.Dispose();
                }
            }
        }

        #region API Methods

        public void init()
        {
            if (IsInitialized) return;

            if (Name == "dc")
            {
                int ret = DcLib.DC_Init();
                if (ret != 0) throw new Exception($"DC_Init failed with code 0x{ret:X}");
            }
            else if (Name == "dg")
            {
                int ret = DgLib.DG_Init();
                if (ret != 0) throw new Exception($"DG_Init failed with code 0x{ret:X}");
            }
            else if (Name == "usb")
            {
                _usbCap = new VideoCapture(Index);
                if (!_usbCap.IsOpened()) throw new Exception($"Failed to open USB camera index {Index}");
            }

            IsInitialized = true;
        }

        public bool load_script(string script, int section)
        {
            if (Name != "dc" && Name != "dg") return false;

            try
            {
                string scriptPath = Path.Combine(@"c:\Users\eunha\Documents\Developments\nova2\Reference", script);
                if (!File.Exists(scriptPath))
                {
                    scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reference", script);
                }
                if (!File.Exists(scriptPath)) return false;

                var sections = ParseScriptSections(scriptPath);
                if (section < 1 || section > sections.Count) return false;

                var lines = sections[section - 1];
                foreach (var line in lines)
                {
                    ExecuteScriptLine(line);
                }
                return true;
            }
            catch (Exception ex)
            {
                GuiManager.PrintLog("ERROR", $"Error running script section: {ex.Message}");
                throw;
            }
        }

        public bool load_image(string image, int width, int height, int bit)
        {
            Width = width;
            Height = height;
            BitDepth = bit;
            Pitch = width * (bit > 8 ? 2 : 1);

            _simImageFiles.Clear();

            string fullPath = Path.Combine(@"c:\Users\eunha\Documents\Developments\nova2\Reference", image);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reference", image);
            }

            if (Directory.Exists(fullPath))
            {
                var files = Directory.GetFiles(fullPath);
                Array.Sort(files);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".raw" || ext == ".bmp" || ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    {
                        _simImageFiles.Add(file);
                    }
                }
            }
            else if (File.Exists(fullPath))
            {
                _simImageFiles.Add(fullPath);
            }
            else
            {
                return false;
            }

            if (_simImageFiles.Count == 0) return false;

            _simImageIndex = 0;
            return LoadFrameFromFile(_simImageFiles[0]);
        }

        public void set_bayer(int bayer)
        {
            Bayer = bayer;
        }

        public int get_bayer()
        {
            return Bayer;
        }

        public void run()
        {
            if (IsRunning) return;

            string viewerName = $"CamViewer_{Name}_{Index}";
            _viewer = new CameraViewer();
            _viewer.Show(viewerName, Width, Height);

            _cts = new CancellationTokenSource();
            IsRunning = true;

            if (Name == "dc") DcLib.DC_Start(Index, 0);
            else if (Name == "dg") DgLib.DG_Start(Index, 0);

            _captureThread = new Thread(CaptureLoop) { IsBackground = true };
            _captureThread.Start();
        }

        public Table get_frame(string name, int count)
        {
            var script = GuiManager.CurrentScript;
            if (script == null) throw new Exception("No active Lua script running.");

            var table = new Table(script);

            lock (_queueLock)
            {
                Queue<Mat> targetQueue = name.ToLower() switch
                {
                    "raw8" => _raw8Queue,
                    "raw16" => _raw16Queue,
                    "rgb" => _rgbQueue,
                    _ => _rgbQueue
                };

                var list = new List<Mat>(targetQueue);
                int start = Math.Max(0, list.Count - count);
                for (int i = start; i < list.Count; i++)
                {
                    table[i - start + 1] = MatWrapper.Create(list[i].Clone());
                }
            }

            return table;
        }

        public void stop()
        {
            if (!IsRunning) return;

            IsRunning = false;
            _cts?.Cancel();
            _captureThread?.Join(1000);

            if (Name == "dc") DcLib.DC_Stop(Index, 0);
            else if (Name == "dg") DgLib.DG_Stop(Index, 0);

            string viewerName = $"CamViewer_{Name}_{Index}";
            _viewer?.Close(viewerName);
            _viewer = null;
        }

        public void close()
        {
            stop();

            if (_usbCap != null)
            {
                _usbCap.Dispose();
                _usbCap = null;
            }

            if (_pmuHandle != IntPtr.Zero)
            {
                if (Name == "dc") DcLib.DC_ReleasePmuHandle(Index, _pmuHandle);
                else if (Name == "dg") DgLib.DG_ReleasePmuHandle(Index, _pmuHandle);
                _pmuHandle = IntPtr.Zero;
            }

            if (Name == "dc") DcLib.DC_Close(Index, 0);
            else if (Name == "dg") DgLib.DG_Close(Index, 0);

            lock (_queueLock)
            {
                ClearQueue(_raw8Queue);
                ClearQueue(_raw16Queue);
                ClearQueue(_rgbQueue);
            }

            IsInitialized = false;
        }

        #endregion

        #region Helper Methods

        private void ClearQueue(Queue<Mat> queue)
        {
            while (queue.Count > 0)
            {
                queue.Dequeue().Dispose();
            }
        }

        private void CaptureLoop()
        {
            var token = _cts!.Token;
            byte[] raw8Buf = new byte[Width * Height];
            short[] raw16Buf = new short[Width * Height];

            while (IsRunning && !token.IsCancellationRequested)
            {
                try
                {
                    if (Name == "dc" || Name == "dg")
                    {
                        IntPtr bufPtr = IntPtr.Zero;
                        ushort len = 0;
                        uint eofLen = 0;
                        int ret = (Name == "dc") ?
                            DcLib.DC_GetFrame(Index, 0, 1, ref bufPtr, ref len, ref eofLen) :
                            DgLib.DG_GetFrame(Index, 0, 1, ref bufPtr, ref len, ref eofLen);

                        if (ret == 0 && bufPtr != IntPtr.Zero)
                        {
                            UnpackMipiFrame(bufPtr, raw8Buf, raw16Buf, Width, Height);

                            Mat raw8Mat = new Mat(Height, Width, MatType.CV_8UC1);
                            Marshal.Copy(raw8Buf, 0, raw8Mat.Data, raw8Buf.Length);

                            Mat raw16Mat = new Mat(Height, Width, MatType.CV_16UC1);
                            Marshal.Copy(raw16Buf, 0, raw16Mat.Data, raw16Buf.Length);

                            Mat rgbMat = new Mat();

                            ColorConversionCodes code = Bayer switch
                            {
                                0 => ColorConversionCodes.BayerBG2RGB,
                                1 => ColorConversionCodes.BayerGB2RGB,
                                2 => ColorConversionCodes.BayerGR2RGB,
                                3 => ColorConversionCodes.BayerRG2RGB,
                                _ => ColorConversionCodes.BayerBG2RGB
                            };

                            try
                            {
                                Cv2.CvtColor(raw8Mat, rgbMat, code);
                            }
                            catch
                            {
                                Cv2.CvtColor(raw8Mat, rgbMat, ColorConversionCodes.GRAY2RGB);
                            }

                            EnqueueFrame(_raw8Queue, raw8Mat);
                            EnqueueFrame(_raw16Queue, raw16Mat);
                            EnqueueFrame(_rgbQueue, rgbMat);

                            _viewer?.Update(rgbMat);

                            raw8Mat.Dispose();
                            raw16Mat.Dispose();
                            rgbMat.Dispose();
                        }
                        else
                        {
                            Thread.Sleep(5);
                        }
                    }
                    else if (Name == "usb" && _usbCap != null)
                    {
                        using (Mat frame = new Mat())
                        {
                            if (_usbCap.Read(frame) && !frame.Empty())
                            {
                                using (Mat resized = new Mat())
                                using (Mat gray = new Mat())
                                using (Mat gray16 = new Mat())
                                {
                                    Cv2.Resize(frame, resized, new OpenCvSharp.Size(Width, Height));
                                    Cv2.CvtColor(resized, gray, ColorConversionCodes.BGR2GRAY);
                                    gray.ConvertTo(gray16, MatType.CV_16U, 256);

                                    using (Mat rgb = new Mat())
                                    {
                                        Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);
                                        EnqueueFrame(_raw8Queue, gray);
                                        EnqueueFrame(_raw16Queue, gray16);
                                        EnqueueFrame(_rgbQueue, rgb);

                                        _viewer?.Update(rgb);
                                    }
                                }
                            }
                        }
                        Thread.Sleep(33);
                    }
                    else if (Name == "sim" && _simImageFiles.Count > 0)
                    {
                        string file = _simImageFiles[_simImageIndex];
                        _simImageIndex = (_simImageIndex + 1) % _simImageFiles.Count;

                        LoadFrameFromFile(file);

                        lock (_queueLock)
                        {
                            if (_rgbQueue.Count > 0)
                            {
                                Mat? lastMat = null;
                                foreach (var m in _rgbQueue)
                                {
                                    lastMat = m;
                                }
                                if (lastMat != null)
                                {
                                    _viewer?.Update(lastMat);
                                }
                            }
                        }
                        Thread.Sleep(33);
                    }
                }
                catch (Exception ex)
                {
                    GuiManager.PrintLog("ERROR", $"Capture loop error: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
        }

        private void UnpackMipiFrame(IntPtr bufPtr, byte[] raw8Data, short[] raw16Data, int w, int h)
        {
            int totalBytes = Pitch * h;
            byte[] inputBytes = new byte[totalBytes];
            Marshal.Copy(bufPtr, inputBytes, 0, totalBytes);

            double ratio = (double)Pitch / w;

            if (Math.Abs(ratio - 1.0) < 0.01) // RAW8
            {
                for (int i = 0; i < w * h; i++)
                {
                    byte val = inputBytes[i];
                    raw8Data[i] = val;
                    raw16Data[i] = (short)(val << 8);
                }
            }
            else if (Math.Abs(ratio - 1.25) < 0.01) // RAW10
            {
                int outIdx = 0;
                for (int i = 0; i < totalBytes; i = i + 5)
                {
                    if (outIdx + 4 > w * h) break;
                    byte b0 = inputBytes[i];
                    byte b1 = inputBytes[i + 1];
                    byte b2 = inputBytes[i + 2];
                    byte b3 = inputBytes[i + 3];
                    byte b4 = inputBytes[i + 4];

                    short p0 = (short)((b0 << 2) | (b4 & 0x03));
                    short p1 = (short)((b1 << 2) | ((b4 >> 2) & 0x03));
                    short p2 = (short)((b2 << 2) | ((b4 >> 4) & 0x03));
                    short p3 = (short)((b3 << 2) | ((b4 >> 6) & 0x03));

                    raw16Data[outIdx] = p0;
                    raw16Data[outIdx + 1] = p1;
                    raw16Data[outIdx + 2] = p2;
                    raw16Data[outIdx + 3] = p3;

                    raw8Data[outIdx] = (byte)(p0 >> 2);
                    raw8Data[outIdx + 1] = (byte)(p1 >> 2);
                    raw8Data[outIdx + 2] = (byte)(p2 >> 2);
                    raw8Data[outIdx + 3] = (byte)(p3 >> 2);

                    outIdx = outIdx + 4;
                }
            }
            else if (Math.Abs(ratio - 1.5) < 0.01) // RAW12
            {
                int outIdx = 0;
                for (int i = 0; i < totalBytes; i = i + 3)
                {
                    if (outIdx + 2 > w * h) break;
                    byte b0 = inputBytes[i];
                    byte b1 = inputBytes[i + 1];
                    byte b2 = inputBytes[i + 2];

                    short p0 = (short)((b0 << 4) | (b2 & 0x0F));
                    short p1 = (short)((b1 << 4) | ((b2 >> 4) & 0x0F));

                    raw16Data[outIdx] = p0;
                    raw16Data[outIdx + 1] = p1;

                    raw8Data[outIdx] = (byte)(p0 >> 4);
                    raw8Data[outIdx + 1] = (byte)(p1 >> 4);

                    outIdx = outIdx + 2;
                }
            }
            else // Default to RAW16 / YUV422 (2 bytes per pixel)
            {
                for (int i = 0; i < w * h; i++)
                {
                    if (i * 2 + 1 >= totalBytes) break;
                    short val = (short)(inputBytes[i * 2] | (inputBytes[i * 2 + 1] << 8));
                    raw16Data[i] = val;
                    raw8Data[i] = inputBytes[i * 2];
                }
            }
        }

        private bool LoadFrameFromFile(string file)
        {
            try
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".raw")
                {
                    byte[] data = File.ReadAllBytes(file);
                    if (BitDepth == 8)
                    {
                        if (data.Length < Width * Height) return false;
                        Mat raw8Mat = new Mat(Height, Width, MatType.CV_8UC1);
                        Marshal.Copy(data, 0, raw8Mat.Data, data.Length);

                        Mat raw16Mat = new Mat(Height, Width, MatType.CV_16UC1);
                        raw8Mat.ConvertTo(raw16Mat, MatType.CV_16U, 256);

                        Mat rgbMat = new Mat();
                        ColorConversionCodes code = Bayer switch
                        {
                            0 => ColorConversionCodes.BayerBG2RGB,
                            1 => ColorConversionCodes.BayerGB2RGB,
                            2 => ColorConversionCodes.BayerGR2RGB,
                            3 => ColorConversionCodes.BayerRG2RGB,
                            _ => ColorConversionCodes.BayerBG2RGB
                        };
                        try
                        {
                            Cv2.CvtColor(raw8Mat, rgbMat, code);
                        }
                        catch
                        {
                            Cv2.CvtColor(raw8Mat, rgbMat, ColorConversionCodes.GRAY2RGB);
                        }

                        EnqueueFrame(_raw8Queue, raw8Mat);
                        EnqueueFrame(_raw16Queue, raw16Mat);
                        EnqueueFrame(_rgbQueue, rgbMat);

                        raw8Mat.Dispose();
                        raw16Mat.Dispose();
                        rgbMat.Dispose();
                    }
                    else
                    {
                        if (data.Length < Width * Height * 2) return false;
                        short[] udata = new short[Width * Height];
                        Buffer.BlockCopy(data, 0, udata, 0, data.Length);

                        Mat raw16Mat = new Mat(Height, Width, MatType.CV_16UC1);
                        Marshal.Copy(udata, 0, raw16Mat.Data, udata.Length);

                        Mat raw8Mat = new Mat(Height, Width, MatType.CV_8UC1);
                        raw16Mat.ConvertTo(raw8Mat, MatType.CV_8U, 1.0 / 256.0);

                        Mat rgbMat = new Mat();
                        ColorConversionCodes code = Bayer switch
                        {
                            0 => ColorConversionCodes.BayerBG2RGB,
                            1 => ColorConversionCodes.BayerGB2RGB,
                            2 => ColorConversionCodes.BayerGR2RGB,
                            3 => ColorConversionCodes.BayerRG2RGB,
                            _ => ColorConversionCodes.BayerBG2RGB
                        };
                        try
                        {
                            Cv2.CvtColor(raw8Mat, rgbMat, code);
                        }
                        catch
                        {
                            Cv2.CvtColor(raw8Mat, rgbMat, ColorConversionCodes.GRAY2RGB);
                        }

                        EnqueueFrame(_raw8Queue, raw8Mat);
                        EnqueueFrame(_raw16Queue, raw16Mat);
                        EnqueueFrame(_rgbQueue, rgbMat);

                        raw8Mat.Dispose();
                        raw16Mat.Dispose();
                        rgbMat.Dispose();
                    }
                }
                else // bmp/png/jpg
                {
                    using (Mat img = Cv2.ImRead(file, ImreadModes.Color))
                    {
                        if (img.Empty()) return false;
                        Mat rgbMat = new Mat();
                        Cv2.Resize(img, rgbMat, new OpenCvSharp.Size(Width, Height));

                        Mat raw8Mat = new Mat();
                        Cv2.CvtColor(rgbMat, raw8Mat, ColorConversionCodes.BGR2GRAY);

                        Mat raw16Mat = new Mat();
                        raw8Mat.ConvertTo(raw16Mat, MatType.CV_16U, 256);

                        using (Mat rgbClean = new Mat())
                        {
                            Cv2.CvtColor(rgbMat, rgbClean, ColorConversionCodes.BGR2RGB);
                            EnqueueFrame(_raw8Queue, raw8Mat);
                            EnqueueFrame(_raw16Queue, raw16Mat);
                            EnqueueFrame(_rgbQueue, rgbClean);
                        }

                        rgbMat.Dispose();
                        raw8Mat.Dispose();
                        raw16Mat.Dispose();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                GuiManager.PrintLog("ERROR", $"Error loading frame: {ex.Message}");
                throw;
            }
        }

        private List<List<string>> ParseScriptSections(string path)
        {
            var sections = new List<List<string>>();
            List<string>? currentSection = null;

            foreach (var line in File.ReadLines(path))
            {
                var cleanLine = line.Trim();
                if (cleanLine.StartsWith("[") && cleanLine.EndsWith("]"))
                {
                    currentSection = new List<string>();
                    sections.Add(currentSection);
                }
                else if (currentSection != null && !string.IsNullOrEmpty(cleanLine))
                {
                    currentSection.Add(cleanLine);
                }
            }
            return sections;
        }

        private void ExecuteScriptLine(string line)
        {
            if (line.StartsWith("//") || line.StartsWith("#")) return;

            var tokens = line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return;

            string cmd = tokens[0].ToLower();

            if (cmd == "open" && tokens.Length >= 4)
            {
                int height = ParseInt(tokens[1]);
                int pitch = ParseInt(tokens[2]);

                Height = height;
                Pitch = pitch;
                if (height == 3000) Width = 4000;
                else if (height == 1920) Width = 2560;
                else if (height == 1080) Width = 1920;
                else if (height == 960) Width = 1280;
                else Width = pitch / 2;

                int ret = (Name == "dc") ?
                    DcLib.DC_Open(Index, 0, Height, Pitch, 16, 8) :
                    DgLib.DG_Open(Index, 0, Height, Pitch, 16, 8);

                if (ret != 0) Console.WriteLine($"Open capture failed with code 0x{ret:X}");
            }
            else if (cmd == "set_test_cam" && tokens.Length >= 2)
            {
                int enable = ParseInt(tokens[1]);
                if (Name == "dc") DcLib.DC_SetMipiCsiTestCamEn(Index, 0, enable);
                else if (Name == "dg") DgLib.DG_SetTestCamEn(Index, 0, enable);
            }
            else if (cmd == "set_mipi_test_cam" && tokens.Length >= 2)
            {
                int enable = ParseInt(tokens[1]);
                if (Name == "dg") DgLib.DG_SetMipiTestCamEn(Index, 0, enable);
            }
            else if (cmd == "set_mipi_default" && tokens.Length >= 2)
            {
                int val = ParseInt(tokens[1]);
                if (Name == "dc") DcLib.DC_SetMipiInfo(Index, 0, 0, val);
                else if (Name == "dg") DgLib.DG_SetMipiDefault(Index, 0, val);
            }
            else if (cmd == "set_ism_io" && tokens.Length >= 4)
            {
                if (Name == "dg") DgLib.DG_SetIsmIo(Index, ParseInt(tokens[1]), ParseInt(tokens[2]), ParseInt(tokens[3]));
            }
            else if (cmd == "set_ism_default")
            {
                if (Name == "dg") DgLib.DG_SetIsmDefault(Index);
            }
            else if (cmd == "set_ism_volt" && tokens.Length >= 2)
            {
                if (Name == "dg") DgLib.DG_SetIsmIoVolt(Index, float.Parse(tokens[1]));
            }
            else if (cmd == "set_ism_freq" && tokens.Length >= 3)
            {
                if (Name == "dg") DgLib.DG_SetIsmFreq(Index, ParseInt(tokens[1]), float.Parse(tokens[2]));
            }
            else if (cmd == "set_ism_reset" && tokens.Length >= 2)
            {
                if (Name == "dg") DgLib.DG_SetIsmReset(Index, ParseInt(tokens[1]));
            }
            else if (cmd == "delay" && tokens.Length >= 2)
            {
                Thread.Sleep(ParseInt(tokens[1]));
            }
            else if (cmd == "set_i2c_freq" && tokens.Length >= 2)
            {
                float freq = float.Parse(tokens[1]);
                if (Name == "dc") DcLib.DC_SetHwI2cFreq(Index, freq);
                else if (Name == "dg") DgLib.DG_SetI2cFreq(Index, freq);
            }
            else if (cmd == "set_i2c" && tokens.Length >= 5)
            {
                byte start = ParseByte(tokens[1]);
                byte last = ParseByte(tokens[2]);
                byte slave = ParseByte(tokens[3]);
                int dataLen = tokens.Length - 4;
                byte[] buf = new byte[dataLen + 1];
                buf[0] = slave;
                for (int i = 0; i < dataLen; i++)
                {
                    buf[i + 1] = ParseByte(tokens[i + 4]);
                }

                if (Name == "dc") DcLib.DC_HwI2cWrite(Index, buf.Length, start, last, buf);
                else if (Name == "dg") DgLib.DG_I2cWrite(Index, buf.Length, start, last, buf);
            }
            else if (cmd == "set_pmu_init")
            {
                EnsurePmuHandle();
                PmuLib.PMU_Init(_pmuHandle);
            }
            else if (cmd == "set_pmu_gnd" && tokens.Length >= 2)
            {
                EnsurePmuHandle();
                PmuLib.PMU_SetGndSw(_pmuHandle, ParseInt(tokens[1]));
            }
            else if (cmd == "set_pmu_pwr" && tokens.Length >= 4)
            {
                EnsurePmuHandle();
                PmuLib.PMU_SetPwr(_pmuHandle, ParseInt(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3]));
            }
            else if (cmd == "set_pmu_sw" && tokens.Length >= 3)
            {
                EnsurePmuHandle();
                PmuLib.PMU_SetPwrSw(_pmuHandle, ParseInt(tokens[1]), ParseInt(tokens[2]));
            }
            else if (cmd == "set_pmu_mon" && tokens.Length >= 3)
            {
                EnsurePmuHandle();
                PmuLib.PMU_DoPwrMon(_pmuHandle, ParseInt(tokens[1]), ParseUInt(tokens[2]));
            }
            else if (cmd == "set_pmu_end")
            {
                EnsurePmuHandle();
                PmuLib.PMU_End(_pmuHandle);
            }
        }

        private void EnsurePmuHandle()
        {
            if (_pmuHandle != IntPtr.Zero) return;
            int ret = (Name == "dc") ?
                DcLib.DC_GetPmuHandle(Index, out _pmuHandle) :
                DgLib.DG_GetPmuHandle(Index, out _pmuHandle);

            if (ret != 0 || _pmuHandle == IntPtr.Zero)
            {
                throw new Exception($"Failed to retrieve PMU Handle for camera {Name}_{Index} (code 0x{ret:X})");
            }
        }

        private static int ParseInt(string token)
        {
            if (token.StartsWith("0x") || token.StartsWith("0X"))
            {
                return Convert.ToInt32(token, 16);
            }
            return int.Parse(token);
        }

        private static byte ParseByte(string token)
        {
            if (token.StartsWith("0x") || token.StartsWith("0X"))
            {
                return Convert.ToByte(token, 16);
            }
            return byte.Parse(token);
        }

        private static uint ParseUInt(string token)
        {
            if (token.StartsWith("0x") || token.StartsWith("0X"))
            {
                return Convert.ToUInt32(token, 16);
            }
            return uint.Parse(token);
        }

        #endregion

        public void Dispose()
        {
            close();
        }
    }
}
