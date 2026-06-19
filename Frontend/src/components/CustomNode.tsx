import React, { useState } from 'react';
import { Handle, Position } from '@xyflow/react';
import type { NodeProps } from '@xyflow/react';
import { Play, Square } from 'lucide-react';

export interface PinData {
  name: string;
  type: string;
  defaultValue?: any;
}

export interface CustomNodeData {
  name: string;
  description?: string;
  script?: string;
  inputs: PinData[];
  outputs: PinData[];
  properties: {
    total: number;
    cnt: number;
    state: 'IDLE' | 'RUNNING' | 'ERROR';
    inheritedTotal?: number;
  };
  inputValues: Record<string, any>;
  connectedInputs: string[]; // List of input pin names that are connected
  computedInputs?: Record<string, any>; // Real-time computed values from upstream outputs
  isFlowDisabled?: boolean;
  isFlowRunning?: boolean;
  onRunNode?: (nodeId: string) => void;
  onStopNode?: (nodeId: string) => void;
  onUpdateTotal?: (nodeId: string, total: number) => void;
  onUpdateInputValue?: (nodeId: string, pinName: string, value: any) => void;
}

export const CustomNode = React.memo(({ id, data, selected }: NodeProps<any>) => {
  const nodeData = data as CustomNodeData;
  const { name, inputs, outputs, properties, inputValues, connectedInputs, computedInputs, isFlowDisabled = false, isFlowRunning = false, onRunNode, onStopNode, onUpdateTotal, onUpdateInputValue } = nodeData;
  const { total = 1, cnt = 0, state = 'IDLE' } = properties || {};

  const [particles, setParticles] = useState<{ id: number; x: number; y: number; dx: number; dy: number; size: number }[]>([]);

  const handleRun = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (onRunNode) onRunNode(id);
  };

  const handleStop = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (onStopNode) onStopNode(id);
  };

  const incrementTotal = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (onUpdateTotal) onUpdateTotal(id, total + 1);
  };

  const decrementTotal = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (onUpdateTotal && total > 0) onUpdateTotal(id, total - 1);
  };

  const handleInputChange = (pinName: string, valStr: string, type: string) => {
    let parsed: any = valStr;
    if (type === 'int') {
      parsed = parseInt(valStr, 10) || 0;
    } else if (type === 'float') {
      parsed = parseFloat(valStr) || 0;
    } else if (type === 'bool') {
      parsed = valStr === 'true';
    }
    if (onUpdateInputValue) {
      onUpdateInputValue(id, pinName, parsed);
    }
  };

  const handleNodeMouseDown = (e: React.MouseEvent) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const clickX = e.clientX - rect.left;
    const clickY = e.clientY - rect.top;

    const newParticles = Array.from({ length: 15 }).map((_, i) => {
      const angle = Math.random() * Math.PI * 2;
      const distance = 25 + Math.random() * 40;
      const dx = Math.cos(angle) * distance;
      const dy = Math.sin(angle) * distance;
      const size = 3 + Math.random() * 4;
      return {
        id: Date.now() + i + Math.random(),
        x: clickX,
        y: clickY,
        dx,
        dy,
        size
      };
    });

    setParticles(newParticles);
    setTimeout(() => {
      setParticles([]);
    }, 800);
  };

  return (
    <div style={{ position: 'relative' }} onMouseDown={handleNodeMouseDown}>
      {/* Click particles */}
      {particles.map(p => (
        <div
          key={p.id}
          className="click-particle"
          style={{
            left: `${p.x}px`,
            top: `${p.y}px`,
            width: `${p.size}px`,
            height: `${p.size}px`,
            '--particle-dx': `${p.dx}px`,
            '--particle-dy': `${p.dy}px`
          } as React.CSSProperties}
        />
      ))}

      {/* Spreading wave rings */}
      {state === 'RUNNING' && (
        <div className="node-wave-container">
          <div className="node-wave-ring" style={{ animationDelay: '0s' }} />
          <div className="node-wave-ring" style={{ animationDelay: '0.6s' }} />
          <div className="node-wave-ring" style={{ animationDelay: '1.2s' }} />
        </div>
      )}

      <div className={`custom-node ${selected ? 'selected' : ''} ${state === 'RUNNING' ? 'running' : ''} ${state === 'ERROR' ? 'error' : ''}`}>
        {/* Flow input handle (Diamond, purple) */}
        <Handle
          type="target"
          position={Position.Left}
          id="flow_in"
          className="flow-handle-left"
          style={{
            top: '17px',
            background: 'var(--node-handle-flow-bg)',
            width: '10px',
            height: '10px',
            borderRadius: '2px',
            transform: 'translateY(-50%) rotate(45deg)',
            border: '1.5px solid var(--node-border)',
            zIndex: 10
          }}
        />

        {/* Node Header */}
        <div className="custom-node-header">
          <span className="custom-node-title" title={name}>{name}</span>
          <div className="custom-node-controls">
            {/* cnt / total loop controls */}
            <button className="counter-btn" onClick={decrementTotal} disabled={isFlowDisabled}>-</button>
            <span>{cnt}/{total === 0 ? '∞' : total}</span>
            <button className="counter-btn" onClick={incrementTotal} disabled={isFlowDisabled}>+</button>
            
            {/* Run / Stop button */}
            {state === 'RUNNING' || (!isFlowDisabled && isFlowRunning) ? (
              <button 
                className="run-btn" 
                onClick={handleStop} 
                title="Stop execution"
                style={{ backgroundColor: 'var(--error-color)', color: 'var(--bg-color)' }}
                disabled={isFlowDisabled}
              >
                <Square size={9} fill="currentColor" />
              </button>
            ) : (
              <button 
                className="run-btn" 
                onClick={handleRun} 
                title="Run node & downstream"
                disabled={isFlowDisabled}
              >
                <Play size={9} fill="currentColor" />
              </button>
            )}
          </div>
        </div>

        {/* Flow output handle (Diamond, purple) */}
        {name !== 'IfElse' && name !== 'Loop' && name !== 'Switch' && (
          <Handle
            type="source"
            position={Position.Right}
            id="flow_out"
            className="flow-handle-right"
            style={{
              top: '17px',
              background: 'var(--node-handle-flow-bg)',
              width: '10px',
              height: '10px',
              borderRadius: '2px',
              transform: 'translateY(-50%) rotate(45deg)',
              border: '1.5px solid var(--node-border)',
              zIndex: 10
            }}
          />
        )}

        {/* Node Body */}
        <div className="custom-node-body">
          {/* Left Column: Inputs */}
          <div className="custom-node-column left">
            {inputs && inputs.map((pin) => {
              const isConnected = connectedInputs?.includes(pin.name);
              const currentVal = inputValues?.[pin.name] !== undefined ? inputValues[pin.name] : (pin.defaultValue ?? '');
              
              return (
                <div key={pin.name} className="pin-row" style={{ minHeight: '22px' }}>
                  <Handle
                    type="target"
                    position={Position.Left}
                    id={pin.name}
                    style={{ top: '50%', transform: 'translateY(-50%)' }}
                  />
                  
                  <span className="pin-label">{pin.name}</span>
                  <span className="pin-type">:{pin.type}</span>

                  {/* Show input field if not connected */}
                  {!isConnected && pin.type !== 'table' && pin.type !== 'image' && (
                    <input
                      type={pin.type === 'int' || pin.type === 'float' ? 'number' : 'text'}
                      className="node-input-field"
                      value={currentVal}
                      onChange={(e) => handleInputChange(pin.name, e.target.value, pin.type)}
                      onKeyDown={(e) => e.stopPropagation()} // Stop backspace deleting node in canvas
                    />
                  )}

                  {/* Show computed value if connected */}
                  {isConnected && computedInputs && computedInputs[pin.name] !== undefined && (
                    <span style={{ fontSize: '0.75rem', color: 'var(--success-color)', marginLeft: '6px', background: 'color-mix(in srgb, var(--success-color) 10%, transparent)', padding: '1px 4px', borderRadius: '3px' }}>
                      {typeof computedInputs[pin.name] === 'object' ? JSON.stringify(computedInputs[pin.name]) : computedInputs[pin.name].toString()}
                    </span>
                  )}
                </div>
              );
            })}
          </div>

          {/* Right Column: Outputs */}
          <div className="custom-node-column right">
            {outputs && outputs.map((pin) => (
              <div key={pin.name} className="pin-row right" style={{ minHeight: '22px' }}>
                <span className="pin-type">:{pin.type} </span>
                <span className="pin-label">{pin.name}</span>
                
                <Handle
                  type="source"
                  position={Position.Right}
                  id={pin.name}
                  style={{ top: '50%', transform: 'translateY(-50%)' }}
                />
              </div>
            ))}
          </div>
        </div>

        {/* Flow outputs for control nodes */}
        {name === 'IfElse' && (
          <div style={{ borderTop: '1px solid var(--border-color)', padding: '6px 10px', display: 'flex', flexDirection: 'column', gap: '4px', backgroundColor: 'rgba(0,0,0,0.1)' }}>
            <div style={{ position: 'relative', display: 'flex', justifyContent: 'flex-end', alignItems: 'center', minHeight: '20px' }}>
              <span style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--success-color)', marginRight: '6px' }}>True</span>
              <Handle
                type="source"
                position={Position.Right}
                id="flow_true"
                style={{
                  top: '50%',
                  transform: 'translateY(-50%) rotate(45deg)',
                  background: 'var(--node-handle-flow-bg)',
                  width: '9px',
                  height: '9px',
                  borderRadius: '2px',
                  border: '1.5px solid var(--node-border)',
                  right: '-5px'
                }}
              />
            </div>
            <div style={{ position: 'relative', display: 'flex', justifyContent: 'flex-end', alignItems: 'center', minHeight: '20px' }}>
              <span style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--error-color)', marginRight: '6px' }}>False</span>
              <Handle
                type="source"
                position={Position.Right}
                id="flow_false"
                style={{
                  top: '50%',
                  transform: 'translateY(-50%) rotate(45deg)',
                  background: 'var(--node-handle-flow-bg)',
                  width: '9px',
                  height: '9px',
                  borderRadius: '2px',
                  border: '1.5px solid var(--node-border)',
                  right: '-5px'
                }}
              />
            </div>
          </div>
        )}

        {name === 'Loop' && (
          <div style={{ borderTop: '1px solid var(--border-color)', padding: '6px 10px', display: 'flex', flexDirection: 'column', gap: '4px', backgroundColor: 'rgba(0,0,0,0.1)' }}>
            <div style={{ position: 'relative', display: 'flex', justifyContent: 'flex-end', alignItems: 'center', minHeight: '20px' }}>
              <span style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--accent-color)', marginRight: '6px' }}>Loop Body</span>
              <Handle
                type="source"
                position={Position.Right}
                id="flow_loop"
                style={{
                  top: '50%',
                  transform: 'translateY(-50%) rotate(45deg)',
                  background: 'var(--node-handle-flow-bg)',
                  width: '9px',
                  height: '9px',
                  borderRadius: '2px',
                  border: '1.5px solid var(--node-border)',
                  right: '-5px'
                }}
              />
            </div>
            <div style={{ position: 'relative', display: 'flex', justifyContent: 'flex-end', alignItems: 'center', minHeight: '20px' }}>
              <span style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', marginRight: '6px' }}>Done</span>
              <Handle
                type="source"
                position={Position.Right}
                id="flow_done"
                style={{
                  top: '50%',
                  transform: 'translateY(-50%) rotate(45deg)',
                  background: 'var(--node-handle-flow-bg)',
                  width: '9px',
                  height: '9px',
                  borderRadius: '2px',
                  border: '1.5px solid var(--node-border)',
                  right: '-5px'
                }}
              />
            </div>
          </div>
        )}

        {name === 'Switch' && (
          <div style={{ borderTop: '1px solid var(--border-color)', padding: '6px 10px', display: 'flex', flexDirection: 'column', gap: '4px', backgroundColor: 'rgba(0,0,0,0.1)' }}>
            {(() => {
              const numVal = inputValues?.number !== undefined ? Number(inputValues.number) : (computedInputs?.number !== undefined ? Number(computedInputs.number) : 0);
              const typeVal = inputValues?.type !== undefined ? String(inputValues.type) : (computedInputs?.type !== undefined ? String(computedInputs.type) : '');
              const casesList: string[] = [];
              if (typeVal && numVal > 0) {
                for (let i = 1; i <= numVal; i++) {
                  casesList.push(`${typeVal} ${i}`);
                }
              } else {
                casesList.push('true', 'false');
              }
              return (
                <>
                  {casesList.map((cName, idx) => (
                    <div key={idx} style={{ position: 'relative', display: 'flex', justifyContent: 'flex-end', alignItems: 'center', minHeight: '20px' }}>
                      <span style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--info-color)', marginRight: '6px' }}>Case {cName}</span>
                      <Handle
                        type="source"
                        position={Position.Right}
                        id={`flow_${cName}`}
                        style={{
                          top: '50%',
                          transform: 'translateY(-50%) rotate(45deg)',
                          background: 'var(--node-handle-flow-bg)',
                          width: '9px',
                          height: '9px',
                          borderRadius: '2px',
                          border: '1.5px solid var(--node-border)',
                          right: '-5px'
                        }}
                      />
                    </div>
                  ))}
                  <div style={{ position: 'relative', display: 'flex', justifyContent: 'flex-end', alignItems: 'center', minHeight: '20px' }}>
                    <span style={{ fontSize: '0.75rem', fontWeight: 600, color: 'var(--text-muted)', marginRight: '6px' }}>Default</span>
                    <Handle
                      type="source"
                      position={Position.Right}
                      id="flow_default"
                      style={{
                        top: '50%',
                        transform: 'translateY(-50%) rotate(45deg)',
                        background: 'var(--node-handle-flow-bg)',
                        width: '9px',
                        height: '9px',
                        borderRadius: '2px',
                        border: '1.5px solid var(--node-border)',
                        right: '-5px'
                      }}
                    />
                  </div>
                </>
              );
            })()}
          </div>
        )}
      </div>
    </div>
  );
});

CustomNode.displayName = 'CustomNode';
