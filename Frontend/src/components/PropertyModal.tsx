import React, { useState } from 'react';
import { X } from 'lucide-react';
import type { CustomNodeData } from './CustomNode';

interface Token {
  type: 'comment' | 'keyword' | 'string' | 'number' | 'function' | 'type' | 'operator' | 'text';
  text: string;
}

function tokenizeLua(code: string): Token[] {
  const tokens: Token[] = [];
  
  const keywords = new Set([
    'and', 'break', 'do', 'else', 'elseif', 'end', 'false', 'for', 'function',
    'if', 'in', 'local', 'nil', 'not', 'or', 'repeat', 'return', 'then', 'true',
    'until', 'while'
  ]);

  const types = new Set([
    'int', 'float', 'string', 'bool', 'table', 'image'
  ]);

  // Combined scanner regex: comments, bracket strings, quoted strings, numbers, words, operators
  const regex = /(--\[\[[\s\S]*?\]\]|--.*|\[\[[\s\S]*?\]\]|"(?:[^"\\]|\\.)*"|'(?:[^'\\]|\\.)*'|\b\d+(?:\.\d+)?\b|[a-zA-Z_][a-zA-Z0-9_]*|->|~=|==|<=|>=|\.\.|[+\-*/=%#~<>(),.:;[\]{}])/g;

  let lastIndex = 0;
  let match;

  while ((match = regex.exec(code)) !== null) {
    const matchText = match[0];
    const matchIndex = match.index;

    if (matchIndex > lastIndex) {
      tokens.push({ type: 'text', text: code.slice(lastIndex, matchIndex) });
    }

    if (matchText.startsWith('--')) {
      tokens.push({ type: 'comment', text: matchText });
    } else if (matchText.startsWith('"') || matchText.startsWith("'") || matchText.startsWith('[[')) {
      tokens.push({ type: 'string', text: matchText });
    } else if (/^\d/.test(matchText)) {
      tokens.push({ type: 'number', text: matchText });
    } else if (/^[a-zA-Z_]/.test(matchText)) {
      if (keywords.has(matchText)) {
        tokens.push({ type: 'keyword', text: matchText });
      } else if (types.has(matchText)) {
        tokens.push({ type: 'type', text: matchText });
      } else {
        const nextPart = code.slice(regex.lastIndex).trim();
        if (nextPart.startsWith('(')) {
          tokens.push({ type: 'function', text: matchText });
        } else {
          tokens.push({ type: 'text', text: matchText });
        }
      }
    } else if (/^(->|~=|==|<=|>=|\.\.|[+\-*/=%#~<>])/.test(matchText)) {
      tokens.push({ type: 'operator', text: matchText });
    } else {
      tokens.push({ type: 'text', text: matchText });
    }

    lastIndex = regex.lastIndex;
  }

  if (lastIndex < code.length) {
    tokens.push({ type: 'text', text: code.slice(lastIndex) });
  }

  return tokens;
}

interface PropertyModalProps {
  nodeId: string;
  data: CustomNodeData;
  outputValues?: Record<string, any>; // Last output values from flow runner
  computedInputValues?: Record<string, any>; // Computed inputs from upstream node outputs
  onClose: () => void;
  onSave: (nodeId: string, inputValues: Record<string, any>, total: number) => void;
}

export const PropertyModal: React.FC<PropertyModalProps> = ({
  nodeId,
  data,
  outputValues = {},
  computedInputValues = {},
  onClose,
  onSave
}) => {
  const [inputs, setInputs] = useState<Record<string, any>>({ ...data.inputValues });
  const [total, setTotal] = useState<number>(data.properties?.total ?? 1);
  const [activeTab, setActiveTab] = useState<'properties' | 'outputs' | 'script'>('properties');

  const handleInputChange = (pinName: string, val: string, type: string) => {
    let parsed: any = val;
    if (type === 'int') {
      parsed = parseInt(val, 10) || 0;
    } else if (type === 'float') {
      parsed = parseFloat(val) || 0.0;
    } else if (type === 'bool') {
      parsed = val === 'true';
    }
    setInputs(prev => ({
      ...prev,
      [pinName]: parsed
    }));
  };

  const handleSave = () => {
    onSave(nodeId, inputs, total);
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ width: '500px' }}>
        {/* Modal Header */}
        <div className="modal-header" style={{ backgroundColor: 'var(--title-bar-bg)', borderBottom: '1px solid var(--border-color)' }}>
          <div className="modal-title" style={{ color: 'var(--title-bar-fg)' }}>Node Details: {data.name}</div>
          <button className="modal-close" onClick={onClose} style={{ color: 'var(--title-bar-fg)' }}>
            <X size={18} />
          </button>
        </div>

        {/* Tabs Bar */}
        <div className="tabs" style={{ padding: '8px 18px 0 18px', borderBottom: '1px solid var(--border-color)', backgroundColor: 'var(--panel-bg)' }}>
          <button 
            className={`tab ${activeTab === 'properties' ? 'active' : ''}`} 
            onClick={() => setActiveTab('properties')}
          >
            Inputs & Setup
          </button>
          {data.outputs && data.outputs.length > 0 && (
            <button 
              className={`tab ${activeTab === 'outputs' ? 'active' : ''}`} 
              onClick={() => setActiveTab('outputs')}
            >
              Outputs
            </button>
          )}
          {data.script && (
            <button 
              className={`tab ${activeTab === 'script' ? 'active' : ''}`} 
              onClick={() => setActiveTab('script')}
            >
              Lua Script
            </button>
          )}
        </div>

        {/* Modal Body */}
        <div className="modal-body" style={{ minHeight: '260px' }}>
          {activeTab === 'properties' && (
            <>
              {data.description && (
                <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontStyle: 'italic', marginBottom: '8px' }}>
                  {data.description}
                </div>
              )}

              {/* Properties Section */}
              <div>
                <div className="modal-section-title">Execution Control</div>
                <div className="modal-field">
                  <label className="modal-label">Repeat Count (total) [0 = ∞] {data.isFlowDisabled && "(Disabled: Downstream Flow Node)"}</label>
                  <input
                    type="number"
                    className={`modal-input ${data.isFlowDisabled ? 'modal-input-readonly' : ''}`}
                    min={0}
                    value={total}
                    disabled={data.isFlowDisabled}
                    onChange={(e) => setTotal(parseInt(e.target.value, 10) >= 0 ? parseInt(e.target.value, 10) : 0)}
                  />
                  {total === 0 && (
                    <span style={{ fontSize: '0.8rem', color: 'var(--success-color)', marginTop: '2px', fontWeight: 500 }}>
                      ∞ (Infinite Loops)
                    </span>
                  )}
                </div>
              </div>

              {/* Inputs Section */}
              {data.inputs && data.inputs.length > 0 && (
                <div style={{ marginTop: '12px' }}>
                  <div className="modal-section-title">Inputs</div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                    {data.inputs.map(pin => {
                      const isConnected = data.connectedInputs?.includes(pin.name);
                      let val = inputs[pin.name] !== undefined ? inputs[pin.name] : (pin.defaultValue ?? '');
                      
                      if (isConnected && computedInputValues && computedInputValues[pin.name] !== undefined) {
                        val = computedInputValues[pin.name];
                      }

                      return (
                        <div key={pin.name} className="modal-field">
                          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                            <span className="modal-label">{pin.name} ({pin.type})</span>
                            {isConnected && (
                              <span style={{ fontSize: '0.7rem', color: 'var(--success-color)' }}>Connected (overwritten at run)</span>
                            )}
                          </div>
                          
                          {pin.type === 'bool' ? (
                            <select
                              className="modal-input"
                              value={val.toString()}
                              onChange={(e) => handleInputChange(pin.name, e.target.value, pin.type)}
                            >
                              <option value="true">True</option>
                              <option value="false">False</option>
                            </select>
                          ) : (
                            <input
                              type={pin.type === 'int' || pin.type === 'float' ? 'number' : 'text'}
                              className={`modal-input ${isConnected ? 'modal-input-readonly' : ''}`}
                              value={typeof val === 'object' ? JSON.stringify(val) : val}
                              disabled={isConnected}
                              onChange={(e) => handleInputChange(pin.name, e.target.value, pin.type)}
                            />
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}
            </>
          )}

          {activeTab === 'outputs' && data.outputs && data.outputs.length > 0 && (
            <div>
              <div className="modal-section-title">Outputs (Last Execution)</div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                {data.outputs.map(pin => {
                  const outVal = outputValues[pin.name];
                  const displayVal = outVal === undefined 
                    ? 'No value computed yet' 
                    : typeof outVal === 'object' 
                      ? JSON.stringify(outVal, null, 2) 
                      : outVal.toString();

                  return (
                    <div key={pin.name} className="modal-field">
                      <span className="modal-label">{pin.name} ({pin.type})</span>
                      <textarea
                        className="modal-input modal-input-readonly"
                        style={{ fontFamily: 'JetBrains Mono', fontSize: '0.75rem', height: '80px', resize: 'none' }}
                        value={displayVal}
                        readOnly
                      />
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {activeTab === 'script' && data.script && (
            <div>
              <div className="modal-section-title">Lua Script Source</div>
              <div className="modal-field">
                <div 
                  className="modal-input modal-input-readonly"
                  style={{
                    fontFamily: 'JetBrains Mono, monospace',
                    fontSize: '0.8rem',
                    height: '240px',
                    overflowY: 'auto',
                    backgroundColor: 'var(--bg-color)',
                    border: '1px solid var(--border-color)',
                    padding: '12px',
                    borderRadius: '6px',
                    whiteSpace: 'pre',
                    textAlign: 'left'
                  }}
                >
                  <code style={{ display: 'block', width: '100%', height: '100%', fontFamily: 'inherit' }}>
                    {tokenizeLua(data.script).map((t, idx) => {
                      let color = 'inherit';
                      let fontWeight = 'normal';
                      
                      if (t.type === 'comment') {
                        color = 'var(--syntax-comment, #6a9955)';
                      } else if (t.type === 'keyword') {
                        color = 'var(--syntax-keyword, #569cd6)';
                        fontWeight = 'bold';
                      } else if (t.type === 'string') {
                        color = 'var(--syntax-string, #ce9178)';
                      } else if (t.type === 'number') {
                        color = 'var(--syntax-number, #b5cea8)';
                      } else if (t.type === 'function') {
                        color = 'var(--syntax-function, #dcdcaa)';
                      } else if (t.type === 'type') {
                        color = 'var(--syntax-type, #4ec9b0)';
                      } else if (t.type === 'operator') {
                        color = 'var(--syntax-operator)';
                      }
                      
                      return (
                        <span key={idx} style={{ color, fontWeight }}>
                          {t.text}
                        </span>
                      );
                    })}
                  </code>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Modal Footer */}
        <div className="modal-footer">
          <button className="btn" onClick={onClose}>Cancel</button>
          <button className="btn btn-primary" onClick={handleSave}>Apply Changes</button>
        </div>
      </div>
    </div>
  );
};
