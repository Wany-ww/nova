import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  ReactFlow,
  MiniMap,
  Controls,
  Background,
  useNodesState,
  useEdgesState,
  addEdge
} from '@xyflow/react';
import type {
  Connection,
  Edge,
  Node,
  ReactFlowInstance
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import { Save, FolderOpen, Trash2, LogOut, Info, Folder, ChevronRight, ChevronDown, FileCode, Activity, X, Check, BookOpen, Search, Copy } from 'lucide-react';
import { bridge } from './utils/bridge';
import { CustomNode } from './components/CustomNode';
import type { CustomNodeData } from './components/CustomNode';
import { PropertyModal } from './components/PropertyModal';
import { ConsolePanel } from './components/ConsolePanel';
import type { LogEntry } from './components/ConsolePanel';
import { ThemeModal, THEME_PRESETS } from './components/ThemeModal';
import type { AppTheme } from './components/ThemeModal';

// Tree hierarchy structures for Sidebar
interface TreeNode {
  name: string;
  path: string;
  isFolder: boolean;
  nodeData?: any;
  children?: TreeNode[];
}

function buildTree(nodeLibrary: any[]): TreeNode[] {
  const root: TreeNode[] = [];
  
  nodeLibrary.forEach(node => {
    const parts = (node.path || '').split('/');
    let currentLevel = root;
    let accumulatedPath = "";
    
    for (let i = 0; i < parts.length - 1; i++) {
      const folderName = parts[i];
      accumulatedPath = accumulatedPath ? `${accumulatedPath}/${folderName}` : folderName;
      let folderNode = currentLevel.find(item => item.name === folderName && item.isFolder);
      if (!folderNode) {
        folderNode = {
          name: folderName,
          path: accumulatedPath,
          isFolder: true,
          children: []
        };
        currentLevel.push(folderNode);
        currentLevel.sort((a, b) => {
          if (a.isFolder && !b.isFolder) return -1;
          if (!a.isFolder && b.isFolder) return 1;
          return a.name.localeCompare(b.name);
        });
      }
      currentLevel = folderNode.children!;
    }
    
    currentLevel.push({
      name: node.name,
      path: node.path || node.name,
      isFolder: false,
      nodeData: node
    });
    
    currentLevel.sort((a, b) => {
      if (a.isFolder && !b.isFolder) return -1;
      if (!a.isFolder && b.isFolder) return 1;
      return a.name.localeCompare(b.name);
    });
  });
  
  return root;
}

const SidebarTreeNode: React.FC<{
  node: TreeNode;
  onDragStart: (event: React.DragEvent, nodeId: string) => void;
}> = ({ node, onDragStart }) => {
  const [isOpen, setIsOpen] = useState(true);

  if (node.isFolder) {
    return (
      <div style={{ marginLeft: '4px', marginTop: '4px' }}>
        <div 
          onClick={() => setIsOpen(!isOpen)}
          className="folder-item"
          style={{
            display: 'flex',
            alignItems: 'center',
            cursor: 'pointer',
            padding: '4px 6px',
            borderRadius: '4px',
            color: 'var(--text-color)',
            fontSize: '0.85rem',
            userSelect: 'none',
            transition: 'background 0.2s',
          }}
        >
          <span style={{ display: 'inline-flex', width: '14px', justifyContent: 'center', marginRight: '4px' }}>
            {isOpen ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
          </span>
          <Folder size={14} color="var(--warning-color, #f9e2af)" style={{ marginRight: '6px' }} />
          <span style={{ fontWeight: 500 }}>{node.name}</span>
        </div>
        {isOpen && node.children && (
          <div style={{ borderLeft: '1px dashed var(--border-color, #313244)', marginLeft: '12px', paddingLeft: '2px' }}>
            {node.children.map((child, idx) => (
              <SidebarTreeNode key={idx} node={child} onDragStart={onDragStart} />
            ))}
          </div>
        )}
      </div>
    );
  } else {
    return (
      <div
        className="node-lib-item-simple"
        draggable
        onDragStart={(event) => onDragStart(event, node.nodeData.id)}
        style={{
          display: 'flex',
          alignItems: 'center',
          padding: '6px 8px',
          margin: '2px 0 2px 8px',
          borderRadius: '6px',
          cursor: 'grab',
          backgroundColor: 'var(--sidebar-node-bg, var(--panel-bg))',
          border: '1px solid var(--border-color)',
          color: 'var(--sidebar-node-fg, var(--text-color))',
          transition: 'all 0.2s ease',
          fontSize: '0.85rem',
          userSelect: 'none'
        }}
      >
        <FileCode size={14} color="var(--info-color, #89b4fa)" style={{ marginRight: '6px' }} />
        <span style={{ fontWeight: 500 }}>{node.name}</span>
      </div>
    );
  }
};

const ResourceChart: React.FC<{
  data: number[];
  label: string;
  color: string;
  currentValue: number;
}> = ({ data, label, color, currentValue }) => {
  const width = 340;
  const height = 100;
  const maxVal = 100;
  const padding = 4; // Padding to prevent line clipping at boundaries
  
  let pathD = "";
  let areaD = "";
  
  if (data.length > 1) {
    const points = data.map((val, idx) => {
      const clampedVal = Math.max(0, Math.min(100, val));
      const x = (idx / (data.length - 1)) * width;
      const y = padding + (1 - clampedVal / maxVal) * (height - 2 * padding);
      return { x, y };
    });
    
    pathD = `M ${points[0].x} ${points[0].y} ` + points.slice(1).map(p => `L ${p.x} ${p.y}`).join(" ");
    areaD = `${pathD} L ${points[points.length - 1].x} ${height} L ${points[0].x} ${height} Z`;
  }
  
  return (
    <div style={{ backgroundColor: 'var(--bg-color)', padding: '12px', borderRadius: '8px', border: '1px solid var(--border-color)', display: 'flex', flexDirection: 'column', gap: '8px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <span style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-color)' }}>{label}</span>
        <span style={{ fontSize: '0.9rem', fontWeight: 700, color: color }}>{currentValue.toFixed(1)}%</span>
      </div>
      <div style={{ position: 'relative', width: `${width}px`, height: `${height}px`, overflow: 'hidden' }}>
        {data.length > 1 ? (
          <svg width={width} height={height} style={{ overflow: 'visible' }}>
            <defs>
              <linearGradient id={`grad-${label.replace(/\s+/g, '')}`} x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor={color} stopOpacity="0.3"/>
                <stop offset="100%" stopColor={color} stopOpacity="0.0"/>
              </linearGradient>
            </defs>
            <line x1="0" y1={height * 0.25} x2={width} y2={height * 0.25} stroke="var(--border-color)" strokeWidth="0.5" strokeDasharray="3,3" />
            <line x1="0" y1={height * 0.5} x2={width} y2={height * 0.5} stroke="var(--border-color)" strokeWidth="0.5" strokeDasharray="3,3" />
            <line x1="0" y1={height * 0.75} x2={width} y2={height * 0.75} stroke="var(--border-color)" strokeWidth="0.5" strokeDasharray="3,3" />
            
            <path d={areaD} fill={`url(#grad-${label.replace(/\s+/g, '')})`} />
            <path d={pathD} fill="none" stroke={color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        ) : (
          <div style={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '0.75rem', color: 'var(--text-muted)', fontStyle: 'italic' }}>
            Waiting for metrics...
          </div>
        )}
      </div>
    </div>
  );
};

function updateGraphFlowProperties(nodes: Node[], edges: Edge[]): Node[] {
  // Helper to find downstream nodes reachable via flow links
  const getDownstreamFlowNodes = (startNodeId: string) => {
    const visited = new Set<string>();
    const queue = [startNodeId];

    while (queue.length > 0) {
      const current = queue.shift()!;
      if (visited.has(current)) continue;
      visited.add(current);

      const outgoing = edges.filter(e => e.source === current && e.sourceHandle?.startsWith('flow_') && e.sourceHandle !== 'flow_in' && e.targetHandle === 'flow_in');
      for (const edge of outgoing) {
        if (!visited.has(edge.target)) {
          queue.push(edge.target);
        }
      }
    }

    return Array.from(visited);
  };

  return nodes.map(node => {
    const connectedHandles = edges
      .filter(edge => edge.target === node.id && edge.targetHandle)
      .map(edge => edge.targetHandle as string);

    const hasIncomingFlow = edges.some(edge => edge.target === node.id && edge.targetHandle === 'flow_in');

    // Calculate if the flow sequence starting at this node is currently running
    let isFlowRunning = false;
    if (!hasIncomingFlow) {
      const downstreamIds = getDownstreamFlowNodes(node.id);
      isFlowRunning = downstreamIds.some(id => {
        const n = nodes.find(x => x.id === id);
        return (n?.data?.properties as any)?.state === 'RUNNING';
      });
    }

    const currentData = node.data as any;
    
    // Check if we actually need to update to avoid unnecessary object allocations
    const isHandlesEqual = (currentData.connectedInputs || []).join(',') === connectedHandles.join(',');
    const isFlowDisabledEqual = currentData.isFlowDisabled === hasIncomingFlow;
    const isFlowRunningEqual = currentData.isFlowRunning === isFlowRunning;

    if (isHandlesEqual && isFlowDisabledEqual && isFlowRunningEqual) {
      return node;
    }

    return {
      ...node,
      data: {
        ...currentData,
        connectedInputs: connectedHandles,
        isFlowDisabled: hasIncomingFlow,
        isFlowRunning: isFlowRunning
      }
    };
  });
}

// Register custom node type using cast to bypass rigid TS node indexing
const nodeTypes = {
  custom: CustomNode as any
};

interface ApiDoc {
  name: string;
  category: string;
  signature: string;
  description: string;
  inputs: { name: string; type: string; desc: string }[];
  outputs: { type: string; desc: string }[];
  example: string;
}

const API_DOCS: ApiDoc[] = [
  {
    name: "log.info",
    category: "Logging",
    signature: "log.info(message: any)",
    description: "Prints an informational message to the log console with INFO severity level.",
    inputs: [{ name: "message", type: "any", desc: "The value or string message to log." }],
    outputs: [],
    example: "log.info(\"System initialized successfully.\")\nlog.info({ status = 200, data = \"OK\" })"
  },
  {
    name: "log.warn",
    category: "Logging",
    signature: "log.warn(message: any)",
    description: "Prints a warning message to the log console with WARN severity level.",
    inputs: [{ name: "message", type: "any", desc: "The value or warning message to log." }],
    outputs: [],
    example: "log.warn(\"Low memory threshold reached: \" .. tostring(memUsed))"
  },
  {
    name: "log.error",
    category: "Logging",
    signature: "log.error(message: any)",
    description: "Prints an error message to the log console with ERROR severity level.",
    inputs: [{ name: "message", type: "any", desc: "The value or error message to log." }],
    outputs: [],
    example: "log.error(\"Failed to open image file: \" .. filePath)"
  },
  {
    name: "log.clear",
    category: "Logging",
    signature: "log.clear()",
    description: "Clears all historical output lines from the frontend log console window.",
    inputs: [],
    outputs: [],
    example: "log.clear()\nlog.info(\"New execution session started.\")"
  },
  {
    name: "log.save",
    category: "Logging",
    signature: "log.save(enable: boolean, filePath?: string)",
    description: "Enables or disables real-time logging to a file. If enable is true, all printed logs are written to the file as they occur.",
    inputs: [
      { name: "enable", type: "boolean", desc: "True to enable file logging, False to disable." },
      { name: "filePath", type: "string", desc: "(Optional) File path relative to executable. If omitted, defaults to 'save\\<YYYYMMDD>_log.txt'." }
    ],
    outputs: [],
    example: "log.save(true) -- Save to default date-based log file\nlog.info(\"This will be saved.\")\nlog.save(true, \"custom_log.txt\") -- Save to a custom file\nlog.save(false) -- Stop file logging"
  },
  {
    name: "console.clear",
    category: "Logging",
    signature: "console.clear()",
    description: "Alias for log.clear(). Clears the frontend log console window.",
    inputs: [],
    outputs: [],
    example: "console.clear()"
  },
  {
    name: "console.save",
    category: "Logging",
    signature: "console.save(enable: boolean, filePath?: string)",
    description: "Alias for log.save(). Configures real-time file logging.",
    inputs: [
      { name: "enable", type: "boolean", desc: "True to enable file logging, False to disable." },
      { name: "filePath", type: "string", desc: "(Optional) File path relative to executable." }
    ],
    outputs: [],
    example: "console.save(true, \"save/console.log\")"
  },
  {
    name: "time.sleep.sec",
    category: "Time",
    signature: "time.sleep.sec(seconds: number)",
    description: "Suspends the execution of the current script node for the specified duration in seconds. Uses responsive steps to allow user stop interruption.",
    inputs: [{ name: "seconds", type: "number", desc: "The sleep duration in seconds (can be decimal, e.g., 0.5)." }],
    outputs: [],
    example: "log.info(\"Waiting...\")\ntime.sleep.sec(2.5) -- Sleep for 2.5 seconds\nlog.info(\"Resume\")"
  },
  {
    name: "time.sleep.ms",
    category: "Time",
    signature: "time.sleep.ms(milliseconds: number)",
    description: "Suspends the execution of the current script node for the specified duration in milliseconds.",
    inputs: [{ name: "milliseconds", type: "number", desc: "The sleep duration in milliseconds." }],
    outputs: [],
    example: "log.info(\"Fast pause\")\ntime.sleep.ms(250) -- Sleep for 250 milliseconds\nlog.info(\"Resume\")"
  },
  {
    name: "time.sleep.us",
    category: "Time",
    signature: "time.sleep.us(microseconds: number)",
    description: "Suspends execution for microseconds using a high-precision busy-wait loop (Stopwatch). Best for ultra-low latency timing, but consumes CPU cycles.",
    inputs: [{ name: "microseconds", type: "number", desc: "The sleep duration in microseconds (1 second = 1,000,000 microseconds)." }],
    outputs: [],
    example: "time.sleep.us(50) -- High-precision sleep for 50 microseconds"
  },
  {
    name: "time.sleep.micro",
    category: "Time",
    signature: "time.sleep.micro(microseconds: number)",
    description: "Alias for time.sleep.us(). Suspends execution for microseconds using high-precision busy-wait.",
    inputs: [{ name: "microseconds", type: "number", desc: "The sleep duration in microseconds." }],
    outputs: [],
    example: "time.sleep.micro(100)"
  },
  {
    name: "cv.Mat",
    category: "OpenCV Core",
    signature: "cv.Mat(rows?: number, cols?: number, type?: number | string) or cv.Mat(filePath: string) or cv.Mat(otherMat: Mat)",
    description: "Creates and initializes a new OpenCV MatWrapper instance (matrix/image). Can create an empty matrix, read from a file path, clone an existing matrix, or construct a sized matrix with type.",
    inputs: [
      { name: "rows", type: "number", desc: "(Optional) Number of rows (height) of the matrix." },
      { name: "cols", type: "number", desc: "(Optional) Number of columns (width) of the matrix." },
      { name: "type", type: "number | string", desc: "(Optional) Matrix type constant (e.g. cv.CV_8UC3, '8UC3')." },
      { name: "filePath", type: "string", desc: "If single string argument, loads image from the given file path." },
      { name: "otherMat", type: "Mat", desc: "If single UserData argument, clones the source matrix." }
    ],
    outputs: [{ type: "Mat", desc: "A wrapped OpenCV Mat object containing the allocated memory." }],
    example: "-- Create empty mat\nlocal emptyImg = cv.Mat()\n\n-- Load image from file\nlocal img = cv.Mat(\"C:/images/test.jpg\")\n\n-- Create black 640x480 RGB image\nlocal canvas = cv.Mat(480, 640, cv.CV_8UC3)\n\n-- Clone an image\nlocal copy = cv.Mat(img)"
  },
  {
    name: "cv.imread",
    category: "OpenCV Core",
    signature: "cv.imread(filename: string, flags?: number)",
    description: "Loads an image from the specified file path.",
    inputs: [
      { name: "filename", type: "string", desc: "The path of the image file to read." },
      { name: "flags", type: "number", desc: "(Optional) OpenCV ImreadModes flags. Defaults to color (cv.CV_8UC3)." }
    ],
    outputs: [{ type: "Mat", desc: "The loaded image matrix." }],
    example: "local img = cv.imread(\"test.png\", 1) -- 1 = IMREAD_COLOR\nif img:empty() then\n    log.error(\"Failed to load image\")\nend"
  },
  {
    name: "cv.imwrite",
    category: "OpenCV Core",
    signature: "cv.imwrite(filename: string, img: Mat)",
    description: "Saves the image matrix to the specified file path.",
    inputs: [
      { name: "filename", type: "string", desc: "Target output file path." },
      { name: "img", type: "Mat", desc: "The OpenCV Mat image to save." }
    ],
    outputs: [{ type: "boolean", desc: "True if saving succeeded, otherwise False." }],
    example: "local success = cv.imwrite(\"output/processed.jpg\", img)\nif success then\n    log.info(\"Image saved successfully!\")\nend"
  },
  {
    name: "cv.imshow",
    category: "OpenCV Core",
    signature: "cv.imshow(winname: string, img: Mat)",
    description: "Displays the image matrix in the specified display tab. If the window name matches an existing docked view direction, updates that panel. Otherwise, displays it in a separate floating window.",
    inputs: [
      { name: "winname", type: "string", desc: "The title/key of the window/tab to display." },
      { name: "img", type: "Mat", desc: "The OpenCV Mat image to show." }
    ],
    outputs: [],
    example: "local img = cv.imread(\"Lenna.png\")\ncv.imshow(\"Lenna View\", img)"
  },
  {
    name: "cv.cvtColor",
    category: "OpenCV Processing",
    signature: "cv.cvtColor(src: Mat, code: number)",
    description: "Converts an image from one color space to another (e.g. BGR to Grayscale).",
    inputs: [
      { name: "src", type: "Mat", desc: "The source OpenCV Mat image." },
      { name: "code", type: "number", desc: "The color space conversion code constant (e.g. cv.COLOR_BGR2GRAY)." }
    ],
    outputs: [{ type: "Mat", desc: "The converted color space image matrix." }],
    example: "local gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)\ncv.imshow(\"Gray Output\", gray)\ngray:release() -- Free unmanaged memory"
  },
  {
    name: "cv.threshold",
    category: "OpenCV Processing",
    signature: "cv.threshold(src: Mat, thresh: number, maxval: number, type: number)",
    description: "Applies a fixed-level thresholding to a single-channel grayscale image. Returns a table containing the threshold value and the thresholded output matrix.",
    inputs: [
      { name: "src", type: "Mat", desc: "Single-channel source image (Grayscale)." },
      { name: "thresh", type: "number", desc: "Threshold value." },
      { name: "maxval", type: "number", desc: "Maximum value to use with THRESH_BINARY and THRESH_BINARY_INV." },
      { name: "type", type: "number", desc: "OpenCV ThresholdTypes code constant (e.g. cv.THRESH_BINARY, cv.THRESH_OTSU)." }
    ],
    outputs: [
      { type: "table", desc: "A Lua table: index [1] contains the threshold value (double), index [2] contains the output thresholded Mat." }
    ],
    example: "-- Apply Otsu binarization\nlocal result = cv.threshold(gray, 0, 255, cv.THRESH_BINARY + cv.THRESH_OTSU)\nlocal threshVal = result[1]\nlocal binImg = result[2]\nlog.info(\"Computed Otsu Thresh: \" .. tostring(threshVal))\ncv.imshow(\"Binary\", binImg)\nbinImg:release()"
  },
  {
    name: "cv.Canny",
    category: "OpenCV Processing",
    signature: "cv.Canny(src: Mat, threshold1: number, threshold2: number)",
    description: "Finds edges in an image using the Canny algorithm.",
    inputs: [
      { name: "src", type: "Mat", desc: "Source image." },
      { name: "threshold1", type: "number", desc: "First threshold for the hysteresis procedure." },
      { name: "threshold2", type: "number", desc: "Second threshold for the hysteresis procedure." }
    ],
    outputs: [{ type: "Mat", desc: "The single-channel edge image matrix." }],
    example: "local edges = cv.Canny(img, 50, 150)\ncv.imshow(\"Edges\", edges)\nedges:release()"
  },
  {
    name: "cv.resize",
    category: "OpenCV Processing",
    signature: "cv.resize(src: Mat, width: number, height: number)",
    description: "Resizes the image to the specified width and height.",
    inputs: [
      { name: "src", type: "Mat", desc: "Source image." },
      { name: "width", type: "number", desc: "Target width in pixels." },
      { name: "height", type: "number", desc: "Target height in pixels." }
    ],
    outputs: [{ type: "Mat", desc: "The resized image matrix." }],
    example: "local resized = cv.resize(img, 320, 240)\ncv.imshow(\"Thumb\", resized)\nresized:release()"
  },
  {
    name: "cv.rectangle",
    category: "OpenCV Drawing",
    signature: "cv.rectangle(img: Mat, x1: number, y1: number, x2: number, y2: number, color?: table | number[], thickness?: number)",
    description: "Draws a simple, thick, or filled rectangle outline on the image.",
    inputs: [
      { name: "img", type: "Mat", desc: "The target image matrix to draw on." },
      { name: "x1", type: "number", desc: "X-coordinate of the starting corner." },
      { name: "y1", type: "number", desc: "Y-coordinate of the starting corner." },
      { name: "x2", type: "number", desc: "X-coordinate of the opposite corner." },
      { name: "y2", type: "number", desc: "Y-coordinate of the opposite corner." },
      { name: "color", type: "table | number[]", desc: "(Optional) Color table with BGR structure, e.g. {r, g, b} or {r, g, b, a}. Default is White." },
      { name: "thickness", type: "number", desc: "(Optional) Line thickness. Negative value (e.g. -1) fills the rectangle. Default is 1." }
    ],
    outputs: [],
    example: "-- Draw red outline\ncv.rectangle(img, 50, 50, 200, 200, {255, 0, 0}, 2)\n\n-- Draw filled green rectangle\ncv.rectangle(img, 300, 10, 400, 100, {0, 255, 0}, -1)"
  },
  {
    name: "cv.circle",
    category: "OpenCV Drawing",
    signature: "cv.circle(img: Mat, cx: number, cy: number, radius: number, color?: table | number[], thickness?: number)",
    description: "Draws a circle outline or filled circle on the image.",
    inputs: [
      { name: "img", type: "Mat", desc: "The target image matrix to draw on." },
      { name: "cx", type: "number", desc: "X-coordinate of the center point." },
      { name: "cy", type: "number", desc: "Y-coordinate of the center point." },
      { name: "radius", type: "number", desc: "Radius of the circle." },
      { name: "color", type: "table | number[]", desc: "(Optional) Color BGR table {r, g, b}. Default is White." },
      { name: "thickness", type: "number", desc: "(Optional) Circle outline thickness. Negative value fills it. Default is 1." }
    ],
    outputs: [],
    example: "-- Draw blue circle\ncv.circle(img, 150, 150, 50, {0, 0, 255}, 3)"
  },
  {
    name: "cv.line",
    category: "OpenCV Drawing",
    signature: "cv.line(img: Mat, x1: number, y1: number, x2: number, y2: number, color?: table | number[], thickness?: number)",
    description: "Draws a straight line segment between two points on the image.",
    inputs: [
      { name: "img", type: "Mat", desc: "The target image matrix to draw on." },
      { name: "x1", type: "number", desc: "X-coordinate of the start point." },
      { name: "y1", type: "number", desc: "Y-coordinate of the start point." },
      { name: "x2", type: "number", desc: "X-coordinate of the end point." },
      { name: "y2", type: "number", desc: "Y-coordinate of the end point." },
      { name: "color", type: "table | number[]", desc: "(Optional) Color BGR table {r, g, b}. Default is White." },
      { name: "thickness", type: "number", desc: "(Optional) Line thickness. Default is 1." }
    ],
    outputs: [],
    example: "-- Draw yellow line\ncv.line(img, 10, 10, 600, 10, {255, 255, 0}, 4)"
  },
  {
    name: "cv.putText",
    category: "OpenCV Drawing",
    signature: "cv.putText(img: Mat, text: string, x: number, y: number, fontScale?: number, color?: table | number[], thickness?: number)",
    description: "Draws a text string on the image at the specified position.",
    inputs: [
      { name: "img", type: "Mat", desc: "The target image matrix to draw on." },
      { name: "text", type: "string", desc: "Text string to be drawn." },
      { name: "x", type: "number", desc: "X-coordinate of the bottom-left corner of the text." },
      { name: "y", type: "number", desc: "Y-coordinate of the bottom-left corner of the text." },
      { name: "fontScale", type: "number", desc: "(Optional) Font scale factor that is multiplied by the font-specific base size. Default is 1.0." },
      { name: "color", type: "table | number[]", desc: "(Optional) Color BGR table {r, g, b}. Default is White." },
      { name: "thickness", type: "number", desc: "(Optional) Line thickness. Default is 1." }
    ],
    outputs: [],
    example: "cv.putText(img, \"NOVA engine\", 20, 40, 1.2, {255, 255, 255}, 2)"
  },
  {
    name: "cv.GaussianBlur",
    category: "OpenCV Processing",
    signature: "cv.GaussianBlur(src: Mat, ksize_w: number, ksize_h: number, sigmaX: number, sigmaY?: number, borderType?: number)",
    description: "Blurs an image using a Gaussian filter.",
    inputs: [
      { name: "src", type: "Mat", desc: "The source image matrix." },
      { name: "ksize_w", type: "number", desc: "Gaussian kernel width (must be positive and odd)." },
      { name: "ksize_h", type: "number", desc: "Gaussian kernel height (must be positive and odd)." },
      { name: "sigmaX", type: "number", desc: "Gaussian kernel standard deviation in X direction." },
      { name: "sigmaY", type: "number", desc: "(Optional) Gaussian kernel standard deviation in Y direction. Default is 0.0." },
      { name: "borderType", type: "number", desc: "(Optional) Pixel extrapolation method. Default is Reflect101." }
    ],
    outputs: [{ type: "Mat", desc: "The blurred destination image matrix." }],
    example: "local blurred = cv.GaussianBlur(img, 5, 5, 1.5)\ncv.imshow(\"Gaussian Blur\", blurred)\nblurred:release()"
  },
  {
    name: "cv.medianBlur",
    category: "OpenCV Processing",
    signature: "cv.medianBlur(src: Mat, ksize: number)",
    description: "Blurs an image using a median filter (great for removing salt-and-pepper noise).",
    inputs: [
      { name: "src", type: "Mat", desc: "The source image matrix." },
      { name: "ksize", type: "number", desc: "Aperture linear size (must be positive and odd, e.g. 3, 5, 7)." }
    ],
    outputs: [{ type: "Mat", desc: "The blurred destination image matrix." }],
    example: "local clean = cv.medianBlur(noisyImg, 5)\ncv.imshow(\"Median Filtered\", clean)\nclean:release()"
  },
  {
    name: "cv.getStructuringElement",
    category: "OpenCV Processing",
    signature: "cv.getStructuringElement(shape: number, kw: number, kh: number)",
    description: "Returns a structuring element (kernel matrix) of the specified size and shape for morphological operations.",
    inputs: [
      { name: "shape", type: "number", desc: "Element shape (cv.MORPH_RECT, cv.MORPH_CROSS, or cv.MORPH_ELLIPSE)." },
      { name: "kw", type: "number", desc: "Structuring element width." },
      { name: "kh", type: "number", desc: "Structuring element height." }
    ],
    outputs: [{ type: "Mat", desc: "Structuring element matrix kernel." }],
    example: "local kernel = cv.getStructuringElement(cv.MORPH_RECT, 3, 3)\nkernel:release()"
  },
  {
    name: "cv.erode",
    category: "OpenCV Processing",
    signature: "cv.erode(src: Mat, element: Mat, iterations?: number)",
    description: "Erodes an image by using a specific structuring element.",
    inputs: [
      { name: "src", type: "Mat", desc: "The source image matrix." },
      { name: "element", type: "Mat", desc: "Structuring element kernel used for erosion." },
      { name: "iterations", type: "number", desc: "(Optional) Number of times erosion is applied. Default is 1." }
    ],
    outputs: [{ type: "Mat", desc: "The eroded destination image matrix." }],
    example: "local kernel = cv.getStructuringElement(cv.MORPH_RECT, 3, 3)\nlocal eroded = cv.erode(img, kernel, 1)\ncv.imshow(\"Erosion\", eroded)\neroded:release()\nkernel:release()"
  },
  {
    name: "cv.dilate",
    category: "OpenCV Processing",
    signature: "cv.dilate(src: Mat, element: Mat, iterations?: number)",
    description: "Dilates an image by using a specific structuring element.",
    inputs: [
      { name: "src", type: "Mat", desc: "The source image matrix." },
      { name: "element", type: "Mat", desc: "Structuring element kernel used for dilation." },
      { name: "iterations", type: "number", desc: "(Optional) Number of times dilation is applied. Default is 1." }
    ],
    outputs: [{ type: "Mat", desc: "The dilated destination image matrix." }],
    example: "local kernel = cv.getStructuringElement(cv.MORPH_RECT, 3, 3)\nlocal dilated = cv.dilate(img, kernel, 1)\ncv.imshow(\"Dilation\", dilated)\ndilated:release()\nkernel:release()"
  },
  {
    name: "cv.getRotationMatrix2D",
    category: "OpenCV Processing",
    signature: "cv.getRotationMatrix2D(cx: number, cy: number, angle: number, scale: number)",
    description: "Calculates an affine matrix of 2D rotation.",
    inputs: [
      { name: "cx", type: "number", desc: "X-coordinate of the center of rotation." },
      { name: "cy", type: "number", desc: "Y-coordinate of the center of rotation." },
      { name: "angle", type: "number", desc: "Rotation angle in degrees. Positive values mean counter-clockwise rotation." },
      { name: "scale", type: "number", desc: "Isotropic scale factor." }
    ],
    outputs: [{ type: "Mat", desc: "The computed 2x3 affine rotation matrix." }],
    example: "local M = cv.getRotationMatrix2D(100, 100, 45, 1.0)\nM:release()"
  },
  {
    name: "cv.warpAffine",
    category: "OpenCV Processing",
    signature: "cv.warpAffine(src: Mat, M: Mat, dw: number, dh: number, flags?: number, borderMode?: number)",
    description: "Applies an affine transformation to an image.",
    inputs: [
      { name: "src", type: "Mat", desc: "The source image matrix." },
      { name: "M", type: "Mat", desc: "2x3 transformation matrix." },
      { name: "dw", type: "number", desc: "Width of the destination image." },
      { name: "dh", type: "number", desc: "Height of the destination image." },
      { name: "flags", type: "number", desc: "(Optional) Combination of interpolation methods (e.g. cv.INTER_LINEAR)." },
      { name: "borderMode", type: "number", desc: "(Optional) Pixel extrapolation method (e.g. cv.BORDER_CONSTANT)." }
    ],
    outputs: [{ type: "Mat", desc: "The warped destination image matrix." }],
    example: "local M = cv.getRotationMatrix2D(img:cols()/2, img:rows()/2, 30, 1.0)\nlocal warped = cv.warpAffine(img, M, img:cols(), img:rows())\ncv.imshow(\"Rotated\", warped)\nwarped:release()\nM:release()"
  },
  {
    name: "cv.bitwise_and",
    category: "OpenCV Processing",
    signature: "cv.bitwise_and(src1: Mat, src2: Mat, mask?: Mat)",
    description: "Computes bitwise conjunction of two matrices element-wise.",
    inputs: [
      { name: "src1", type: "Mat", desc: "First source matrix." },
      { name: "src2", type: "Mat", desc: "Second source matrix." },
      { name: "mask", type: "Mat", desc: "(Optional) Operation mask. Specifies elements of the output matrix to be changed." }
    ],
    outputs: [{ type: "Mat", desc: "The logical AND destination matrix." }],
    example: "local output = cv.bitwise_and(img1, img2)\noutput:release()"
  },
  {
    name: "cv.bitwise_or",
    category: "OpenCV Processing",
    signature: "cv.bitwise_or(src1: Mat, src2: Mat, mask?: Mat)",
    description: "Computes bitwise bgr/gray disjunction of two matrices element-wise.",
    inputs: [
      { name: "src1", type: "Mat", desc: "First source matrix." },
      { name: "src2", type: "Mat", desc: "Second source matrix." },
      { name: "mask", type: "Mat", desc: "(Optional) Operation mask." }
    ],
    outputs: [{ type: "Mat", desc: "The logical OR destination matrix." }],
    example: "local output = cv.bitwise_or(img1, img2)\noutput:release()"
  },
  {
    name: "cv.bitwise_xor",
    category: "OpenCV Processing",
    signature: "cv.bitwise_xor(src1: Mat, src2: Mat, mask?: Mat)",
    description: "Computes bitwise exclusive-or of two matrices element-wise.",
    inputs: [
      { name: "src1", type: "Mat", desc: "First source matrix." },
      { name: "src2", type: "Mat", desc: "Second source matrix." },
      { name: "mask", type: "Mat", desc: "(Optional) Operation mask." }
    ],
    outputs: [{ type: "Mat", desc: "The logical XOR destination matrix." }],
    example: "local output = cv.bitwise_xor(img1, img2)\noutput:release()"
  },
  {
    name: "cv.bitwise_not",
    category: "OpenCV Processing",
    signature: "cv.bitwise_not(src: Mat, mask?: Mat)",
    description: "Inverts every bit of an array.",
    inputs: [
      { name: "src", type: "Mat", desc: "Source matrix." },
      { name: "mask", type: "Mat", desc: "(Optional) Operation mask." }
    ],
    outputs: [{ type: "Mat", desc: "The logical inverted destination matrix." }],
    example: "local inverted = cv.bitwise_not(img)\ncv.imshow(\"Inverted\", inverted)\ninverted:release()"
  },
  {
    name: "cv.split",
    category: "OpenCV Core",
    signature: "cv.split(src: Mat)",
    description: "Divides a multi-channel array into several single-channel arrays.",
    inputs: [{ name: "src", type: "Mat", desc: "Source multi-channel matrix." }],
    outputs: [{ type: "table", desc: "An array list containing individual single-channel Mat elements." }],
    example: "local channels = cv.split(rgbImg)\nlocal blue = channels[1]\nlocal green = channels[2]\nlocal red = channels[3]\ncv.imshow(\"Blue Channel\", blue)\nblue:release()\ngreen:release()\nred:release()"
  },
  {
    name: "cv.merge",
    category: "OpenCV Core",
    signature: "cv.merge(channels: table)",
    description: "Creates one multi-channel array out of several single-channel ones.",
    inputs: [{ name: "channels", type: "table", desc: "An array table list containing individual single-channel Mat elements." }],
    outputs: [{ type: "Mat", desc: "Combined multi-channel destination matrix." }],
    example: "local rgbImg = cv.merge({bChannel, gChannel, rChannel})"
  },
  {
    name: "cv.matchTemplate",
    category: "OpenCV Processing",
    signature: "cv.matchTemplate(image: Mat, templ: Mat, method: number)",
    description: "Compares a template against overlapped image regions.",
    inputs: [
      { name: "image", type: "Mat", desc: "Image where the search is running." },
      { name: "templ", type: "Mat", desc: "Searched template. It must be not greater than the source image." },
      { name: "method", type: "number", desc: "OpenCV TemplateMatchModes constant (e.g. cv.TM_CCOEFF_NORMED)." }
    ],
    outputs: [{ type: "Mat", desc: "Comparison map matrix of type CV_32FC1." }],
    example: "local map = cv.matchTemplate(img, template, cv.TM_CCOEFF_NORMED)\nmap:release()"
  },
  {
    name: "cv.minMaxLoc",
    category: "OpenCV Processing",
    signature: "cv.minMaxLoc(src: Mat)",
    description: "Finds the global minimum and maximum values and their locations in a single-channel array.",
    inputs: [{ name: "src", type: "Mat", desc: "Source single-channel matrix." }],
    outputs: [{ type: "table", desc: "Table structure containing fields: minVal, maxVal, minLoc (table {x,y}), and maxLoc (table {x,y})." }],
    example: "local map = cv.matchTemplate(img, template, cv.TM_CCOEFF_NORMED)\nlocal locs = cv.minMaxLoc(map)\nlog.info(\"Max match confidence: \" .. tostring(locs.maxVal))\ncv.rectangle(img, locs.maxLoc.x, locs.maxLoc.y, locs.maxLoc.x + template:cols(), locs.maxLoc.y + template:rows(), {0, 255, 0}, 2)"
  },
  {
    name: "cv.findContours",
    category: "OpenCV Processing",
    signature: "cv.findContours(src: Mat, mode: number, method: number)",
    description: "Finds contours in a binary image.",
    inputs: [
      { name: "src", type: "Mat", desc: "Source 8-bit single-channel binary image." },
      { name: "mode", type: "number", desc: "Contour retrieval mode (e.g. cv.RETR_EXTERNAL)." },
      { name: "method", type: "number", desc: "Contour approximation method (e.g. cv.CHAIN_APPROX_SIMPLE)." }
    ],
    outputs: [{ type: "table", desc: "A list of contours. Each contour is an array list of points: { {x=10, y=20}, {x=12, y=21}, ... }." }],
    example: "local bin = cv.Canny(img, 50, 150)\nlocal contours = cv.findContours(bin, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_SIMPLE)\nlog.info(\"Found contours: \" .. tostring(#contours))"
  },
  {
    name: "cv.drawContours",
    category: "OpenCV Drawing",
    signature: "cv.drawContours(img: Mat, contours: table, contourIdx: number, color: table | number[], thickness: number)",
    description: "Draws contours outlines or filled contours.",
    inputs: [
      { name: "img", type: "Mat", desc: "Destination image to draw on." },
      { name: "contours", type: "table", desc: "All the contours table list returned from findContours." },
      { name: "contourIdx", type: "number", desc: "Parameter indicating a contour to draw. If it is negative, all the contours are drawn." },
      { name: "color", type: "table | number[]", desc: "Color BGR table {r, g, b}." },
      { name: "thickness", type: "number", desc: "Thickness of lines the contours are drawn with. If it is negative (e.g. -1), the contour interiors are filled." }
    ],
    outputs: [],
    example: "cv.drawContours(img, contours, -1, {0, 255, 0}, 2)"
  },
  {
    name: "cv.boundingRect",
    category: "OpenCV Processing",
    signature: "cv.boundingRect(contour: table)",
    description: "Calculates the up-right bounding rectangle of a point set/contour.",
    inputs: [{ name: "contour", type: "table", desc: "A single contour (list of points {x, y}) from the list." }],
    outputs: [{ type: "table", desc: "Bounding box table containing fields: x, y, width, height." }],
    example: "for i = 1, #contours do\n    local rect = cv.boundingRect(contours[i])\n    cv.rectangle(img, rect.x, rect.y, rect.x + rect.width, rect.y + rect.height, {255, 0, 0}, 1)\nend"
  },
  {
    name: "Mat:release",
    category: "Mat Wrapper",
    signature: "mat:release()",
    description: "Explicitly releases the underlying C++ unmanaged OpenCV Mat resources. Highly recommended inside loop execution blocks to prevent unmanaged memory overhead.",
    inputs: [],
    outputs: [],
    example: "local mat = cv.imread(\"sample.bmp\")\n-- Process image...\nmat:release() -- Immediately frees unmanaged memory"
  },
  {
    name: "Mat:clone",
    category: "Mat Wrapper",
    signature: "mat:clone()",
    description: "Creates an exact deep copy of the matrix.",
    inputs: [],
    outputs: [{ type: "Mat", desc: "The cloned MatWrapper duplicate matrix." }],
    example: "local dup = img:clone()"
  },
  {
    name: "Mat:empty",
    category: "Mat Wrapper",
    signature: "mat:empty()",
    description: "Checks if the matrix contains no elements (uninitialized or failed to read).",
    inputs: [],
    outputs: [{ type: "boolean", desc: "True if the matrix is empty, otherwise False." }],
    example: "if img:empty() then\n    log.error(\"Empty image detected!\")\nend"
  },
  {
    name: "Mat member fields",
    category: "Mat Wrapper",
    signature: "mat.width, mat.height, mat.cols, mat.rows, mat.channels",
    description: "Member fields retrieving matrix size and properties.",
    inputs: [],
    outputs: [
      { type: "number", desc: "width / cols: image width in pixels." },
      { type: "number", desc: "height / rows: image height in pixels." },
      { type: "number", desc: "channels: number of color channels (e.g. 1 for grayscale, 3 for RGB)." }
    ],
    example: "log.info(\"Image Dim: \" .. tostring(img.width) .. \"x\" .. tostring(img.height))\nlog.info(\"Channels: \" .. tostring(img.channels))"
  },
  {
    name: "variable.set",
    category: "Global Memory",
    signature: "variable.set(name: string, value: any)",
    description: "Saves a value of type bool, int, float, string, or table to the global memory under a specific variable name. This value is shared across all executing script nodes and persists throughout the execution run.",
    inputs: [
      { name: "name", type: "string", desc: "The identifier/name of the variable." },
      { name: "value", type: "any", desc: "The value to store (bool, int, float, string, or table)." }
    ],
    outputs: [],
    example: "-- Store a numeric value\nvariable.set(\"globalCount\", 10)\n\n-- Store a configuration table\nvariable.set(\"options\", { threshold = 128, mode = \"binary\" })"
  },
  {
    name: "variable.get",
    category: "Global Memory",
    signature: "variable.get(name: string)",
    description: "Retrieves a variable stored in global memory by name. Returns the value, or nil if the variable does not exist.",
    inputs: [
      { name: "name", type: "string", desc: "The identifier/name of the variable to retrieve." }
    ],
    outputs: [
      { type: "any", desc: "The retrieved value (bool, int, float, string, table), or nil if not found." }
    ],
    example: "-- Retrieve a value\nlocal cnt = variable.get(\"globalCount\")\nif cnt then\n    log.info(\"Current count: \" .. tostring(cnt))\nend\n\n-- Retrieve a table\nlocal opt = variable.get(\"options\")\nif opt then\n    log.info(\"Threshold: \" .. tostring(opt.threshold))\nend"
  },
  {
    name: "filesystem.current",
    category: "Filesystem",
    signature: "filesystem.current()",
    description: "Returns the current working directory path of the active application process.",
    inputs: [],
    outputs: [{ type: "string", desc: "The absolute path of the current directory." }],
    example: "local dir = filesystem.current()\nlog.info(\"Current directory: \" .. dir)"
  },
  {
    name: "filesystem.remove",
    category: "Filesystem",
    signature: "filesystem.remove(path: string)",
    description: "Forcefully deletes the file or directory at the specified path (including all subdirectories and files recursively).",
    inputs: [{ name: "path", type: "string", desc: "The absolute or relative path of the file or directory to delete." }],
    outputs: [],
    example: "filesystem.remove(\"output/old_temp\")\nfilesystem.remove(\"temp_log.txt\")"
  },
  {
    name: "filesystem.create",
    category: "Filesystem",
    signature: "filesystem.create(path: string)",
    description: "Recursively creates all directories and subdirectories along the specified path unless they already exist.",
    inputs: [{ name: "path", type: "string", desc: "The directory path to create." }],
    outputs: [],
    example: "filesystem.create(\"output/images/processed\")"
  },
  {
    name: "filesystem.is_exist",
    category: "Filesystem",
    signature: "filesystem.is_exist(file: string)",
    description: "Checks if a file or directory exists at the specified path.",
    inputs: [{ name: "file", type: "string", desc: "The path of the file or directory to check." }],
    outputs: [{ type: "boolean", desc: "True if the file or directory exists, otherwise False." }],
    example: "if filesystem.is_exist(\"save/2026_log.txt\") then\n    log.info(\"Log file exists.\")\nend"
  },
  {
    name: "filesystem.copy",
    category: "Filesystem",
    signature: "filesystem.copy(src: string, dst: string)",
    description: "Copies a file or an entire directory recursively from the source path to the destination path.",
    inputs: [
      { name: "src", type: "string", desc: "The source file or directory path." },
      { name: "dst", type: "string", desc: "The target destination path." }
    ],
    outputs: [],
    example: "filesystem.copy(\"config.json\", \"config_backup.json\")\nfilesystem.copy(\"nodes/\", \"backup/nodes_backup/\")"
  },
  {
    name: "tcp.server.create",
    category: "Network",
    signature: "tcp.server.create(port: number)",
    description: "Starts a TCP listener on the specified port. Returns a socket object that accepts incoming connections, or nil if creation fails.",
    inputs: [{ name: "port", type: "number", desc: "The TCP port number to listen on." }],
    outputs: [{ type: "socket", desc: "A TCP server socket object, or nil on failure." }],
    example: "local server = tcp.server.create(8080)\nif server then\n    log.info(\"TCP server listening on port 8080\")\nend"
  },
  {
    name: "tcp.client.connect",
    category: "Network",
    signature: "tcp.client.connect(ip: string, port: number)",
    description: "Connects a TCP client to the target IP address and port. Returns a socket object on success, or nil if connection fails.",
    inputs: [
      { name: "ip", type: "string", desc: "The destination IP address (e.g. '127.0.0.1')." },
      { name: "port", type: "number", desc: "The target TCP port number." }
    ],
    outputs: [{ type: "socket", desc: "A TCP client socket object, or nil on failure." }],
    example: "local client = tcp.client.connect(\"127.0.0.1\", 8080)\nif client then\n    log.info(\"Connected to TCP server\")\nend"
  },
  {
    name: "udp.server.create",
    category: "Network",
    signature: "udp.server.create(port: number)",
    description: "Binds a UDP socket to the specified port to receive packages from any sender. Returns a socket object, or nil on failure.",
    inputs: [{ name: "port", type: "number", desc: "The UDP port number to bind to." }],
    outputs: [{ type: "socket", desc: "A UDP socket object, or nil on failure." }],
    example: "local server = udp.server.create(9090)\nif server then\n    log.info(\"UDP server bound to port 9090\")\nend"
  },
  {
    name: "udp.client.connect",
    category: "Network",
    signature: "udp.client.connect(ip: string, port: number)",
    description: "Creates a UDP client socket pre-configured to send packages to the specified destination. Returns a socket object, or nil on failure.",
    inputs: [
      { name: "ip", type: "string", desc: "The target destination IP address." },
      { name: "port", type: "number", desc: "The target UDP port number." }
    ],
    outputs: [{ type: "socket", desc: "A UDP socket object, or nil on failure." }],
    example: "local client = udp.client.connect(\"127.0.0.1\", 9090)\nif client then\n    log.info(\"UDP client connected to 127.0.0.1:9090\")\nend"
  },
  {
    name: "socket:set_timeout",
    category: "Network",
    signature: "socket:set_timeout(timeoutMs: number)",
    description: "Sets the receive, send, and connection timeouts of the socket in milliseconds.",
    inputs: [{ name: "timeoutMs", type: "number", desc: "The timeout duration in milliseconds." }],
    outputs: [],
    example: "client:set_timeout(1000) -- Set 1 second timeout"
  },
  {
    name: "socket:transmit",
    category: "Network",
    signature: "socket:transmit(bytes: table)",
    description: "Transmits a package of bytes (formatted as an array table containing byte integers 0-255) through the socket.",
    inputs: [{ name: "bytes", type: "table", desc: "An array of byte integers (0-255) to send." }],
    outputs: [],
    example: "local packet = { 72, 101, 108, 108, 111 } -- 'Hello'\nclient:transmit(packet)"
  },
  {
    name: "socket:receive",
    category: "Network",
    signature: "socket:receive()",
    description: "Receives a byte package from the network stream or socket buffer. Blocks until data arrives or the socket times out.",
    inputs: [],
    outputs: [{ type: "table", desc: "A table array containing the received byte integers (0-255), or an empty table on timeout/failure." }],
    example: "local data = client:receive()\nif #data > 0 then\n    log.info(\"Received \" .. tostring(#data) .. \" bytes\")\nend"
  },
  {
    name: "socket:has_data",
    category: "Network",
    signature: "socket:has_data()",
    description: "Checks if there are bytes available to read in the socket buffer without blocking.",
    inputs: [],
    outputs: [{ type: "boolean", desc: "True if bytes are available, otherwise False." }],
    example: "if client:has_data() then\n    local data = client:receive()\n    log.info(\"Read \" .. tostring(#data) .. \" bytes\")\nend"
  },
  {
    name: "socket:is_connected",
    category: "Network",
    signature: "socket:is_connected()",
    description: "Checks if the socket is currently connected (for TCP) or active (for UDP).",
    inputs: [],
    outputs: [{ type: "boolean", desc: "True if connected or active, otherwise False." }],
    example: "if client:is_connected() then\n    client:transmit({65, 66, 67})\nend"
  },
  {
    name: "socket:get_address",
    category: "Network",
    signature: "socket:get_address()",
    description: "Retrieves the remote IP endpoint address if connected, or the local listener address.",
    inputs: [],
    outputs: [{ type: "string", desc: "The IP address and port string (e.g. '127.0.0.1:12345')." }],
    example: "log.info(\"Socket address: \" .. client:get_address())"
  },
  {
    name: "http.get",
    category: "HTTP",
    signature: "http.get(url: string, headers: table)",
    description: "Sends an HTTP GET request to the specified URL with optional request headers.",
    inputs: [
      { name: "url", type: "string", desc: "The target URL." },
      { name: "headers", type: "table", desc: "A table of key-value pairs representing HTTP request headers (can be nil)." }
    ],
    outputs: [{ type: "table", desc: "A response table containing 'status' (number), 'body' (string), 'headers' (table), and optionally 'error' (string)." }],
    example: "local res = http.get(\"https://api.github.com/zen\", { [\"User-Agent\"] = \"NOVA\" })\nif res.status == 200 then\n    log.info(\"Zen: \" .. res.body)\nelse\n    log.error(\"Error: \" .. tostring(res.error))\nend"
  },
  {
    name: "http.post",
    category: "HTTP",
    signature: "http.post(url: string, body: string, headers: table)",
    description: "Sends an HTTP POST request to the specified URL with a body and optional request headers.",
    inputs: [
      { name: "url", type: "string", desc: "The target URL." },
      { name: "body", type: "string", desc: "The request body payload string." },
      { name: "headers", type: "table", desc: "A table of key-value pairs representing HTTP request headers (can be nil)." }
    ],
    outputs: [{ type: "table", desc: "A response table containing 'status' (number), 'body' (string), 'headers' (table), and optionally 'error' (string)." }],
    example: "local headers = { [\"Content-Type\"] = \"application/json\" }\nlocal body = \"{\\\"msg\\\":\\\"hello\\\"}\"\nlocal res = http.post(\"https://httpbin.org/post\", body, headers)\nlog.info(\"Post status: \" .. tostring(res.status))"
  },
  {
    name: "json.parse",
    category: "JSON",
    signature: "json.parse(str: string)",
    description: "Parses a JSON-formatted string and converts it into a corresponding Lua table or basic type.",
    inputs: [{ name: "str", type: "string", desc: "The JSON string to parse." }],
    outputs: [{ type: "table|any", desc: "The parsed Lua table/value, or nil if parsing fails." }],
    example: "local data = json.parse(\"{\\\"status\\\": \\\"ok\\\", \\\"value\\\": 123}\")\nif data then\n    log.info(\"Status: \" .. data.status .. \", Val: \" .. tostring(data.value))\nend"
  },
  {
    name: "json.stringify",
    category: "JSON",
    signature: "json.stringify(data: any)",
    description: "Serializes a Lua table, array, or basic type into a JSON-formatted string.",
    inputs: [{ name: "data", type: "any", desc: "The Lua value (table, string, number, boolean) to serialize." }],
    outputs: [{ type: "string", desc: "The serialized JSON string representation." }],
    example: "local myTable = { name = \"NOVA 2\", tags = {\"api\", \"json\"} }\nlocal jsonStr = json.stringify(myTable)\nlog.info(\"JSON: \" .. jsonStr)"
  },
  {
    name: "system.run",
    category: "System",
    signature: "system.run(command: string, args: table)",
    description: "Executes an external system command or process with arguments in a hidden shell, returning stdout and exit code.",
    inputs: [
      { name: "command", type: "string", desc: "The executable name or path (e.g. 'cmd.exe', 'python')." },
      { name: "args", type: "table", desc: "An array of argument strings to pass (can be nil)." }
    ],
    outputs: [
      { type: "string", desc: "The combined standard output and error stream from the process." },
      { type: "number", desc: "The process exit code." }
    ],
    example: "local stdout, exit_code = system.run(\"cmd.exe\", {\"/c\", \"dir\"})\nlog.info(\"Exit code: \" .. tostring(exit_code) .. \"\\nOutput: \\n\" .. stdout)"
  },
  {
    name: "cv.imencode",
    category: "OpenCV Core",
    signature: "cv.imencode(format: string, mat: Mat)",
    description: "Encodes an OpenCV Mat image into a byte array representation (Lua table of integers 0-255) in a specific file format.",
    inputs: [
      { name: "format", type: "string", desc: "The file format extension starting with dot (e.g. '.png', '.jpg')." },
      { name: "mat", type: "Mat", desc: "The OpenCV Mat image object to encode." }
    ],
    outputs: [{ type: "table", desc: "A table array of byte integers (0-255) containing the encoded image." }],
    example: "local pngBytes = cv.imencode(\".png\", myMat)\nlog.info(\"Encoded PNG size: \" .. tostring(#pngBytes) .. \" bytes\")"
  },
  {
    name: "cv.imdecode",
    category: "OpenCV Core",
    signature: "cv.imdecode(bytes: table)",
    description: "Decodes an OpenCV Mat image from a byte array representation (Lua table of integers 0-255).",
    inputs: [{ name: "bytes", type: "table", desc: "A table array of byte integers (0-255) containing the encoded image." }],
    outputs: [{ type: "Mat", desc: "The decoded OpenCV Mat image object." }],
    example: "local newMat = cv.imdecode(pngBytes)\ncv.imshow(\"Decoded Image\", newMat)"
  },
  {
    name: "ftp.upload",
    category: "FTP",
    signature: "ftp.upload(host: string, port: number, user: string, pass: string, localFile: string, remoteFile: string)",
    description: "Uploads a local file to the specified FTP server host and path.",
    inputs: [
      { name: "host", type: "string", desc: "The FTP server host IP or hostname." },
      { name: "port", type: "number", desc: "The FTP port number (use 21 for default)." },
      { name: "user", type: "string", desc: "The username for authentication." },
      { name: "pass", type: "string", desc: "The password for authentication." },
      { name: "localFile", type: "string", desc: "The absolute or relative path of the local file to upload." },
      { name: "remoteFile", type: "string", desc: "The destination file path on the FTP server." }
    ],
    outputs: [{ type: "boolean", desc: "True if the upload succeeded, otherwise False." }],
    example: "local ok = ftp.upload(\"127.0.0.1\", 21, \"admin\", \"1234\", \"save/log.txt\", \"/logs/log_backup.txt\")\nif ok then\n    log.info(\"FTP Upload Succeeded\")\nend"
  },
  {
    name: "ftp.download",
    category: "FTP",
    signature: "ftp.download(host: string, port: number, user: string, pass: string, remoteFile: string, localFile: string)",
    description: "Downloads a file from the specified FTP server to the local file path.",
    inputs: [
      { name: "host", type: "string", desc: "The FTP server host IP or hostname." },
      { name: "port", type: "number", desc: "The FTP port number (use 21 for default)." },
      { name: "user", type: "string", desc: "The username for authentication." },
      { name: "pass", type: "string", desc: "The password for authentication." },
      { name: "remoteFile", type: "string", desc: "The source file path on the FTP server." },
      { name: "localFile", type: "string", desc: "The destination path where the file will be saved locally." }
    ],
    outputs: [{ type: "boolean", desc: "True if the download succeeded, otherwise False." }],
    example: "local ok = ftp.download(\"127.0.0.1\", 21, \"admin\", \"1234\", \"/configs/app.json\", \"config/downloaded_app.json\")\nif ok then\n    log.info(\"FTP Download Succeeded\")\nend"
  },
  {
    name: "system.notify",
    category: "System",
    signature: "system.notify(title: string, message: string, type: string)",
    description: "Displays a standard Windows balloon notification from the system tray taskbar.",
    inputs: [
      { name: "title", type: "string", desc: "The title of the notification balloon." },
      { name: "message", type: "string", desc: "The main body message of the notification." },
      { name: "type", type: "string", desc: "The notification icon type: 'info', 'warning', or 'error'." }
    ],
    outputs: [],
    example: "system.notify(\"Flow Engine Alert\", \"Process finished successfully!\", \"info\")"
  },
  {
    name: "input.mouse_move",
    category: "Input",
    signature: "input.mouse_move(x: number, y: number)",
    description: "Simulates moving the mouse cursor to absolute screen coordinates (x, y).",
    inputs: [
      { name: "x", type: "number", desc: "The target X coordinate." },
      { name: "y", type: "number", desc: "The target Y coordinate." }
    ],
    outputs: [],
    example: "input.mouse_move(500, 300)"
  },
  {
    name: "input.mouse_click",
    category: "Input",
    signature: "input.mouse_click(button: string)",
    description: "Simulates a mouse click (left, right, or middle) at the current cursor position.",
    inputs: [{ name: "button", type: "string", desc: "The mouse button to click: 'left', 'right', or 'middle'." }],
    outputs: [],
    example: "input.mouse_click(\"left\")"
  },
  {
    name: "input.key_press",
    category: "Input",
    signature: "input.key_press(keyCode: number)",
    description: "Simulates a single virtual key press and release using the Windows Virtual-Key code.",
    inputs: [{ name: "keyCode", type: "number", desc: "The Virtual-Key Code integer (e.g. 13 for Enter, 27 for Escape)." }],
    outputs: [],
    example: "input.key_press(13) -- Press Enter key"
  },
  {
    name: "input.key_type",
    category: "Input",
    signature: "input.key_type(text: string)",
    description: "Simulates sequential typing of a string of unicode characters.",
    inputs: [{ name: "text", type: "string", desc: "The text string to type out." }],
    outputs: [],
    example: "input.key_type(\"Hello, NOVA!\")"
  },
  {
    name: "system.cpu_usage",
    category: "System",
    signature: "system.cpu_usage()",
    description: "Returns the current overall system CPU usage percentage.",
    inputs: [],
    outputs: [{ type: "number", desc: "The CPU usage percentage (0.0 to 100.0)." }],
    example: "local cpu = system.cpu_usage()\nlog.info(\"CPU Usage: \" .. string.format(\"%.1f%%\", cpu))"
  },
  {
    name: "system.ram_usage",
    category: "System",
    signature: "system.ram_usage()",
    description: "Returns physical memory (RAM) utilization statistics.",
    inputs: [],
    outputs: [{ type: "table", desc: "A table with keys: totalGb, availableGb, usedGb, and load (percentage)." }],
    example: "local ram = system.ram_usage()\nlog.info(string.format(\"RAM: %.1f / %.1f GB (%.1f%%)\", ram.usedGb, ram.totalGb, ram.load))"
  },
  {
    name: "system.disk_free",
    category: "System",
    signature: "system.disk_free(drive: string)",
    description: "Returns the available free space on the specified disk partition in gigabytes (GB).",
    inputs: [{ name: "drive", type: "string", desc: "The drive letter or root path (e.g. 'C:', 'D:/')." }],
    outputs: [{ type: "number", desc: "The free space in GB." }],
    example: "local freeGb = system.disk_free(\"C:\")\nlog.info(\"C: Free Space: \" .. string.format(\"%.1f GB\", freeGb))"
  },
  {
    name: "system.speak",
    category: "System",
    signature: "system.speak(text: string)",
    description: "Speaks the specified message using the system's default text-to-speech (TTS) voice.",
    inputs: [{ name: "text", type: "string", desc: "The text message to synthesize." }],
    outputs: [],
    example: "system.speak(\"System resources warning. CPU usage is too high.\")"
  },
  {
    name: "crypto.sha256",
    category: "Cryptography",
    signature: "crypto.sha256(str: string)",
    description: "Computes the SHA-256 cryptographic hash of the input string, returning it as a hexadecimal string.",
    inputs: [{ name: "str", type: "string", desc: "The input string to hash." }],
    outputs: [{ type: "string", desc: "The calculated SHA-256 hash in hexadecimal." }],
    example: "local hash = crypto.sha256(\"admin123\")\nlog.info(\"SHA256: \" .. hash)"
  },
  {
    name: "crypto.md5",
    category: "Cryptography",
    signature: "crypto.md5(str: string)",
    description: "Computes the MD5 cryptographic hash of the input string, returning it as a hexadecimal string.",
    inputs: [{ name: "str", type: "string", desc: "The input string to hash." }],
    outputs: [{ type: "string", desc: "The calculated MD5 hash in hexadecimal." }],
    example: "local hash = crypto.md5(\"hello\")\nlog.info(\"MD5: \" .. hash)"
  },
  {
    name: "crypto.base64_encode",
    category: "Cryptography",
    signature: "crypto.base64_encode(str: string)",
    description: "Encodes a plain text string into a Base64-encoded string.",
    inputs: [{ name: "str", type: "string", desc: "The plain text string to encode." }],
    outputs: [{ type: "string", desc: "The encoded Base64 representation." }],
    example: "local encoded = crypto.base64_encode(\"NOVA Engine\")\nlog.info(\"Base64: \" .. encoded)"
  },
  {
    name: "crypto.base64_decode",
    category: "Cryptography",
    signature: "crypto.base64_decode(str: string)",
    description: "Decodes a Base64-encoded string back into its original plain text representation.",
    inputs: [{ name: "str", type: "string", desc: "The Base64 string to decode." }],
    outputs: [{ type: "string", desc: "The decoded plain text." }],
    example: "local decoded = crypto.base64_decode(\"Tk9WQSBFbmdpbmU=\")\nlog.info(\"Decoded: \" .. decoded)"
  },
  {
    name: "csv.read",
    category: "CSV",
    signature: "csv.read(filePath: string)",
    description: "Reads a CSV spreadsheet file and parses it into a 2D Lua table array (rows and columns). Supports double-quoted values containing commas.",
    inputs: [{ name: "filePath", type: "string", desc: "The absolute or relative path to the CSV file." }],
    outputs: [{ type: "table", desc: "A 2D array representing rows containing columns." }],
    example: "local data = csv.read(\"save/results.csv\")\nif #data > 0 then\n    log.info(\"First Row, First Col: \" .. tostring(data[1][1]))\nend"
  },
  {
    name: "csv.write",
    category: "CSV",
    signature: "csv.write(filePath: string, dataTable: table)",
    description: "Writes a 2D Lua table array of rows and columns to a CSV file. Automatically quotes values containing commas or quotes.",
    inputs: [
      { name: "filePath", type: "string", desc: "The destination path where the CSV will be saved." },
      { name: "dataTable", type: "table", desc: "A 2D array of strings/numbers to write." }
    ],
    outputs: [{ type: "boolean", desc: "True if writing succeeded, otherwise False." }],
    example: "local myData = {\n  {\"Name\", \"Score\", \"Passed\"},\n  {\"Alice\", 95, \"true\"},\n  {\"Bob\", 82, \"false\"}\n}\nlocal success = csv.write(\"save/output.csv\", myData)\nif success then\n    log.info(\"CSV written successfully!\")\nend"
  },
  {
    name: "http.download",
    category: "HTTP",
    signature: "http.download(url: string, destPath: string)",
    description: "Downloads a file from the specified URL and saves it directly to the local destination path.",
    inputs: [
      { name: "url", type: "string", desc: "The web address of the file to download." },
      { name: "destPath", type: "string", desc: "The local destination path to save the file." }
    ],
    outputs: [{ type: "boolean", desc: "True if downloading succeeded, otherwise False." }],
    example: "local ok = http.download(\"https://picsum.photos/200/300\", \"save/random.jpg\")\nif ok then\n    log.info(\"Download succeeded and image saved!\")\nend"
  },
  {
    name: "gui.dialog.create",
    category: "GUI",
    signature: "gui.dialog.create(name: string)",
    description: "Creates a new custom GUI dialog layout canvas. The created dialog tab is dockable (can be dragged, split, and merged) just like cv.imshow windows. If a dialog with the same unique name already exists, this does nothing.\n\n### Suffix Masking (## Convention)\nIf the dialog name contains '##', the part after '##' serves as a unique identifier key in the backend, while only the part before '##' is displayed as the header title on the tab or floating window. This enables creating multiple separate dialogs with the same visible title.",
    inputs: [{ name: "name", type: "string", desc: "The unique identifier name of the dialog (e.g. 'Control Panel##dlg1')." }],
    outputs: [],
    example: "-- Create two unique dialogs with the same visible header 'Settings'\ngui.dialog.create(\"Settings##dlg1\")\ngui.dialog.create(\"Settings##dlg2\")"
  },
  {
    name: "gui.dialog.show",
    category: "GUI",
    signature: "gui.dialog.show(name: string, visible: boolean)",
    description: "Shows or hides the specified GUI dialog in the layout workspace. When shown, if the dialog is not currently docked anywhere, it is displayed in a new floating window. If it is already docked, the system shifts focus to its tab header.",
    inputs: [
      { name: "name", type: "string", desc: "The unique identifier name of the dialog." },
      { name: "visible", type: "boolean", desc: "True to show/focus, False to hide/close the dialog." }
    ],
    outputs: [],
    example: "local dlg = \"Dashboard##main\"\ngui.dialog.create(dlg)\ngui.dialog.show(dlg, true) -- Show dialog\n\n-- Hide dialog later\ngui.dialog.show(dlg, false)"
  },
  {
    name: "gui.widget.create",
    category: "GUI",
    signature: "gui.widget.create(name: string, type: string, parent: string)",
    description: "Creates a dynamic visual widget under a parent dialog, panel, or plot. Suffix masking (`##` convention) hides the unique trailing identifier.\n\n### Available Widget Types:\n- **panel**: A container box hosting other child widgets.\n- **button** / **label**: Interactive buttons and text labels.\n- **slider** / **progress**: Adjustable range bar and completion status.\n- **checkbox** / **radiobutton** / **dropdown** / **textinput** / **textarea**: Standard forms and multi-line text inputs.\n- **image**: Displays real-time matrix images (`cv.Mat`).\n- **plot2d** / **plot3d**: Graph canvases. Can host child `plotline` widgets.\n- **plotline**: A data series widget nested under `plot2d` or `plot3d`.\n- **colorpicker**: Button opening an RGB color selection window.",
    inputs: [
      { name: "name", type: "string", desc: "Unique identifier name of the widget (e.g. 'btnStart##btn')." },
      { name: "type", type: "string", desc: "One of: 'panel', 'button', 'label', 'slider', 'checkbox', 'radiobutton', 'dropdown', 'textinput', 'textarea', 'image', 'plot2d', 'plot3d', 'progress', 'colorpicker', 'plotline'." },
      { name: "parent", type: "string", desc: "Unique name of the parent dialog or parent panel widget." }
    ],
    outputs: [],
    example: "local dlg = \"Monitor##dlg\"\ngui.dialog.create(dlg)\n\n-- Create a panel child under the dialog\ngui.widget.create(\"myPanel##pn\", \"panel\", dlg)\n\n-- Create a button child under the panel container\ngui.widget.create(\"btnRun##btn\", \"button\", \"myPanel##pn\")"
  },
  {
    name: "gui.config.set",
    category: "GUI",
    signature: "gui.config.set(name: string, type: string, key: string, value: any)",
    description: "Sets properties, datasets, and callbacks for widgets or dialogs.\n\n### Configuration Keys:\n- **size** (`table`): Dimensions `{width, height}` in pixels.\n- **pos** (`table`): Coordinates `{x, y}` relative to parent.\n- **foreground_color** (`table`): Color `{r, g, b, a}`. Customizes series color on `plotline`.\n- **background_color** (`table`): Background color `{r, g, b, a}`.\n- **label** (`string`): Display text for buttons, labels, checkboxes, radiobuttons.\n- **horizontal** (`boolean`): Arranges panel children horizontally.\n- **legend** (`string`): Legend name for plots or specific `plotline` series.\n- **legend_text_color** (`table`): Legend text color `{r, g, b, a}` for `plot2d`/`plot3d`.\n- **plot_type** (`string`): Series plot style for `plotline` ('line', 'scatter', 'bar').\n- **title** (`string`): Plot title text.\n- **title_font_size** (`number`): Title font size (default 12).\n- **title_color** / **title_background_color** (`table`): Title text/background colors `{r, g, b, a}`.\n- **title_visible** (`boolean`): Toggle title visibility.\n- **grid_visible_x** / **grid_visible_y** / **grid_visible_z** (`boolean`): Toggle grid lines per axis.\n- **grid_interval_x** / **grid_interval_y** / **grid_interval_z** (`number`): Grid division count or spacing.\n- **grid_color_x** / **grid_color_y** / **grid_color_z** (`table`): Grid colors per axis `{r, g, b, a}`.\n- **grid_thickness_x** / **grid_thickness_y** / **grid_thickness_z** (`number`): Grid line thickness.\n- **line_thickness** (`number`) / **line_style** (`string`): Line thickness and style ('solid', 'dashed', 'dotted') for `plotline`.\n- **marker_color** (`table`) / **marker_size** (`number`) / **marker_style** (`string`): Scatter plot markers ('circle', 'square', 'triangle') for `plotline`.\n- **bar_color** (`table`) / **bar_width** (`number`) / **bar_style** (`string`): Bar chart options ('solid', 'gradient') for `plotline`.\n- **data_x** / **data_y** / **data_z** (`table`): Explicit 1D coordinates for `plotline` lines or scatter series.\n\n### Callback Functions:\n- **onclick** / **onhover** / **ondoubleclick**: Triggers on interaction.\n- **onchanged**: Triggers when value changes (for slider, checkbox, radiobutton, dropdown, textinput, textarea, colorpicker).\n\n### Data Inputs (`data` key):\n- **slider** / **progress**: Numerical value.\n- **checkbox** / **radiobutton**: Boolean (`true`/`false`).\n- **dropdown**: Selection index (1-based).\n- **textinput** / **textarea**: String text.\n- **colorpicker**: Color table `{r, g, b, a}`.\n- **image**: A `cv.Mat` object to draw in real-time.\n- **plot2d** / **plotline** (under plot2d): Array of Y-values or structured coordinates `{x={...}, y={...}}`.\n- **plot3d** / **plotline** (under plot3d): 2D grid heights `{{z11, z12, ...}, ...}` or structured coordinates `{x={...}, y={...}, z={...}}`.",
    inputs: [
      { name: "name", type: "string", desc: "The widget or dialog unique identifier name." },
      { name: "type", type: "string", desc: "Type of the target (e.g. 'dialog', 'button', 'slider', 'colorpicker', 'plotline', 'radiobutton', 'textarea')." },
      { name: "key", type: "string", desc: "Property key to modify (e.g., 'size', 'pos', 'onclick', 'data', 'background_color', 'plot_type')." },
      { name: "value", type: "any", desc: "Value matching key requirements (table, function, number, string, boolean, cv.Mat)." }
    ],
    outputs: [],
    example: "-- 1. Create and customize a dialog with custom background\nlocal dlg = \"MyDashboard##1\"\ngui.dialog.create(dlg)\ngui.config.set(dlg, \"dialog\", \"size\", {500, 400})\ngui.config.set(dlg, \"dialog\", \"background_color\", {30, 30, 46, 255})\ngui.dialog.show(dlg, true)\n\n-- 2. Create parent panel\ngui.widget.create(\"panel1##pn\", \"panel\", dlg)\ngui.config.set(\"panel1##pn\", \"panel\", \"pos\", {10, 10})\ngui.config.set(\"panel1##pn\", \"panel\", \"size\", {480, 380})\n\n-- 3. Setup bidirectional slider & textinput sync\ngui.widget.create(\"mySlider##sl\", \"slider\", \"panel1##pn\")\ngui.config.set(\"mySlider##sl\", \"slider\", \"pos\", {10, 40})\ngui.config.set(\"mySlider##sl\", \"slider\", \"range\", {0, 100})\ngui.config.set(\"mySlider##sl\", \"slider\", \"data\", 50)\n\ngui.widget.create(\"myInput##txt\", \"textinput\", \"panel1##pn\")\ngui.config.set(\"myInput##txt\", \"textinput\", \"pos\", {150, 40})\ngui.config.set(\"myInput##txt\", \"textinput\", \"data\", \"50\")\n\ngui.config.set(\"mySlider##sl\", \"slider\", \"onchanged\", function(val)\n    gui.config.set(\"myInput##txt\", \"textinput\", \"data\", tostring(val))\nend)\n\ngui.config.set(\"myInput##txt\", \"textinput\", \"onchanged\", function(val)\n    local num = tonumber(val)\n    if num and num >= 0 and num <= 100 then\n        gui.config.set(\"mySlider##sl\", \"slider\", \"data\", num)\n    end\nend)\n\n-- 4. ColorPicker changing dialog background in real-time\ngui.widget.create(\"myPicker##cp\", \"colorpicker\", \"panel1##pn\")\ngui.config.set(\"myPicker##cp\", \"colorpicker\", \"pos\", {10, 80})\ngui.config.set(\"myPicker##cp\", \"colorpicker\", \"onchanged\", function(color)\n    gui.config.set(dlg, \"dialog\", \"background_color\", color)\nend)"
  }
];

function parseInlineStyles(text: string): React.ReactNode[] | string {
  const parts: React.ReactNode[] = [];
  let currentIdx = 0;
  const regex = /(\*\*.*?\*\*|`.*?`)/g;
  let match;
  
  while ((match = regex.exec(text)) !== null) {
    const matchStr = match[0];
    const matchIdx = match.index;
    
    if (matchIdx > currentIdx) {
      parts.push(text.substring(currentIdx, matchIdx));
    }
    
    if (matchStr.startsWith('**') && matchStr.endsWith('**')) {
      parts.push(
        <strong key={matchIdx} style={{ color: 'var(--text-color)', fontWeight: 600 }}>
          {matchStr.slice(2, -2)}
        </strong>
      );
    } else if (matchStr.startsWith('`') && matchStr.endsWith('`')) {
      parts.push(
        <code key={matchIdx} style={{ 
          fontFamily: 'JetBrains Mono, monospace', 
          backgroundColor: 'color-mix(in srgb, var(--primary-color) 10%, transparent)', 
          padding: '2px 4px', 
          borderRadius: '4px',
          fontSize: '0.75rem',
          color: 'var(--warning-color)'
        }}>
          {matchStr.slice(1, -1)}
        </code>
      );
    }
    
    currentIdx = regex.lastIndex;
  }
  
  if (currentIdx < text.length) {
    parts.push(text.substring(currentIdx));
  }
  
  return parts.length > 0 ? parts : text;
}

function renderFormattedDescription(description: string) {
  if (!description) return null;
  const lines = description.split('\n');
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
      {lines.map((line, idx) => {
        const trimmed = line.trim();
        if (trimmed.startsWith('### ')) {
          return (
            <h4 key={idx} style={{ 
              fontSize: '0.9rem', 
              fontWeight: 600, 
              color: 'var(--primary-color)', 
              marginTop: '12px', 
              marginBottom: '4px',
              borderBottom: '1px solid var(--border-color)',
              paddingBottom: '4px'
            }}>
              {trimmed.substring(4)}
            </h4>
          );
        }
        if (trimmed.startsWith('- ')) {
          const content = trimmed.substring(2);
          const boldMatch = content.match(/^\*\*(.*?)\*\*(.*)$/);
          if (boldMatch) {
            const [, boldText, rest] = boldMatch;
            return (
              <div key={idx} style={{ fontSize: '0.8rem', paddingLeft: '8px', lineHeight: 1.4, display: 'flex', alignItems: 'flex-start', gap: '4px' }}>
                <span style={{ color: 'var(--primary-color)', fontSize: '0.75rem', marginTop: '2px' }}>•</span>
                <div>
                  <strong style={{ color: 'var(--text-color)', fontWeight: 600 }}>{boldText}</strong>
                  {parseInlineStyles(rest)}
                </div>
              </div>
            );
          }
          return (
            <div key={idx} style={{ fontSize: '0.8rem', paddingLeft: '8px', lineHeight: 1.4, display: 'flex', alignItems: 'flex-start', gap: '4px' }}>
              <span style={{ color: 'var(--primary-color)', fontSize: '0.75rem', marginTop: '2px' }}>•</span>
              <span style={{ color: 'var(--text-color)' }}>{parseInlineStyles(content)}</span>
            </div>
          );
        }
        
        if (trimmed === '') {
          return <div key={idx} style={{ height: '4px' }} />;
        }
        
        return (
          <p key={idx} style={{ fontSize: '0.82rem', margin: 0, color: 'var(--text-color)', lineHeight: 1.45 }}>
            {parseInlineStyles(trimmed)}
          </p>
        );
      })}
    </div>
  );
}

interface ApiDocCardProps {
  doc: ApiDoc;
  apiSearchQuery: string;
  copiedName: string | null;
  handleCopy: (name: string, text: string) => void;
}

function ApiDocCard({ doc, apiSearchQuery, copiedName, handleCopy }: ApiDocCardProps) {
  const [isOpen, setIsOpen] = useState(false);

  // Auto-expand when a search query is active
  useEffect(() => {
    if (apiSearchQuery.trim() !== '') {
      setIsOpen(true);
    } else {
      setIsOpen(false);
    }
  }, [apiSearchQuery]);

  return (
    <div
      style={{
        backgroundColor: 'var(--panel-bg)',
        border: '1px solid var(--border-color)',
        borderRadius: '8px',
        display: 'flex',
        flexDirection: 'column',
        transition: 'all 0.2s ease',
        boxShadow: '0 4px 12px rgba(0,0,0,0.1)',
        overflow: 'hidden',
        flexShrink: 0
      }}
      onMouseEnter={(e) => e.currentTarget.style.borderColor = 'var(--accent-color)'}
      onMouseLeave={(e) => e.currentTarget.style.borderColor = 'var(--border-color)'}
    >
      {/* Header (always visible, clickable to toggle) */}
      <div
        onClick={() => setIsOpen(!isOpen)}
        style={{
          padding: '14px 16px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          cursor: 'pointer',
          userSelect: 'none',
          backgroundColor: isOpen ? 'color-mix(in srgb, var(--accent-color) 4%, transparent)' : 'transparent',
          transition: 'background-color 0.2s'
        }}
      >
        <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', flex: 1, minWidth: 0, paddingRight: '12px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
            <span style={{ fontFamily: 'JetBrains Mono, monospace', fontSize: '0.95rem', fontWeight: 700, color: 'var(--info-color)' }}>
              {doc.name}
            </span>
            <span style={{
              fontSize: '0.7rem',
              backgroundColor: 'color-mix(in srgb, var(--accent-color) 15%, transparent)',
              color: 'var(--accent-color)',
              padding: '1px 6px',
              borderRadius: '10px',
              fontWeight: 600,
              border: '1px solid color-mix(in srgb, var(--accent-color) 25%, transparent)'
            }}>
              {doc.category}
            </span>
          </div>
          <span style={{
            fontFamily: 'JetBrains Mono, monospace',
            fontSize: '0.75rem',
            color: 'var(--text-muted)',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap'
          }}>
            {doc.signature}
          </span>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          {/* Copy button in header for quick access */}
          <button
            onClick={(e) => {
              e.stopPropagation(); // don't toggle open/close
              handleCopy(doc.name, doc.example);
            }}
            style={{
              background: 'none',
              border: 'none',
              color: copiedName === doc.name ? 'var(--success-color)' : 'var(--text-muted)',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '4px',
              fontSize: '0.75rem',
              padding: '4px 8px',
              borderRadius: '4px',
              transition: 'all 0.2s',
              backgroundColor: copiedName === doc.name ? 'color-mix(in srgb, var(--success-color) 10%, transparent)' : 'transparent'
            }}
          >
            {copiedName === doc.name ? <Check size={12} /> : <Copy size={12} />}
            <span>{copiedName === doc.name ? 'Copied!' : 'Copy'}</span>
          </button>
          
          {/* Chevron icon indicating state */}
          <div style={{ color: 'var(--text-muted)', display: 'flex', alignItems: 'center', transform: isOpen ? 'rotate(90deg)' : 'rotate(0deg)', transition: 'transform 0.2s' }}>
            <ChevronRight size={16} />
          </div>
        </div>
      </div>

      {/* Body content (visible only when open) */}
      {isOpen && (
        <div style={{
          padding: '16px',
          borderTop: '1px solid var(--border-color)',
          backgroundColor: 'var(--panel-bg)',
          display: 'flex',
          flexDirection: 'column',
          gap: '12px'
        }}>
          {/* Description */}
          <div style={{ fontSize: '0.85rem', color: 'var(--text-color)', lineHeight: 1.5 }}>
            {renderFormattedDescription(doc.description)}
          </div>

          {/* Inputs/Outputs */}
          {(doc.inputs.length > 0 || doc.outputs.length > 0) && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', borderTop: '1px dashed var(--border-color)', paddingTop: '10px' }}>
              {doc.inputs.length > 0 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                  <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase' }}>Parameters</div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', paddingLeft: '8px' }}>
                    {doc.inputs.map(input => (
                      <div key={input.name} style={{ fontSize: '0.8rem', lineHeight: 1.4 }}>
                        <span style={{ fontFamily: 'JetBrains Mono, monospace', fontWeight: 600, color: 'var(--warning-color)' }}>{input.name}</span>
                        <span style={{ fontFamily: 'JetBrains Mono, monospace', color: 'var(--text-muted)', fontSize: '0.75rem' }}> ({input.type})</span>
                        <span style={{ color: 'var(--text-color)' }}>: {input.desc}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
              {doc.outputs.length > 0 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', marginTop: doc.inputs.length > 0 ? '4px' : '0' }}>
                  <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase' }}>Returns</div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', paddingLeft: '8px' }}>
                    {doc.outputs.map((output, oIdx) => (
                      <div key={oIdx} style={{ fontSize: '0.8rem', lineHeight: 1.4 }}>
                        <span style={{ fontFamily: 'JetBrains Mono, monospace', color: 'var(--text-muted)', fontSize: '0.75rem' }}>({output.type})</span>
                        <span style={{ color: 'var(--text-color)' }}> {output.desc}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Example */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', borderTop: '1px dashed var(--border-color)', paddingTop: '10px' }}>
            <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase' }}>Usage Example</div>
            <pre style={{
              margin: 0,
              fontFamily: 'JetBrains Mono, monospace',
              fontSize: '0.75rem',
              backgroundColor: 'var(--sidebar-bg)',
              padding: '10px 12px',
              borderRadius: '6px',
              border: '1px solid var(--border-color)',
              color: 'var(--text-color)',
              overflowX: 'auto',
              maxWidth: '100%',
              lineHeight: 1.4
            }}>{doc.example}</pre>
          </div>
        </div>
      )}
    </div>
  );
}

export default function App() {
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [nodeLibrary, setNodeLibrary] = useState<any[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [activeModal, setActiveModal] = useState<{ nodeId: string; data: CustomNodeData } | null>(null);
  const [projectName, setProjectName] = useState<string>("New NOVA Project");
  const [nodeOutputs, setNodeOutputs] = useState<Record<string, Record<string, any>>>({});
  const [activeMenu, setActiveMenu] = useState<string | null>(null);
  const [showAboutModal, setShowAboutModal] = useState<boolean>(false);
  const [showResourceModal, setShowResourceModal] = useState<boolean>(false);
  const [showApiHelpModal, setShowApiHelpModal] = useState<boolean>(false);
  const [selectedApiCategory, setSelectedApiCategory] = useState<string>("All");
  const [apiSearchQuery, setApiSearchQuery] = useState<string>("");
  const [copiedName, setCopiedName] = useState<string | null>(null);

  const handleCopy = (name: string, text: string) => {
    navigator.clipboard.writeText(text);
    setCopiedName(name);
    setTimeout(() => setCopiedName(null), 1500);
  };
  const [resourceHistory, setResourceHistory] = useState<{ cpu: number[]; gpu: number[]; memory: number[] }>({
    cpu: [],
    gpu: [],
    memory: []
  });
  const [resourceDetails, setResourceDetails] = useState<any>(null);
  
  const [reactFlowInstance, setReactFlowInstance] = useState<ReactFlowInstance | null>(null);

  const [showSidebar, setShowSidebar] = useState<boolean>(true);
  const [showConsole, setShowConsole] = useState<boolean>(true);
  const [sidebarWidth, setSidebarWidth] = useState<number>(260);
  const [consoleHeight, setConsoleHeight] = useState<number>(220);

  const [activeTheme, setActiveTheme] = useState<AppTheme>(() => {
    const saved = localStorage.getItem('nova-active-theme');
    if (saved) {
      try {
        return JSON.parse(saved);
      } catch (e) {
        console.error(e);
      }
    }
    return THEME_PRESETS["Catppuccin Mocha"];
  });
  const [showThemeModal, setShowThemeModal] = useState<boolean>(false);

  useEffect(() => {
    const root = document.documentElement;
    root.style.setProperty('--bg-color', activeTheme.bgColor);
    root.style.setProperty('--panel-bg', activeTheme.panelBg);
    root.style.setProperty('--sidebar-bg', activeTheme.sidebarBg);
    root.style.setProperty('--border-color', activeTheme.borderColor);
    root.style.setProperty('--text-color', activeTheme.textColor);
    root.style.setProperty('--text-muted', activeTheme.textMuted);
    root.style.setProperty('--accent-color', activeTheme.accentColor);
    root.style.setProperty('--success-color', activeTheme.successColor);
    root.style.setProperty('--error-color', activeTheme.errorColor);
    root.style.setProperty('--info-color', activeTheme.infoColor);
    root.style.setProperty('--warning-color', activeTheme.warningColor);
    
    root.style.setProperty('--title-bar-bg', activeTheme.titleBarBg);
    root.style.setProperty('--title-bar-fg', activeTheme.titleBarFg);
    root.style.setProperty('--sidebar-node-bg', activeTheme.sidebarNodeBg);
    root.style.setProperty('--sidebar-node-fg', activeTheme.sidebarNodeFg);

    // Dialog & Menu Colors
    root.style.setProperty('--dialog-header-bg', activeTheme.dialogHeaderBg);
    root.style.setProperty('--dialog-header-fg', activeTheme.dialogHeaderFg);
    root.style.setProperty('--menu-bg', activeTheme.menuBg);
    root.style.setProperty('--menu-fg', activeTheme.menuFg);

    // Advanced Node Details
    root.style.setProperty('--node-border', activeTheme.nodeBorder);
    root.style.setProperty('--node-header-bg', activeTheme.nodeHeaderBg);
    root.style.setProperty('--node-header-fg', activeTheme.nodeHeaderFg);
    root.style.setProperty('--node-pin-fg', activeTheme.nodePinFg);
    root.style.setProperty('--node-pin-type-fg', activeTheme.nodePinTypeFg);
    root.style.setProperty('--node-handle-bg', activeTheme.nodeHandleBg);
    root.style.setProperty('--node-handle-border', activeTheme.nodeHandleBorder);
    root.style.setProperty('--node-handle-flow-bg', activeTheme.nodeHandleFlowBg);
    root.style.setProperty('--node-edge-idle', activeTheme.nodeEdgeIdle);
    root.style.setProperty('--node-edge-active', activeTheme.nodeEdgeActive);
    root.style.setProperty('--node-input-bg', activeTheme.nodeInputBg);
    root.style.setProperty('--node-input-fg', activeTheme.nodeInputFg);
    root.style.setProperty('--node-error-color', activeTheme.nodeErrorColor);

    // Animations & Particles
    root.style.setProperty('--anim-running-glow', activeTheme.animRunningGlow);
    root.style.setProperty('--anim-running-wave', activeTheme.animRunningWave);
    root.style.setProperty('--anim-click-particle', activeTheme.animClickParticle);

    // Syntax Highlighting
    root.style.setProperty('--syntax-comment', activeTheme.syntaxComment);
    root.style.setProperty('--syntax-keyword', activeTheme.syntaxKeyword);
    root.style.setProperty('--syntax-string', activeTheme.syntaxString);
    root.style.setProperty('--syntax-number', activeTheme.syntaxNumber);
    root.style.setProperty('--syntax-function', activeTheme.syntaxFunction);
    root.style.setProperty('--syntax-type', activeTheme.syntaxType);
    root.style.setProperty('--syntax-operator', activeTheme.syntaxOperator);

    root.style.setProperty('--base-font-size', `${activeTheme.baseFontSize}px`);
    root.style.setProperty('--menu-height', `${activeTheme.menuHeight}px`);
    root.style.setProperty('--border-radius', `${activeTheme.borderRadius}px`);
    root.style.setProperty('--node-width', `${activeTheme.nodeWidth}px`);

    localStorage.setItem('nova-active-theme', JSON.stringify(activeTheme));

    // Notify C# Host of Title Bar Theme Changes
    bridge.sendRequest('THEME_CHANGED', {
      titleBarBg: activeTheme.titleBarBg,
      titleBarFg: activeTheme.titleBarFg,
      borderColor: activeTheme.borderColor,
      panelBg: activeTheme.panelBg,
      dialogHeaderBg: activeTheme.dialogHeaderBg,
      dialogHeaderFg: activeTheme.dialogHeaderFg,
      textMuted: activeTheme.textMuted
    }).catch(err => console.error("Failed to notify WPF host of theme change:", err));
  }, [activeTheme]);

  const handleSidebarMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    const startX = e.clientX;
    const startWidth = sidebarWidth;

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const deltaX = moveEvent.clientX - startX;
      const newWidth = Math.max(180, Math.min(500, startWidth + deltaX));
      setSidebarWidth(newWidth);
    };

    const handleMouseUp = () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  const handleConsoleMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    const startY = e.clientY;
    const startHeight = consoleHeight;

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const deltaY = moveEvent.clientY - startY;
      const newHeight = Math.max(100, Math.min(600, startHeight - deltaY));
      setConsoleHeight(newHeight);
    };

    const handleMouseUp = () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  };

  // Refs to store the latest states to prevent stale closures in custom node callbacks
  const nodesRef = useRef<Node[]>([]);
  const edgesRef = useRef<Edge[]>([]);
  const projectNameRef = useRef<string>(projectName);
  const nodeLibraryRef = useRef<any[]>([]);

  useEffect(() => {
    nodesRef.current = nodes;
  }, [nodes]);

  useEffect(() => {
    edgesRef.current = edges;
  }, [edges]);

  useEffect(() => {
    projectNameRef.current = projectName;
  }, [projectName]);

  useEffect(() => {
    nodeLibraryRef.current = nodeLibrary;
  }, [nodeLibrary]);

  // 1. Fetch Node Library from C# Backend on Mount
  useEffect(() => {
    const loadLibrary = async () => {
      try {
        addSystemLog('INFO', 'Loading node library...');
        const lib = await bridge.sendRequest('GET_NODE_LIBRARY');
        setNodeLibrary(lib);
        addSystemLog('INFO', `Successfully loaded ${lib.length} nodes from library.`);
      } catch (err: any) {
        addSystemLog('ERROR', `Failed to load node library: ${err.message}`);
      }
    };

    loadLibrary();

    // 2. Set up event listeners for real-time messages from C# host
    const handleLogPrinted = (payload: { level: 'INFO' | 'WARN' | 'ERROR'; message: string }) => {
      const isLuaPrint = payload.message.startsWith('[LUA_PRINT] ');
      const cleanMessage = isLuaPrint ? payload.message.replace('[LUA_PRINT] ', '') : payload.message;
      const source = isLuaPrint ? 'USER_LUA' : 'SYSTEM';
      
      setLogs(prev => [
        ...prev,
        {
          timestamp: new Date(),
          level: payload.level,
          message: cleanMessage,
          source: source as any
        }
      ]);
    };

    const handleNodeStateChanged = (payload: { nodeId: string; cnt: number; state: 'IDLE' | 'RUNNING' | 'ERROR' }) => {
      setNodes(prevNodes => {
        const updated = prevNodes.map(node => {
          if (node.id === payload.nodeId) {
            const data = node.data as any;
            return {
              ...node,
              data: {
                ...data,
                properties: {
                  ...data.properties,
                  cnt: payload.cnt,
                  state: payload.state
                }
              }
            };
          }
          return node;
        });
        return updateGraphFlowProperties(updated, edgesRef.current);
      });
    };

    const handleResourceUsage = (payload: any) => {
      setResourceHistory(prev => {
        const nextCpu = [...prev.cpu, payload.cpu];
        const nextGpu = [...prev.gpu, payload.gpu];
        const nextMemory = [...prev.memory, payload.memory];
        
        if (nextCpu.length > 30) nextCpu.shift();
        if (nextGpu.length > 30) nextGpu.shift();
        if (nextMemory.length > 30) nextMemory.shift();
        
        return { cpu: nextCpu, gpu: nextGpu, memory: nextMemory };
      });
      setResourceDetails(payload);
    };

    const handleLogCleared = () => {
      setLogs([]);
    };

    bridge.on('LOG_PRINTED', handleLogPrinted);
    bridge.on('LOG_CLEARED', handleLogCleared);
    bridge.on('NODE_STATE_CHANGED', handleNodeStateChanged);
    bridge.on('RESOURCE_USAGE', handleResourceUsage);

    return () => {
      bridge.off('LOG_PRINTED', handleLogPrinted);
      bridge.off('LOG_CLEARED', handleLogCleared);
      bridge.off('NODE_STATE_CHANGED', handleNodeStateChanged);
      bridge.off('RESOURCE_USAGE', handleResourceUsage);
    };
  }, [setNodes]);

  // Helper to add system log entries
  const addSystemLog = (level: 'INFO' | 'WARN' | 'ERROR', message: string) => {
    setLogs(prev => [
      ...prev,
      {
        timestamp: new Date(),
        level,
        message,
        source: 'SYSTEM'
      }
    ]);
  };

  // 3. Connection Handler (Pin logic and Type Check)
  const onConnect = useCallback((params: Connection) => {
    const currentNodes = nodesRef.current;
    
    // 4. 한 노드에서는 핀끼리 연결 안됨
    if (params.source === params.target) {
      addSystemLog('ERROR', 'Connection failed: Cannot connect pins on the same node.');
      return;
    }

    const isSourceFlow = params.sourceHandle?.startsWith('flow_') && params.sourceHandle !== 'flow_in';
    const isTargetFlow = params.targetHandle === 'flow_in';

    // 5. flow 출력핀은 flow 입력핀하고만 연결 가능
    if (isSourceFlow || isTargetFlow) {
      if (isSourceFlow && isTargetFlow) {
        // Flow sequence connection
        setEdges(eds => addEdge(params, eds));
        return;
      } else {
        addSystemLog('ERROR', 'Connection failed: Flow connection handles can only connect to other flow connection handles.');
        return;
      }
    }

    const sourceNode = currentNodes.find(n => n.id === params.source);
    const targetNode = currentNodes.find(n => n.id === params.target);
    if (!sourceNode || !targetNode) return;

    const sourceData = sourceNode.data as any;
    const targetData = targetNode.data as any;

    const outPin = sourceData.outputs?.find((p: any) => p.name === params.sourceHandle);
    const inPin = targetData.inputs?.find((p: any) => p.name === params.targetHandle);

    if (!outPin || !inPin) {
      addSystemLog('WARN', 'Invalid connection handles.');
      return;
    }

    // 2. 만약 연결하고자 하는 출력핀과 입력 핀의 타입이 다른 경우 연결 안됨
    if (outPin.type !== inPin.type) {
      addSystemLog('ERROR', `Connection failed: Pin types mismatch. Cannot connect output type '${outPin.type}' to input type '${inPin.type}'.`);
      return;
    }

    setEdges(eds => addEdge(params, eds));
    
    // Mark target pin as connected in the node data
    setNodes(nds =>
      nds.map(node => {
        if (node.id === params.target && params.targetHandle) {
          const data = node.data as any;
          const connectedInputs = data.connectedInputs || [];
          return {
            ...node,
            data: {
              ...data,
              connectedInputs: [...new Set([...connectedInputs, params.targetHandle])]
            }
          };
        }
        return node;
      })
    );
  }, [setEdges, setNodes]);

  // Clean connectedInputs state and calculate isFlowDisabled when edges change
  useEffect(() => {
    setNodes(nds => updateGraphFlowProperties(nds, edges));
  }, [edges, setNodes]);

  // Synchronize computed inputs to custom nodes reactively when nodeOutputs or edges change
  useEffect(() => {
    setNodes(nds =>
      nds.map(node => {
        const computedInputs: Record<string, any> = {};
        edges.forEach(edge => {
          if (edge.target === node.id && edge.targetHandle) {
            const sourceOutputs = nodeOutputs[edge.source];
            if (sourceOutputs && edge.sourceHandle) {
              const val = sourceOutputs[edge.sourceHandle];
              if (val !== undefined) {
                computedInputs[edge.targetHandle] = val;
              }
            }
          }
        });
        
        const oldData = node.data as any;
        if (JSON.stringify(oldData.computedInputs) !== JSON.stringify(computedInputs)) {
          return {
            ...node,
            data: {
              ...oldData,
              computedInputs
            }
          };
        }
        return node;
      })
    );
  }, [edges, nodeOutputs, setNodes]);

  // Helper to compute inputs for a specific node in modal
  const getComputedInputs = useCallback((nodeId: string) => {
    const computedInputs: Record<string, any> = {};
    edges.forEach(edge => {
      if (edge.target === nodeId && edge.targetHandle) {
        const sourceOutputs = nodeOutputs[edge.source];
        if (sourceOutputs && edge.sourceHandle) {
          const val = sourceOutputs[edge.sourceHandle];
          if (val !== undefined) {
            computedInputs[edge.targetHandle] = val;
          }
        }
      }
    });
    return computedInputs;
  }, [edges, nodeOutputs]);

  // 4. Drag & Drop Handlers from Sidebar
  const onDragStart = (event: React.DragEvent, nodeType: string) => {
    event.dataTransfer.setData('application/reactflow', nodeType);
    event.dataTransfer.effectAllowed = 'copy';
  };

  const onDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'copy';
  }, []);

  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault();

      if (!reactFlowInstance) return;

      const nodeTypeId = event.dataTransfer.getData('application/reactflow');

      // Check if the dropped element is valid
      if (typeof nodeTypeId === 'undefined' || !nodeTypeId) return;

      const template = nodeLibraryRef.current.find(n => n.id === nodeTypeId);
      if (!template) return;

      const position = reactFlowInstance.screenToFlowPosition({
        x: event.clientX,
        y: event.clientY,
      });

      const newNodeId = `node_${Math.random().toString(36).substring(2, 9)}`;
      
      const newNode: Node = {
        id: newNodeId,
        type: 'custom',
        position,
        data: {
          name: template.name,
          description: template.description,
          inputs: template.inputs,
          outputs: template.outputs,
          script: template.script,
          properties: {
            total: 1,
            cnt: 0,
            state: 'IDLE'
          },
          inputValues: {},
          connectedInputs: [],
          onRunNode: (id: string) => handleRunFlow(id),
          onStopNode: () => handleStopFlow(),
          onUpdateTotal: (id: string, total: number) => handleUpdateTotal(id, total),
          onUpdateInputValue: (id: string, pin: string, val: any) => handleUpdateInputValue(id, pin, val)
        }
      };

      setNodes(nds => nds.concat(newNode));
      addSystemLog('INFO', `Created node instance: ${template.name} (${newNodeId})`);
    },
    [reactFlowInstance, setNodes]
  );

  // 5. Node Event Handlers
  const handleUpdateTotal = (nodeId: string, total: number) => {
    setNodes(nds => {
      const updated = nds.map(node => {
        if (node.id === nodeId) {
          const data = node.data as any;
          return {
            ...node,
            data: {
              ...data,
              properties: {
                ...data.properties,
                total
              }
            }
          };
        }
        return node;
      });
      return updateGraphFlowProperties(updated, edgesRef.current);
    });
  };

  const handleUpdateInputValue = (nodeId: string, pinName: string, value: any) => {
    setNodes(nds =>
      nds.map(node => {
        if (node.id === nodeId) {
          const data = node.data as any;
          return {
            ...node,
            data: {
              ...data,
              inputValues: {
                ...data.inputValues,
                [pinName]: value
              }
            }
          };
        }
        return node;
      })
    );
  };

  const onNodeDoubleClick = useCallback((_event: React.MouseEvent, node: Node) => {
    setActiveModal({
      nodeId: node.id,
      data: node.data as any as CustomNodeData
    });
  }, []);

  const handleSaveModal = (nodeId: string, inputValues: Record<string, any>, total: number) => {
    setNodes(nds => {
      const updated = nds.map(node => {
        if (node.id === nodeId) {
          const data = node.data as any;
          return {
            ...node,
            data: {
              ...data,
              inputValues,
              properties: {
                ...data.properties,
                total
              }
            }
          };
        }
        return node;
      });
      return updateGraphFlowProperties(updated, edgesRef.current);
    });
    setActiveModal(null);
    addSystemLog('INFO', `Updated node properties for: ${nodeId}`);
  };

  // 6. Flow Running Triggers
  const handleRunFlow = async (startNodeId?: string) => {
    try {
      const currentNodes = nodesRef.current;
      const currentEdges = edgesRef.current;
      const currentProjectName = projectNameRef.current;

      const graphPayload = {
        projectName: currentProjectName,
        startNodeId: startNodeId || null,
        nodes: currentNodes.map(n => {
          const d = n.data as any;
          return {
            id: n.id,
            type: d.name,
            properties: {
              total: d.properties.total,
              cnt: d.properties.cnt
            },
            inputs: d.inputValues,
            script: d.script
          };
        }),
        links: currentEdges.map(e => ({
          fromNode: e.source,
          fromOutput: e.sourceHandle,
          toNode: e.target,
          toInput: e.targetHandle
        }))
      };

      addSystemLog('INFO', startNodeId ? `Triggered execution starting from node: ${startNodeId}` : 'Triggered full flow execution.');
      
      // Reset running state of nodes before starting
      setNodes(nds =>
        nds.map(n => {
          const d = n.data as any;
          return {
            ...n,
            data: {
              ...d,
              properties: {
                ...d.properties,
                cnt: 0,
                state: 'IDLE'
              }
            }
          };
        })
      );

      const res = await bridge.sendRequest('RUN_FLOW', graphPayload);
      if (res.success && res.outputs) {
        setNodeOutputs(res.outputs);
        addSystemLog('INFO', 'Outputs successfully computed and propagated.');
      }
    } catch (err: any) {
      addSystemLog('ERROR', `Execution request failed: ${err.message}`);
    }
  };

  const handleStopFlow = async () => {
    try {
      addSystemLog('INFO', 'Stopping flow execution...');
      await bridge.sendRequest('STOP_FLOW');
    } catch (err: any) {
      addSystemLog('ERROR', `Failed to stop flow: ${err.message}`);
    }
  };

  // 7. Save / Load Projects
  const handleSaveProject = async () => {
    try {
      const currentNodes = nodesRef.current;
      const currentEdges = edgesRef.current;
      const currentProjectName = projectNameRef.current;

      const projectData = {
        projectName: currentProjectName,
        nodes: currentNodes.map(n => {
          const d = n.data as any;
          return {
            id: n.id,
            type: d.name,
            position: n.position,
            properties: d.properties,
            inputs: d.inputValues,
            script: d.script
          };
        }),
        links: currentEdges.map(e => ({
          fromNode: e.source,
          fromOutput: e.sourceHandle,
          toNode: e.target,
          toInput: e.targetHandle
        }))
      };

      const res = await bridge.sendRequest('SAVE_PROJECT', projectData);
      if (res.success) {
        addSystemLog('INFO', `Project saved successfully at: ${res.filePath}`);
      } else {
        addSystemLog('INFO', 'Save operation canceled.');
      }
    } catch (err: any) {
      addSystemLog('ERROR', `Failed to save project: ${err.message}`);
    }
  };

  const handleLoadProject = async () => {
    try {
      const res = await bridge.sendRequest('LOAD_PROJECT');
      if (res.success && res.data) {
        const data = res.data;
        setProjectName(data.projectName || "Loaded Project");
        
        const currentLibrary = nodeLibraryRef.current;

        // Reconstruct nodes: CRITICAL - Only allow creation of nodes if their Lua file exists in the library!
        const loadedNodes: Node[] = (data.nodes || [])
          .filter((n: any) => currentLibrary.some(libNode => libNode.name === n.type))
          .map((n: any) => {
            const template = currentLibrary.find(libNode => libNode.name === n.type)!;
            return {
              id: n.id,
              type: 'custom',
              position: n.position || { x: 100, y: 100 },
              data: {
                name: n.type,
                description: template.description || '',
                inputs: template.inputs || [],
                outputs: template.outputs || [],
                script: n.script,
                properties: {
                  total: n.properties?.total ?? 1,
                  cnt: 0,
                  state: 'IDLE'
                },
                inputValues: n.inputs || {},
                connectedInputs: [],
                onRunNode: (id: string) => handleRunFlow(id),
                onStopNode: () => handleStopFlow(),
                onUpdateTotal: (id: string, total: number) => handleUpdateTotal(id, total),
                onUpdateInputValue: (id: string, pin: string, val: any) => handleUpdateInputValue(id, pin, val)
              }
            };
          });

        const skippedNodesCount = (data.nodes || []).length - loadedNodes.length;
        if (skippedNodesCount > 0) {
          addSystemLog('WARN', `${skippedNodesCount} node(s) were skipped because their Lua definition files (.lua) do not exist in the local nodes/ library.`);
        }

        // Reconstruct edges
        const loadedEdges: Edge[] = (data.links || []).map((l: any, i: number) => ({
          id: `edge_${i}_${l.fromNode}_${l.toNode}`,
          source: l.fromNode,
          sourceHandle: l.fromOutput,
          target: l.toNode,
          targetHandle: l.toInput
        }));

        setNodes(loadedNodes);
        setEdges(loadedEdges);
        addSystemLog('INFO', `Successfully loaded project from: ${res.filePath}`);
      } else {
        addSystemLog('INFO', 'Load operation canceled.');
      }
    } catch (err: any) {
      addSystemLog('ERROR', `Failed to load project: ${err.message}`);
    }
  };

  const handleClearProject = () => {
    if (window.confirm("Are you sure you want to clear the canvas?")) {
      setNodes([]);
      setEdges([]);
      addSystemLog('INFO', 'Canvas cleared.');
    }
  };

  const handleExitApp = () => {
    if (window.confirm("Are you sure you want to exit NOVA?")) {
      bridge.sendRequest('EXIT_APP');
    }
  };

  // Dynamically compute animated edges based on executing state of source nodes
  const animatedEdges = React.useMemo(() => {
    return edges.map(edge => {
      const sourceNode = nodes.find(n => n.id === edge.source);
      const isSourceRunning = (sourceNode?.data as any)?.properties?.state === 'RUNNING';
      
      let style: React.CSSProperties = { stroke: 'var(--node-edge-idle)', strokeWidth: 1.5 };
      if (edge.selected) {
        style = { stroke: 'var(--accent-color)', strokeWidth: 3, filter: 'drop-shadow(0 0 4px var(--accent-color))' };
      } else if (isSourceRunning) {
        style = { stroke: 'var(--node-edge-active)', strokeWidth: 3, filter: 'drop-shadow(0 0 4px var(--node-edge-active))' };
      }
      
      return {
        ...edge,
        animated: isSourceRunning,
        style
      };
    });
  }, [edges, nodes]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', width: '100%', height: '100%', overflow: 'hidden' }}>
      {/* Click overlay to close dropdowns */}
      {activeMenu && (
        <div
          style={{ position: 'fixed', top: 0, left: 0, width: '100vw', height: '100vh', zIndex: 99, background: 'transparent' }}
          onClick={() => setActiveMenu(null)}
        />
      )}

      {/* Menu Bar (Top) */}
      <div className="menu-bar" style={{ zIndex: 100 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 20 }}>
          {/* Dropdown Menu Container */}
          <div className="menu-container">
            {/* File Menu */}
            <div className="menu-item-wrapper">
              <button 
                className={`menu-trigger ${activeMenu === 'File' ? 'active' : ''}`}
                onClick={() => setActiveMenu(activeMenu === 'File' ? null : 'File')}
              >
                File
              </button>
              {activeMenu === 'File' && (
                <div className="dropdown-menu">
                  <button className="dropdown-btn" onClick={() => { handleLoadProject(); setActiveMenu(null); }}>
                    <FolderOpen size={14} /> Open Project...
                  </button>
                  <button className="dropdown-btn" onClick={() => { handleSaveProject(); setActiveMenu(null); }}>
                    <Save size={14} /> Save Project...
                  </button>
                  <div className="dropdown-divider" />
                  <button className="dropdown-btn" style={{ color: 'var(--error-color)' }} onClick={() => { handleExitApp(); setActiveMenu(null); }}>
                    <LogOut size={14} /> Exit
                  </button>
                </div>
              )}
            </div>

            {/* Edit Menu */}
            <div className="menu-item-wrapper">
              <button 
                className={`menu-trigger ${activeMenu === 'Edit' ? 'active' : ''}`}
                onClick={() => setActiveMenu(activeMenu === 'Edit' ? null : 'Edit')}
              >
                Edit
              </button>
              {activeMenu === 'Edit' && (
                <div className="dropdown-menu">
                  <button className="dropdown-btn" onClick={() => { handleClearProject(); setActiveMenu(null); }}>
                    <Trash2 size={14} /> Clear Canvas
                  </button>
                </div>
              )}
            </div>

            {/* View Menu */}
            <div className="menu-item-wrapper">
              <button 
                className={`menu-trigger ${activeMenu === 'View' ? 'active' : ''}`}
                onClick={() => setActiveMenu(activeMenu === 'View' ? null : 'View')}
              >
                View
              </button>
              {activeMenu === 'View' && (
                <div className="dropdown-menu">
                  <button className="dropdown-btn" onClick={() => { setShowSidebar(!showSidebar); setActiveMenu(null); }}>
                    <div style={{ width: 14, height: 14, display: 'flex', alignItems: 'center', justifyContent: 'center', marginRight: 6 }}>
                      {showSidebar && <Check size={12} />}
                    </div>
                    <span>Show Sidebar</span>
                  </button>
                  <button className="dropdown-btn" onClick={() => { setShowConsole(!showConsole); setActiveMenu(null); }}>
                    <div style={{ width: 14, height: 14, display: 'flex', alignItems: 'center', justifyContent: 'center', marginRight: 6 }}>
                      {showConsole && <Check size={12} />}
                    </div>
                    <span>Show Console</span>
                  </button>
                </div>
              )}
            </div>

            {/* Theme Menu */}
            <div className="menu-item-wrapper">
              <button 
                className={`menu-trigger ${activeMenu === 'Theme' ? 'active' : ''}`}
                onClick={() => setActiveMenu(activeMenu === 'Theme' ? null : 'Theme')}
              >
                Theme
              </button>
              {activeMenu === 'Theme' && (
                <div className="dropdown-menu" style={{ minWidth: '180px' }}>
                  <button className="dropdown-btn" onClick={() => { setShowThemeModal(true); setActiveMenu(null); }}>
                    <div style={{ width: 14, height: 14, display: 'flex', alignItems: 'center', justifyContent: 'center', marginRight: 6 }}>
                      {/* Spacer */}
                    </div>
                    <span>Customize Theme...</span>
                  </button>
                  <div className="dropdown-divider" />
                  {Object.keys(THEME_PRESETS).map(name => (
                    <button 
                      key={name} 
                      className="dropdown-btn" 
                      onClick={() => { setActiveTheme(THEME_PRESETS[name]); setActiveMenu(null); }}
                    >
                      <div style={{ width: 14, height: 14, display: 'flex', alignItems: 'center', justifyContent: 'center', marginRight: 6 }}>
                        {activeTheme.name === name && <Check size={12} />}
                      </div>
                      <span>{name}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Help Menu */}
            <div className="menu-item-wrapper">
              <button 
                className={`menu-trigger ${activeMenu === 'Help' ? 'active' : ''}`}
                onClick={() => setActiveMenu(activeMenu === 'Help' ? null : 'Help')}
              >
                Help
              </button>
              {activeMenu === 'Help' && (
                <div className="dropdown-menu">
                  <button className="dropdown-btn" onClick={() => { setShowResourceModal(true); setActiveMenu(null); }}>
                    <Activity size={14} /> Resource Monitor
                  </button>
                  <button className="dropdown-btn" onClick={() => { setShowApiHelpModal(true); setActiveMenu(null); }}>
                    <BookOpen size={14} /> API Reference
                  </button>
                  <button className="dropdown-btn" onClick={() => { setShowAboutModal(true); setActiveMenu(null); }}>
                    <Info size={14} /> About
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Dynamic Project Indicator */}
        <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 500, marginRight: '10px' }}>
          Project: {projectName}
        </div>
      </div>

      {/* Main Content (Middle) */}
      <div style={{ flex: 1, display: 'flex', minHeight: 0 }}>
        {/* Sidebar (Left) */}
        {showSidebar && (
          <>
            <div className="sidebar" style={{ width: `${sidebarWidth}px` }}>
              <div className="sidebar-header">Node</div>
              <div className="sidebar-content">
                {nodeLibrary.length === 0 ? (
                  <div style={{ color: 'var(--text-muted)', fontStyle: 'italic', fontSize: '0.8rem', padding: '10px' }}>
                    No nodes found. Create .lua scripts in the 'nodes' directory.
                  </div>
                ) : (
                  buildTree(nodeLibrary).map((treeNode, index) => (
                    <SidebarTreeNode
                      key={index}
                      node={treeNode}
                      onDragStart={onDragStart}
                    />
                  ))
                )}
              </div>
            </div>
            <div className="sidebar-resizer" onMouseDown={handleSidebarMouseDown} />
          </>
        )}

        {/* Canvas (Center) */}
        <div style={{ flex: 1, height: '100%', position: 'relative' }}>
          <ReactFlow
            nodes={nodes}
            edges={animatedEdges}
            onNodesChange={onNodesChange}
            onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            onInit={setReactFlowInstance}
            onDrop={onDrop}
            onDragOver={onDragOver}
            onNodeDoubleClick={onNodeDoubleClick}
            nodeTypes={nodeTypes}
            deleteKeyCode={['Backspace', 'Delete']}
            minZoom={0.05}
            maxZoom={4.0}
            defaultViewport={{ x: 100, y: 100, zoom: 0.75 }}
            fitView
            fitViewOptions={{ padding: 0.5, maxZoom: 0.8 }}
          >
            <Controls />
            <MiniMap 
              style={{ backgroundColor: activeTheme.minimapBg, border: '1px solid var(--border-color)' }}
              maskColor={activeTheme.minimapMask + "b3"}
              nodeColor={() => activeTheme.minimapNode}
            />
            <Background color="var(--border-color)" gap={16} />
          </ReactFlow>
        </div>
      </div>

      {/* Console (Bottom) */}
      {showConsole && (
        <>
          <div className="console-resizer" onMouseDown={handleConsoleMouseDown} />
          <ConsolePanel logs={logs} onClear={() => setLogs([])} style={{ height: `${consoleHeight}px` }} />
        </>
      )}

      {/* Property Modal */}
      {activeModal && (
        <PropertyModal
          nodeId={activeModal.nodeId}
          data={activeModal.data}
          outputValues={nodeOutputs[activeModal.nodeId] || {}}
          computedInputValues={getComputedInputs(activeModal.nodeId)}
          onClose={() => setActiveModal(null)}
          onSave={handleSaveModal}
        />
      )}

      {/* About Modal */}
      {showAboutModal && (
        <div className="modal-overlay" onClick={() => setShowAboutModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ width: '450px' }}>
            <div className="modal-header">
              <div className="modal-title">About NOVA</div>
              <button className="modal-close" onClick={() => setShowAboutModal(false)}>
                <X size={18} />
              </button>
            </div>
            <div className="modal-body" style={{ display: 'flex', flexDirection: 'column', gap: '16px', padding: '24px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '16px', borderBottom: '1px solid var(--border-color)', paddingBottom: '16px' }}>
                <img src="/icon_trans.png" alt="NOVA" style={{ width: '64px', height: '64px', objectFit: 'contain' }} />
                <div style={{ textAlign: 'left' }}>
                  <h3 style={{ margin: '0 0 4px 0', fontSize: '1.40rem', fontWeight: 800, color: 'var(--accent-color)', letterSpacing: '0.5px' }}>NOVA</h3>
                  <p style={{ margin: 0, fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 500 }}>Lua Visual Scripting Engine</p>
                </div>
              </div>
              
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', textAlign: 'left', fontSize: '0.85rem', color: 'var(--text-color)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-color)', paddingBottom: '6px' }}>
                  <span style={{ color: 'var(--text-muted)', fontWeight: 600 }}>App Version</span>
                  <span style={{ fontFamily: 'JetBrains Mono', color: 'var(--info-color)' }}>v1.0.0</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-color)', paddingBottom: '6px' }}>
                  <span style={{ color: 'var(--text-muted)', fontWeight: 600 }}>Execution Host</span>
                  <span style={{ color: 'var(--accent-color)' }}>.NET 8.0 (WPF Assembly)</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-color)', paddingBottom: '6px' }}>
                  <span style={{ color: 'var(--text-muted)', fontWeight: 600 }}>Web Runtime</span>
                  <span style={{ color: 'var(--accent-color)' }}>Microsoft WebView2 (Chromium)</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-color)', paddingBottom: '6px' }}>
                  <span style={{ color: 'var(--text-muted)', fontWeight: 600 }}>Core Languages</span>
                  <span style={{ color: 'var(--success-color)' }}>C# 12 / TypeScript 5 / Lua</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-color)', paddingBottom: '6px' }}>
                  <span style={{ color: 'var(--text-muted)', fontWeight: 600 }}>Web Framework</span>
                  <span style={{ color: 'var(--warning-color)' }}>React 19 / Vite 8 / @xyflow</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid var(--border-color)', paddingBottom: '6px' }}>
                  <span style={{ color: 'var(--text-muted)', fontWeight: 600 }}>Lua Interpreter</span>
                  <span style={{ color: 'var(--accent-color)' }}>MoonSharp v2.0.0</span>
                </div>
              </div>

              <div style={{ borderTop: '1px solid var(--border-color)', width: '100%', paddingTop: '12px', fontSize: '0.75rem', color: 'var(--text-muted)', textAlign: 'center' }}>
                &copy; {new Date().getFullYear()} NOVA Development Team. All rights reserved.
              </div>
            </div>
            <div className="modal-footer" style={{ padding: '8px 16px' }}>
              <button className="btn btn-primary" onClick={() => setShowAboutModal(false)}>Close</button>
            </div>
          </div>
        </div>
      )}

      {/* Resource Monitor Modal */}
      {showResourceModal && (
        <div className="modal-overlay" onClick={() => setShowResourceModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ width: '820px', maxWidth: '95vw' }}>
            <div className="modal-header">
              <div className="modal-title" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <Activity size={18} color="var(--success-color)" />
                <span>Resource Monitor</span>
              </div>
              <button className="modal-close" onClick={() => setShowResourceModal(false)}>
                <X size={18} />
              </button>
            </div>
            
            <div className="modal-body" style={{ display: 'flex', flexDirection: 'row', gap: '24px', padding: '20px', overflowX: 'hidden', maxHeight: 'none' }}>
              {/* Left Column: Graphs */}
              <div style={{ flex: '0 0 366px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                <ResourceChart
                  data={resourceHistory.cpu}
                  label="CPU Load"
                  color="var(--info-color)"
                  currentValue={resourceHistory.cpu[resourceHistory.cpu.length - 1] ?? 0}
                />
                <ResourceChart
                  data={resourceHistory.gpu}
                  label="GPU Load"
                  color="var(--success-color)"
                  currentValue={resourceHistory.gpu[resourceHistory.gpu.length - 1] ?? 0}
                />
                <ResourceChart
                  data={resourceHistory.memory}
                  label="Memory Load"
                  color="var(--warning-color)"
                  currentValue={resourceHistory.memory[resourceHistory.memory.length - 1] ?? 0}
                />
              </div>

              {/* Right Column: Detailed Metrics */}
              <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <div style={{ fontSize: '0.9rem', fontWeight: 700, color: 'var(--text-color)', borderBottom: '1px solid var(--border-color)', paddingBottom: '8px' }}>
                  System Specifications & Diagnostics
                </div>
                
                {resourceDetails ? (
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                    {/* CPU Card */}
                    <div style={{ gridColumn: 'span 2', backgroundColor: 'var(--bg-color)', padding: '10px 14px', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
                      <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', marginBottom: '4px' }}>Processor (CPU)</div>
                      <div style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-color)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={resourceDetails.cpuModel}>
                        {resourceDetails.cpuModel}
                      </div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '2px', display: 'flex', justifyContent: 'space-between' }}>
                        <span>Cores: {resourceDetails.cpuCores} Logical Cores</span>
                        <span style={{ fontWeight: 600, color: 'var(--info-color)' }}>Load: {resourceDetails.cpu.toFixed(1)}%</span>
                      </div>
                    </div>

                    {/* GPU Card */}
                    <div style={{ gridColumn: 'span 2', backgroundColor: 'var(--bg-color)', padding: '10px 14px', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
                      <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', marginBottom: '4px' }}>Graphics (GPU)</div>
                      <div style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-color)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={resourceDetails.gpuName}>
                        {resourceDetails.gpuName}
                      </div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '2px', display: 'flex', justifyContent: 'space-between' }}>
                        <span>Renderer: DX12 / OpenGL</span>
                        <span style={{ fontWeight: 600, color: 'var(--success-color)' }}>Load: {resourceDetails.gpu.toFixed(1)}%</span>
                      </div>
                    </div>

                    {/* Memory Card */}
                    <div style={{ backgroundColor: 'var(--bg-color)', padding: '10px 14px', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
                      <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', marginBottom: '4px' }}>Physical RAM</div>
                      <div style={{ fontSize: '1rem', fontWeight: 700, color: 'var(--text-color)' }}>
                        {resourceDetails.usedMemoryGb.toFixed(1)} <span style={{ fontSize: '0.8rem', fontWeight: 500, color: 'var(--text-muted)' }}>/ {resourceDetails.totalMemoryGb.toFixed(1)} GB</span>
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--warning-color)', marginTop: '4px', fontWeight: 600 }}>
                        Usage: {resourceDetails.memory.toFixed(1)}%
                      </div>
                    </div>

                    {/* Active Threads Card */}
                    <div style={{ backgroundColor: 'var(--bg-color)', padding: '10px 14px', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
                      <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', marginBottom: '4px' }}>Process Threads</div>
                      <div style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--text-color)' }}>
                        {resourceDetails.threads} <span style={{ fontSize: '0.75rem', fontWeight: 500, color: 'var(--text-muted)' }}>Active</span>
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                        Current application threads
                      </div>
                    </div>

                    {/* GC Heap Card */}
                    <div style={{ backgroundColor: 'var(--bg-color)', padding: '10px 14px', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
                      <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', marginBottom: '4px' }}>GC Managed Heap</div>
                      <div style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--accent-color)' }}>
                        {resourceDetails.heapMb.toFixed(1)} <span style={{ fontSize: '0.8rem', fontWeight: 500, color: 'var(--text-muted)' }}>MB</span>
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                        Active CLR allocated objects
                      </div>
                    </div>

                    {/* App Private Working Set */}
                    <div style={{ backgroundColor: 'var(--bg-color)', padding: '10px 14px', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
                      <div style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', marginBottom: '4px' }}>Working Set</div>
                      <div style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--info-color)' }}>
                        {resourceDetails.privateMemoryMb.toFixed(1)} <span style={{ fontSize: '0.8rem', fontWeight: 500, color: 'var(--text-muted)' }}>MB</span>
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                        Process physical memory size
                      </div>
                    </div>
                  </div>
                ) : (
                  <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '0.85rem', color: 'var(--text-muted)', fontStyle: 'italic' }}>
                    Loading specs...
                  </div>
                )}
              </div>
            </div>
            
            <div className="modal-footer" style={{ padding: '8px 16px' }}>
              <button className="btn btn-primary" onClick={() => setShowResourceModal(false)}>Close</button>
            </div>
          </div>
        </div>
      )}

      {/* Script API Help Modal */}
      {showApiHelpModal && (
        <div className="modal-overlay" onClick={() => setShowApiHelpModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ width: '1080px', maxWidth: '95vw', height: '760px', maxHeight: '90vh', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
            <div className="modal-header">
              <div className="modal-title" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <BookOpen size={18} color="var(--accent-color)" />
                <span>Script API Reference</span>
              </div>
              <button className="modal-close" onClick={() => setShowApiHelpModal(false)}>
                <X size={18} />
              </button>
            </div>
            
            <div className="modal-body" style={{ display: 'flex', flexDirection: 'row', gap: '0px', padding: '0px', overflow: 'hidden', flex: 1, maxHeight: 'none', minHeight: 0 }}>
              {/* Category Sidebar (Left) */}
              <div style={{
                width: '220px',
                flexShrink: 0,
                borderRight: '1px solid var(--border-color)',
                backgroundColor: 'var(--sidebar-bg)',
                display: 'flex',
                flexDirection: 'column',
                padding: '12px 0',
                overflowY: 'auto',
                minHeight: 0
              }}>
                <div style={{ padding: '0 16px 8px 16px', fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase' }}>Categories</div>
                {['All', 'Logging', 'Time', 'Global Memory', 'Filesystem', 'Network', 'HTTP', 'JSON', 'System', 'FTP', 'Input', 'Cryptography', 'CSV', 'OpenCV Core', 'OpenCV Processing', 'OpenCV Drawing', 'Mat Wrapper', 'GUI'].map(cat => (
                  <button
                    key={cat}
                    onClick={() => setSelectedApiCategory(cat)}
                    style={{
                      background: 'none',
                      border: 'none',
                      color: selectedApiCategory === cat ? 'var(--accent-color)' : 'var(--text-color)',
                      textAlign: 'left',
                      padding: '8px 16px',
                      fontSize: '0.85rem',
                      fontWeight: selectedApiCategory === cat ? 600 : 500,
                      cursor: 'pointer',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      backgroundColor: selectedApiCategory === cat ? 'color-mix(in srgb, var(--accent-color) 10%, transparent)' : 'transparent',
                      borderLeft: selectedApiCategory === cat ? '3px solid var(--accent-color)' : '3px solid transparent',
                      transition: 'all 0.2s ease'
                    }}
                  >
                    <span>{cat}</span>
                    <span style={{
                      fontSize: '0.75rem',
                      backgroundColor: selectedApiCategory === cat ? 'var(--accent-color)' : 'var(--border-color)',
                      color: selectedApiCategory === cat ? 'var(--bg-color)' : 'var(--text-muted)',
                      padding: '1px 6px',
                      borderRadius: '10px',
                      fontWeight: 600
                    }}>
                      {cat === 'All' 
                        ? API_DOCS.length 
                        : API_DOCS.filter(d => d.category === cat).length
                      }
                    </span>
                  </button>
                ))}
              </div>

              <div style={{ flex: 1, display: 'flex', flexDirection: 'column', backgroundColor: 'var(--bg-color)', minHeight: 0, minWidth: 0 }}>
                {/* Search Bar */}
                <div style={{
                  padding: '12px 18px',
                  borderBottom: '1px solid var(--border-color)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  backgroundColor: 'var(--panel-bg)'
                }}>
                  <Search size={16} style={{ color: 'var(--text-muted)' }} />
                  <input
                    type="text"
                    placeholder="Search functions by name or description..."
                    value={apiSearchQuery}
                    onChange={(e) => setApiSearchQuery(e.target.value)}
                    style={{
                      flex: 1,
                      background: 'transparent',
                      border: 'none',
                      color: 'var(--text-color)',
                      outline: 'none',
                      fontSize: '0.85rem'
                    }}
                  />
                  {apiSearchQuery && (
                    <button
                      onClick={() => setApiSearchQuery('')}
                      style={{
                        background: 'none',
                        border: 'none',
                        color: 'var(--text-muted)',
                        cursor: 'pointer',
                        display: 'flex',
                        alignItems: 'center'
                      }}
                    >
                      <X size={14} />
                    </button>
                  )}
                </div>

                {/* API Docs List */}
                <div style={{ flex: 1, overflowY: 'auto', padding: '18px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                  {(() => {
                    const filteredDocs = API_DOCS.filter(doc => {
                      const matchesCategory = selectedApiCategory === 'All' || doc.category === selectedApiCategory;
                      const matchesSearch = doc.name.toLowerCase().includes(apiSearchQuery.toLowerCase()) || 
                                            doc.description.toLowerCase().includes(apiSearchQuery.toLowerCase());
                      return matchesCategory && matchesSearch;
                    });

                    if (filteredDocs.length === 0) {
                      return (
                        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '200px', color: 'var(--text-muted)' }}>
                          <BookOpen size={32} style={{ marginBottom: '8px', opacity: 0.5 }} />
                          <span style={{ fontSize: '0.9rem', fontStyle: 'italic' }}>No API functions found matching "{apiSearchQuery}"</span>
                        </div>
                      );
                    }

                    return filteredDocs.map((doc) => (
                      <ApiDocCard
                        key={doc.name}
                        doc={doc}
                        apiSearchQuery={apiSearchQuery}
                        copiedName={copiedName}
                        handleCopy={handleCopy}
                      />
                    ));
                  })()}
                </div>
              </div>
            </div>
            
            <div className="modal-footer" style={{ padding: '8px 16px' }}>
              <button className="btn btn-primary" onClick={() => setShowApiHelpModal(false)}>Close</button>
            </div>
          </div>
        </div>
      )}

      {/* Theme Modal */}
      {showThemeModal && (
        <ThemeModal
          activeTheme={activeTheme}
          onChangeTheme={setActiveTheme}
          onClose={() => setShowThemeModal(false)}
        />
      )}
    </div>
  );
}
