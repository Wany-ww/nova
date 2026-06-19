# NOVA 2 (Flow-based Visual Scripting Engine)

NOVA 2 is a modern, high-performance flow-based visual programming environment designed for rapid prototyping of image processing and automation pipelines. It combines a sleek React-based node-editor interface with a robust C# WPF backend hosting a responsive Lua scripting runtime.

---

## 🚀 Key Features

### 1. Flow-Based Scripting Editor
- **Dynamic Node Editor**: Interactive drag-and-drop canvas built using `@xyflow/react` (React Flow).
- **Custom Node Creation**: Define nodes dynamically by placing `.lua` script files inside the local `nodes/` library. The system parses inputs/outputs from the script automatically.
- **Visual Execution Tracking**: Real-time styling updates showing active, running, and error states of nodes with custom animated glow transitions.

### 2. Powerful Scripting APIs
NOVA 2 integrates extensive Lua scripting APIs for control flow, diagnostics, and computer vision:
- **Logging & Diagnostics**: 
  - `log.info(...)` / `log.warn(...)` / `log.error(...)` for console output.
  - `log.clear()` programmatically clears the log panel.
  - `log.save(enable, filePath)` records logs in real-time to a file (defaults to `save/<YYYYMMDD>_log.txt`).
- **High-Precision Timing**:
  - `time.sleep.sec(seconds)` responsive delay using cooperative task yield.
  - `time.sleep.ms(milliseconds)` for millisecond timing.
  - `time.sleep.us(microseconds)` high-precision busy-wait timing (sub-millisecond resolution).
- **OpenCV & Image Processing**:
  - `cv.Mat()` wrapper to create and manipulate images/matrices.
  - `cv.imread(path)` and `cv.imwrite(path, mat)` for robust I/O.
  - `cv.imshow(windowName, mat)` updates docked UI panels or spawns floating views.
  - `cv.cvtColor()`, `cv.threshold()`, `cv.Canny()`, `cv.resize()`, etc.
- **OpenCV Drawing Utilities**:
  - `cv.rectangle()`, `cv.circle()`, `cv.line()`, `cv.putText()` for real-time annotations.
- **Resource Management**:
  - `mat:release()` to immediately free unmanaged C++ memory and prevent memory leaks in loop blocks.

### 3. Advanced Docking Layout System
- **Pixel-Perfect Drag-and-Drop Guides**: Spans the entire screen with interactive centering overlay boxes.
- **Recursive Layout Splits**: Supports nested horizontal/vertical docking splits (docking panels next to other docked panels) and window merging.
- **Flat Theme-Aligned Aesthetics**: Clean border-free styling with a thin 3px grid splitter aligning with Catppuccin and customized dark themes.

### 4. Interactive Help & Diagnostic Systems
- **Script API Reference**: An interactive help dialog accessible via `Help -> API Reference`.
  - Side-by-side category browser (`Logging`, `Time`, `OpenCV Core`, etc.).
  - Instant text-search indexing.
  - Formatted parameter/return listings and copyable example code blocks with copy success feedback.
- **Diagnostics Monitor**: Real-time graphs showing process CPU load, GPU load, RAM size, working sets, active thread counts, and CLR GC heap sizes.

### 5. Static Code Quality Evaluator (`analyze.py`)
- Automated analysis script evaluating C# and Lua files for:
  - Cyclomatic Complexity (CC)
  - Lines of Code (LOC)
  - Comment density (Documentation score)
  - Code Smells (Magic numbers, deep indentation, implicit global variables, raw print statements)
- Generates a glassmorphic report dashboard showing a dynamic **Report Card (코드 품질 성적표)**, overall letter grades (A+ through F), and individual file metric scorecards.

---

## 📂 Project Structure

```
nova2/
├── Backend/
│   └── FlowEngine/          # C# WPF Host Application code
│       ├── Engine/          # Lua parser, runner, projects and theme managers
│       ├── wwwroot/         # Compiled React static web assets (WebView2 target)
│       └── nodes/           # Pre-loaded Lua nodes library sorted by category
├── Frontend/                # Vite + React + TypeScript Source Code
│   ├── src/                 # Application component tree and styles
│   └── vite.config.ts       # Outputs compiled assets directly to Backend wwwroot
├── analyze.py               # Static code quality analyzer script
└── static_analysis_dashboard.html  # Generated quality dashboard report
```

---

## 🛠️ Getting Started

### Prerequisites
- **Node.js**: v18+ (for building the React frontend)
- **.NET SDK**: .NET 8.0 (for running the WPF Backend host)
- **Python**: v3+ (optional, for code quality evaluation)

### Build and Run

1. **Build the Frontend**:
   ```bash
   cd Frontend
   npm install
   npm run build
   ```

2. **Run the WPF Host Application**:
   Open Visual Studio or run through the command line:
   ```bash
   cd ../Backend/FlowEngine
   dotnet run
   ```

3. **Verify Code Quality**:
   ```bash
   cd ../../
   python analyze.py
   ```
