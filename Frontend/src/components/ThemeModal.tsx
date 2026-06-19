import React, { useState, useEffect } from 'react';
import { X, Save, Trash2, Download, Upload } from 'lucide-react';
import { bridge } from '../utils/bridge';

export interface AppTheme {
  name: string;
  bgColor: string;
  panelBg: string;
  sidebarBg: string;
  borderColor: string;
  textColor: string;
  textMuted: string;
  accentColor: string;
  successColor: string;
  errorColor: string;
  infoColor: string;
  warningColor: string;
  titleBarBg: string;
  titleBarFg: string;
  sidebarNodeBg: string;
  sidebarNodeFg: string;
  minimapBg: string;
  minimapMask: string;
  minimapNode: string;
  dialogHeaderBg: string;
  dialogHeaderFg: string;
  menuBg: string;
  menuFg: string;
  nodeBorder: string;
  nodeHeaderBg: string;
  nodeHeaderFg: string;
  nodePinFg: string;
  nodePinTypeFg: string;
  nodeHandleBg: string;
  nodeHandleBorder: string;
  nodeHandleFlowBg: string;
  nodeEdgeIdle: string;
  nodeEdgeActive: string;
  nodeInputBg: string;
  nodeInputFg: string;
  nodeErrorColor: string;
  animRunningGlow: string;
  animRunningWave: string;
  animClickParticle: string;
  syntaxComment: string;
  syntaxKeyword: string;
  syntaxString: string;
  syntaxNumber: string;
  syntaxFunction: string;
  syntaxType: string;
  syntaxOperator: string;
  baseFontSize: number;
  menuHeight: number;
  borderRadius: number;
  nodeWidth: number;
}

export const THEME_PRESETS: Record<string, AppTheme> = {
  "Catppuccin Mocha": {
    name: "Catppuccin Mocha",
    bgColor: "#11111b",
    panelBg: "#1e1e2e",
    sidebarBg: "#181825",
    borderColor: "#313244",
    textColor: "#cdd6f4",
    textMuted: "#a6adc8",
    accentColor: "#cba6f7",
    successColor: "#a6e3a1",
    errorColor: "#f38ba8",
    infoColor: "#89b4fa",
    warningColor: "#f9e2af",
    titleBarBg: "#1e1e2e",
    titleBarFg: "#cdd6f4",
    sidebarNodeBg: "#1e1e2e",
    sidebarNodeFg: "#cdd6f4",
    minimapBg: "#1e1e2e",
    minimapMask: "#11111b",
    minimapNode: "#313244",
    dialogHeaderBg: "#252538",
    dialogHeaderFg: "#f5e0dc",
    menuBg: "#1e1e2e",
    menuFg: "#cdd6f4",
    nodeBorder: "#313244",
    nodeHeaderBg: "#181825",
    nodeHeaderFg: "#cdd6f4",
    nodePinFg: "#cdd6f4",
    nodePinTypeFg: "#89b4fa",
    nodeHandleBg: "#1e1e2e",
    nodeHandleBorder: "#89b4fa",
    nodeHandleFlowBg: "#cba6f7",
    nodeEdgeIdle: "#45475a",
    nodeEdgeActive: "#a6e3a1",
    nodeInputBg: "#11111b",
    nodeInputFg: "#cdd6f4",
    nodeErrorColor: "#f38ba8",
    animRunningGlow: "#a6e3a1",
    animRunningWave: "#a6e3a1",
    animClickParticle: "#cba6f7",
    syntaxComment: "#6c7086",
    syntaxKeyword: "#cba6f7",
    syntaxString: "#a6e3a1",
    syntaxNumber: "#fab387",
    syntaxFunction: "#89b4fa",
    syntaxType: "#f9e2af",
    syntaxOperator: "#f5c2e7",
    baseFontSize: 14,
    menuHeight: 32,
    borderRadius: 8,
    nodeWidth: 240
  },
  "Nord Ice": {
    name: "Nord Ice",
    bgColor: "#2e3440",
    panelBg: "#3b4252",
    sidebarBg: "#2e3440",
    borderColor: "#4c566a",
    textColor: "#eceff4",
    textMuted: "#d8dee9",
    accentColor: "#88c0d0",
    successColor: "#a3be8c",
    errorColor: "#bf616a",
    infoColor: "#81a1c1",
    warningColor: "#ebcb8b",
    titleBarBg: "#3b4252",
    titleBarFg: "#eceff4",
    sidebarNodeBg: "#3b4252",
    sidebarNodeFg: "#eceff4",
    minimapBg: "#3b4252",
    minimapMask: "#2e3440",
    minimapNode: "#4c566a",
    dialogHeaderBg: "#2e3440",
    dialogHeaderFg: "#eceff4",
    menuBg: "#3b4252",
    menuFg: "#eceff4",
    nodeBorder: "#4c566a",
    nodeHeaderBg: "#2e3440",
    nodeHeaderFg: "#eceff4",
    nodePinFg: "#d8dee9",
    nodePinTypeFg: "#81a1c1",
    nodeHandleBg: "#3b4252",
    nodeHandleBorder: "#81a1c1",
    nodeHandleFlowBg: "#88c0d0",
    nodeEdgeIdle: "#4c566a",
    nodeEdgeActive: "#a3be8c",
    nodeInputBg: "#2e3440",
    nodeInputFg: "#eceff4",
    nodeErrorColor: "#bf616a",
    animRunningGlow: "#a3be8c",
    animRunningWave: "#a3be8c",
    animClickParticle: "#88c0d0",
    syntaxComment: "#4c566a",
    syntaxKeyword: "#81a1c1",
    syntaxString: "#a3be8c",
    syntaxNumber: "#b48ead",
    syntaxFunction: "#88c0d0",
    syntaxType: "#ebcb8b",
    syntaxOperator: "#81a1c1",
    baseFontSize: 14,
    menuHeight: 32,
    borderRadius: 8,
    nodeWidth: 240
  },
  "Monokai Pro": {
    name: "Monokai Pro",
    bgColor: "#2d2a2e",
    panelBg: "#403e41",
    sidebarBg: "#2d2a2e",
    borderColor: "#5b595c",
    textColor: "#fcfcfa",
    textMuted: "#939293",
    accentColor: "#ffd866",
    successColor: "#a9dc76",
    errorColor: "#ff6188",
    infoColor: "#78dce8",
    warningColor: "#fc9867",
    titleBarBg: "#403e41",
    titleBarFg: "#fcfcfa",
    sidebarNodeBg: "#403e41",
    sidebarNodeFg: "#fcfcfa",
    minimapBg: "#403e41",
    minimapMask: "#2d2a2e",
    minimapNode: "#5b595c",
    dialogHeaderBg: "#2d2a2e",
    dialogHeaderFg: "#fcfcfa",
    menuBg: "#403e41",
    menuFg: "#fcfcfa",
    nodeBorder: "#5b595c",
    nodeHeaderBg: "#2d2a2e",
    nodeHeaderFg: "#fcfcfa",
    nodePinFg: "#fcfcfa",
    nodePinTypeFg: "#78dce8",
    nodeHandleBg: "#403e41",
    nodeHandleBorder: "#78dce8",
    nodeHandleFlowBg: "#ffd866",
    nodeEdgeIdle: "#5b595c",
    nodeEdgeActive: "#a9dc76",
    nodeInputBg: "#2d2a2e",
    nodeInputFg: "#fcfcfa",
    nodeErrorColor: "#ff6188",
    animRunningGlow: "#a9dc76",
    animRunningWave: "#a9dc76",
    animClickParticle: "#ffd866",
    syntaxComment: "#727072",
    syntaxKeyword: "#ff6188",
    syntaxString: "#ffd866",
    syntaxNumber: "#ab9df2",
    syntaxFunction: "#a9dc76",
    syntaxType: "#78dce8",
    syntaxOperator: "#fcfcfa",
    baseFontSize: 14,
    menuHeight: 32,
    borderRadius: 8,
    nodeWidth: 240
  },
  "Light Modern": {
    name: "Light Modern",
    bgColor: "#f4f4f7",
    panelBg: "#ffffff",
    sidebarBg: "#eaeaf0",
    borderColor: "#dcdce3",
    textColor: "#24292e",
    textMuted: "#586069",
    accentColor: "#0366d6",
    successColor: "#28a745",
    errorColor: "#d73a49",
    infoColor: "#005cc5",
    warningColor: "#e36209",
    titleBarBg: "#ffffff",
    titleBarFg: "#24292e",
    sidebarNodeBg: "#ffffff",
    sidebarNodeFg: "#24292e",
    minimapBg: "#ffffff",
    minimapMask: "#f4f4f7",
    minimapNode: "#dcdce3",
    dialogHeaderBg: "#eaeaf0",
    dialogHeaderFg: "#24292e",
    menuBg: "#ffffff",
    menuFg: "#24292e",
    nodeBorder: "#dcdce3",
    nodeHeaderBg: "#eaeaf0",
    nodeHeaderFg: "#24292e",
    nodePinFg: "#24292e",
    nodePinTypeFg: "#005cc5",
    nodeHandleBg: "#ffffff",
    nodeHandleBorder: "#0366d6",
    nodeHandleFlowBg: "#005cc5",
    nodeEdgeIdle: "#dcdce3",
    nodeEdgeActive: "#28a745",
    nodeInputBg: "#f4f4f7",
    nodeInputFg: "#24292e",
    nodeErrorColor: "#d73a49",
    animRunningGlow: "#28a745",
    animRunningWave: "#28a745",
    animClickParticle: "#0366d6",
    syntaxComment: "#008000",
    syntaxKeyword: "#0000ff",
    syntaxString: "#a31515",
    syntaxNumber: "#098658",
    syntaxFunction: "#795e26",
    syntaxType: "#267f99",
    syntaxOperator: "#24292e",
    baseFontSize: 14,
    menuHeight: 32,
    borderRadius: 8,
    nodeWidth: 240
  }
};

interface ThemeModalProps {
  activeTheme: AppTheme;
  onChangeTheme: (theme: AppTheme) => void;
  onClose: () => void;
}

export const ThemeModal: React.FC<ThemeModalProps> = ({ activeTheme, onChangeTheme, onClose }) => {
  const [customThemes, setCustomThemes] = useState<Record<string, AppTheme>>({});
  const [selectedPreset, setSelectedPreset] = useState<string>(activeTheme.name);
  const [customName, setCustomName] = useState<string>("");

  useEffect(() => {
    // Load custom themes from localStorage
    const saved = localStorage.getItem('nova-custom-themes');
    if (saved) {
      try {
        setCustomThemes(JSON.parse(saved));
      } catch (e) {
        console.error(e);
      }
    }
  }, []);

  const saveCustomThemes = (updated: Record<string, AppTheme>) => {
    setCustomThemes(updated);
    localStorage.setItem('nova-custom-themes', JSON.stringify(updated));
  };

  const handlePresetChange = (name: string) => {
    setSelectedPreset(name);
    if (THEME_PRESETS[name]) {
      onChangeTheme(THEME_PRESETS[name]);
    } else if (customThemes[name]) {
      onChangeTheme(customThemes[name]);
    }
  };

  const handleColorChange = (key: keyof AppTheme, value: string) => {
    const updated = { ...activeTheme, [key]: value };
    onChangeTheme(updated);
  };

  const handleSizeChange = (key: keyof AppTheme, value: number) => {
    const updated = { ...activeTheme, [key]: value };
    onChangeTheme(updated);
  };

  const handleSaveCustom = () => {
    const nameToSave = customName.trim();
    if (!nameToSave) {
      alert("Please enter a theme name.");
      return;
    }

    const updatedTheme = { ...activeTheme, name: nameToSave };
    const newCustoms = { ...customThemes, [nameToSave]: updatedTheme };
    saveCustomThemes(newCustoms);
    setSelectedPreset(nameToSave);
    onChangeTheme(updatedTheme);
    setCustomName("");
    alert(`Theme "${nameToSave}" saved successfully.`);
  };

  const handleDeleteCustom = () => {
    if (THEME_PRESETS[selectedPreset]) {
      alert("Cannot delete built-in presets.");
      return;
    }

    if (window.confirm(`Are you sure you want to delete theme "${selectedPreset}"?`)) {
      const newCustoms = { ...customThemes };
      delete newCustoms[selectedPreset];
      saveCustomThemes(newCustoms);
      
      // Fallback to default
      setSelectedPreset("Catppuccin Mocha");
      onChangeTheme(THEME_PRESETS["Catppuccin Mocha"]);
    }
  };

  const handleExportTheme = async () => {
    try {
      const res = await bridge.sendRequest('SAVE_THEME', activeTheme);
      if (res.success) {
        alert(`Theme exported successfully to:\n${res.filePath}`);
      }
    } catch (err: any) {
      alert(`Failed to export theme: ${err.message}`);
    }
  };

  const handleImportTheme = async () => {
    try {
      const res = await bridge.sendRequest('LOAD_THEME');
      if (res.success && res.data) {
        const imported = res.data as AppTheme;
        
        // Ensure imported has name
        if (!imported.name) imported.name = "Imported Theme";
        
        onChangeTheme(imported);
        
        // Save to custom themes list
        const newCustoms = { ...customThemes, [imported.name]: imported };
        saveCustomThemes(newCustoms);
        setSelectedPreset(imported.name);
        
        alert(`Theme "${imported.name}" imported successfully.`);
      }
    } catch (err: any) {
      alert(`Failed to import theme: ${err.message}`);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ width: '600px', maxWidth: '95vw' }}>
        <div className="modal-header">
          <div className="modal-title">Theme Customizer</div>
          <button className="modal-close" onClick={onClose}>
            <X size={18} />
          </button>
        </div>

        <div className="modal-body" style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxHeight: '550px' }}>
          
          {/* Preset Selection */}
          <div style={{ display: 'flex', gap: '10px', alignItems: 'flex-end', borderBottom: '1px solid var(--border-color)', paddingBottom: '12px' }}>
            <div className="modal-field" style={{ flex: 1 }}>
              <label className="modal-label">Preset / Saved Themes</label>
              <select 
                className="modal-input" 
                value={selectedPreset} 
                onChange={(e) => handlePresetChange(e.target.value)}
                style={{ backgroundColor: 'var(--bg-color)', color: 'var(--text-color)', border: '1px solid var(--border-color)' }}
              >
                <optgroup label="Presets">
                  {Object.keys(THEME_PRESETS).map(name => (
                    <option key={name} value={name}>{name}</option>
                  ))}
                </optgroup>
                {Object.keys(customThemes).length > 0 && (
                  <optgroup label="Custom Themes">
                    {Object.keys(customThemes).map(name => (
                      <option key={name} value={name}>{name}</option>
                    ))}
                  </optgroup>
                )}
              </select>
            </div>
            
            {!THEME_PRESETS[selectedPreset] && (
              <button className="btn" style={{ height: '34px', backgroundColor: 'var(--error-color)', color: '#11111b', border: 'none' }} onClick={handleDeleteCustom}>
                <Trash2 size={14} /> Delete
              </button>
            )}
          </div>

          {/* Color Adjustments Grid */}
          <div>
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem' }}>Interface Colors</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginTop: '6px' }}>
              
              {/* Background Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Canvas Background</span>
                <input type="color" value={activeTheme.bgColor} onChange={(e) => handleColorChange('bgColor', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Panel Background */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Panel Background</span>
                <input type="color" value={activeTheme.panelBg} onChange={(e) => handleColorChange('panelBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Sidebar Background */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Sidebar Background</span>
                <input type="color" value={activeTheme.sidebarBg} onChange={(e) => handleColorChange('sidebarBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Border Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Border Color</span>
                <input type="color" value={activeTheme.borderColor} onChange={(e) => handleColorChange('borderColor', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Text Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Primary Text</span>
                <input type="color" value={activeTheme.textColor} onChange={(e) => handleColorChange('textColor', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Accent Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Lavender Accent</span>
                <input type="color" value={activeTheme.accentColor} onChange={(e) => handleColorChange('accentColor', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Success Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Success (Running)</span>
                <input type="color" value={activeTheme.successColor} onChange={(e) => handleColorChange('successColor', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Error Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Error (Stop)</span>
                <input type="color" value={activeTheme.errorColor} onChange={(e) => handleColorChange('errorColor', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
            </div>

            {/* Title Bar & Sidebar Node section */}
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem', marginTop: '12px' }}>Title Bar & Node List Colors</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginTop: '6px' }}>
              
              {/* Title Bar Background */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Title Bar Bg</span>
                <input type="color" value={activeTheme.titleBarBg} onChange={(e) => handleColorChange('titleBarBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Title Bar Font Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Title Bar Font</span>
                <input type="color" value={activeTheme.titleBarFg} onChange={(e) => handleColorChange('titleBarFg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Sidebar Node Background */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Sidebar Node Bg</span>
                <input type="color" value={activeTheme.sidebarNodeBg} onChange={(e) => handleColorChange('sidebarNodeBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Sidebar Node Font Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Sidebar Node Font</span>
                <input type="color" value={activeTheme.sidebarNodeFg} onChange={(e) => handleColorChange('sidebarNodeFg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
            </div>

            {/* Node Details section */}
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem', marginTop: '12px' }}>Node Box Detail Colors</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginTop: '6px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Node Border (Idle)</span>
                <input type="color" value={activeTheme.nodeBorder} onChange={(e) => handleColorChange('nodeBorder', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Node Header Bg</span>
                <input type="color" value={activeTheme.nodeHeaderBg} onChange={(e) => handleColorChange('nodeHeaderBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Node Header Font</span>
                <input type="color" value={activeTheme.nodeHeaderFg} onChange={(e) => handleColorChange('nodeHeaderFg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Node Error Border</span>
                <input type="color" value={activeTheme.nodeErrorColor} onChange={(e) => handleColorChange('nodeErrorColor', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Pin Label Font</span>
                <input type="color" value={activeTheme.nodePinFg} onChange={(e) => handleColorChange('nodePinFg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Pin Type Font</span>
                <input type="color" value={activeTheme.nodePinTypeFg} onChange={(e) => handleColorChange('nodePinTypeFg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Value Input Bg</span>
                <input type="color" value={activeTheme.nodeInputBg} onChange={(e) => handleColorChange('nodeInputBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Value Input Font</span>
                <input type="color" value={activeTheme.nodeInputFg} onChange={(e) => handleColorChange('nodeInputFg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
            </div>

            {/* Connection & Handles section */}
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem', marginTop: '12px' }}>Connections & Handles Colors</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginTop: '6px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Handle Bg</span>
                <input type="color" value={activeTheme.nodeHandleBg} onChange={(e) => handleColorChange('nodeHandleBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Handle Border</span>
                <input type="color" value={activeTheme.nodeHandleBorder} onChange={(e) => handleColorChange('nodeHandleBorder', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Flow Handle Bg</span>
                <input type="color" value={activeTheme.nodeHandleFlowBg} onChange={(e) => handleColorChange('nodeHandleFlowBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Conn Line (Idle)</span>
                <input type="color" value={activeTheme.nodeEdgeIdle} onChange={(e) => handleColorChange('nodeEdgeIdle', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Conn Line (Active)</span>
                <input type="color" value={activeTheme.nodeEdgeActive} onChange={(e) => handleColorChange('nodeEdgeActive', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
            </div>

            {/* Animation & Particles section */}
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem', marginTop: '12px' }}>Animation & Particle Colors</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginTop: '6px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Glow Pulse Color</span>
                <input type="color" value={activeTheme.animRunningGlow} onChange={(e) => handleColorChange('animRunningGlow', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Wave Ripple Color</span>
                <input type="color" value={activeTheme.animRunningWave} onChange={(e) => handleColorChange('animRunningWave', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Click Particle Color</span>
                <input type="color" value={activeTheme.animClickParticle} onChange={(e) => handleColorChange('animClickParticle', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
            </div>

            {/* Lua Syntax Highlighting section */}
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem', marginTop: '12px' }}>Lua Syntax Highlighting Colors</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginTop: '6px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Comments</span>
                <input type="color" value={activeTheme.syntaxComment} onChange={(e) => handleColorChange('syntaxComment', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Keywords</span>
                <input type="color" value={activeTheme.syntaxKeyword} onChange={(e) => handleColorChange('syntaxKeyword', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Strings</span>
                <input type="color" value={activeTheme.syntaxString} onChange={(e) => handleColorChange('syntaxString', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Numbers</span>
                <input type="color" value={activeTheme.syntaxNumber} onChange={(e) => handleColorChange('syntaxNumber', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Functions</span>
                <input type="color" value={activeTheme.syntaxFunction} onChange={(e) => handleColorChange('syntaxFunction', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Types</span>
                <input type="color" value={activeTheme.syntaxType} onChange={(e) => handleColorChange('syntaxType', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Operators</span>
                <input type="color" value={activeTheme.syntaxOperator} onChange={(e) => handleColorChange('syntaxOperator', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
            </div>

            {/* MiniMap Colors section */}
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem', marginTop: '12px' }}>MiniMap Colors</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginTop: '6px' }}>
              
              {/* MiniMap Background */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>MiniMap Bg</span>
                <input type="color" value={activeTheme.minimapBg} onChange={(e) => handleColorChange('minimapBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* MiniMap Mask */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>MiniMap Mask</span>
                <input type="color" value={activeTheme.minimapMask} onChange={(e) => handleColorChange('minimapMask', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* MiniMap Node */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>MiniMap Node Rep</span>
                <input type="color" value={activeTheme.minimapNode} onChange={(e) => handleColorChange('minimapNode', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
            </div>

            {/* Dialog & Menu Colors section */}
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem', marginTop: '12px' }}>Dialog & Menu Colors</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', marginTop: '6px' }}>
              
              {/* Dialog Header Background */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Dialog Header Bg</span>
                <input type="color" value={activeTheme.dialogHeaderBg} onChange={(e) => handleColorChange('dialogHeaderBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Dialog Header Font Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Dialog Header Font</span>
                <input type="color" value={activeTheme.dialogHeaderFg} onChange={(e) => handleColorChange('dialogHeaderFg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Menu Background */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Menu & Dropdown Bg</span>
                <input type="color" value={activeTheme.menuBg} onChange={(e) => handleColorChange('menuBg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>

              {/* Menu Font Color */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', justifyContent: 'space-between', padding: '6px 8px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)' }}>Menu Trigger Font</span>
                <input type="color" value={activeTheme.menuFg} onChange={(e) => handleColorChange('menuFg', e.target.value)} style={{ border: 'none', width: '28px', height: '24px', cursor: 'pointer', background: 'none' }} />
              </div>
            </div>
          </div>

          {/* Size Adjustments */}
          <div>
            <div className="modal-section-title" style={{ color: 'var(--accent-color)', fontWeight: 700, fontSize: '0.8rem' }}>Interface Sizes</div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', marginTop: '6px' }}>
              
              {/* Font Size */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', justifyContent: 'space-between', padding: '8px 12px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)', width: '120px' }}>Base Font Size</span>
                <input type="range" min="11" max="18" value={activeTheme.baseFontSize} onChange={(e) => handleSizeChange('baseFontSize', parseInt(e.target.value))} style={{ flex: 1, accentColor: 'var(--accent-color)' }} />
                <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', width: '35px', textAlign: 'right', fontFamily: 'JetBrains Mono' }}>{activeTheme.baseFontSize}px</span>
              </div>

              {/* Menu Bar Height */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', justifyContent: 'space-between', padding: '8px 12px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)', width: '120px' }}>Menu Bar Height</span>
                <input type="range" min="28" max="48" value={activeTheme.menuHeight} onChange={(e) => handleSizeChange('menuHeight', parseInt(e.target.value))} style={{ flex: 1, accentColor: 'var(--accent-color)' }} />
                <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', width: '35px', textAlign: 'right', fontFamily: 'JetBrains Mono' }}>{activeTheme.menuHeight}px</span>
              </div>

              {/* Border Radius */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', justifyContent: 'space-between', padding: '8px 12px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)', width: '120px' }}>Border Radius</span>
                <input type="range" min="0" max="16" value={activeTheme.borderRadius} onChange={(e) => handleSizeChange('borderRadius', parseInt(e.target.value))} style={{ flex: 1, accentColor: 'var(--accent-color)' }} />
                <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', width: '35px', textAlign: 'right', fontFamily: 'JetBrains Mono' }}>{activeTheme.borderRadius}px</span>
              </div>

              {/* Node Width */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px', justifyContent: 'space-between', padding: '8px 12px', backgroundColor: 'var(--bg-color)', borderRadius: '6px', border: '1px solid var(--border-color)' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-color)', width: '120px' }}>Node Box Width</span>
                <input type="range" min="180" max="320" value={activeTheme.nodeWidth} onChange={(e) => handleSizeChange('nodeWidth', parseInt(e.target.value))} style={{ flex: 1, accentColor: 'var(--accent-color)' }} />
                <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)', width: '45px', textAlign: 'right', fontFamily: 'JetBrains Mono' }}>{activeTheme.nodeWidth}px</span>
              </div>
            </div>
          </div>

          {/* Save Custom Name */}
          <div style={{ display: 'flex', gap: '10px', alignItems: 'flex-end', borderTop: '1px solid var(--border-color)', paddingTop: '12px' }}>
            <div className="modal-field" style={{ flex: 1 }}>
              <label className="modal-label">Save Current Style as Custom Theme</label>
              <input 
                type="text" 
                placeholder="Enter theme name..." 
                className="modal-input" 
                value={customName}
                onChange={(e) => setCustomName(e.target.value)}
                style={{ backgroundColor: 'var(--bg-color)', color: 'var(--text-color)', border: '1px solid var(--border-color)' }}
              />
            </div>
            <button className="btn btn-accent" style={{ height: '34px' }} onClick={handleSaveCustom}>
              <Save size={14} /> Save
            </button>
          </div>

        </div>

        {/* Modal Footer with Disk Load/Save */}
        <div className="modal-footer" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px 18px', backgroundColor: 'var(--sidebar-bg)' }}>
          <div style={{ display: 'flex', gap: '8px' }}>
            <button className="btn" onClick={handleImportTheme} title="Import Theme File from Disk">
              <Upload size={14} /> Import File...
            </button>
            <button className="btn" onClick={handleExportTheme} title="Export Current Theme to Disk">
              <Download size={14} /> Export File...
            </button>
          </div>
          <button className="btn btn-primary" onClick={onClose}>Close</button>
        </div>
      </div>
    </div>
  );
};
