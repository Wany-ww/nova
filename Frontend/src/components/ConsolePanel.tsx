import React, { useState } from 'react';
import { Trash2 } from 'lucide-react';

export interface LogEntry {
  timestamp: Date;
  level: 'INFO' | 'WARN' | 'ERROR';
  message: string;
  source: 'USER_LUA' | 'SYSTEM';
}

interface ConsolePanelProps {
  logs: LogEntry[];
  onClear: () => void;
  style?: React.CSSProperties;
}

export const ConsolePanel: React.FC<ConsolePanelProps> = ({ logs, onClear, style }) => {
  const [activeTab, setActiveTab] = useState<'LUA' | 'SYSTEM'>('LUA');
  const [filters, setFilters] = useState({
    INFO: true,
    WARN: true,
    ERROR: true
  });

  const toggleFilter = (level: 'INFO' | 'WARN' | 'ERROR') => {
    setFilters(prev => ({
      ...prev,
      [level]: !prev[level]
    }));
  };

  const filteredLogs = logs.filter(log => {
    // Tab filter
    if (activeTab === 'LUA' && log.source !== 'USER_LUA') return false;
    if (activeTab === 'SYSTEM' && log.source !== 'SYSTEM') return false;
    
    // Level filter
    return filters[log.level];
  });

  const formatTime = (date: Date) => {
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
  };

  return (
    <div className="bottom-panel" style={style}>
      {/* Panel Header */}
      <div className="panel-header">
        <div className="tabs">
          <button
            className={`tab ${activeTab === 'LUA' ? 'active' : ''}`}
            onClick={() => setActiveTab('LUA')}
          >
            Log
          </button>
          <button
            className={`tab ${activeTab === 'SYSTEM' ? 'active' : ''}`}
            onClick={() => setActiveTab('SYSTEM')}
          >
            Console (System)
          </button>
        </div>

        <div className="panel-filters">
          {/* Level filters */}
          <label className="filter-checkbox">
            <input
              type="checkbox"
              checked={filters.INFO}
              onChange={() => toggleFilter('INFO')}
            />
            Info
          </label>
          <label className="filter-checkbox">
            <input
              type="checkbox"
              checked={filters.WARN}
              onChange={() => toggleFilter('WARN')}
            />
            Warn
          </label>
          <label className="filter-checkbox">
            <input
              type="checkbox"
              checked={filters.ERROR}
              onChange={() => toggleFilter('ERROR')}
            />
            Error
          </label>

          <div style={{ width: '1px', height: '14px', backgroundColor: 'var(--border-color)', margin: '0 4px' }} />

          {/* Clear button */}
          <button className="btn" style={{ padding: '4px 8px' }} onClick={onClear} title="Clear Log">
            <Trash2 size={13} />
            Clear
          </button>
        </div>
      </div>

      {/* Panel Content (Logs console) */}
      <div className="panel-content">
        {filteredLogs.length === 0 ? (
          <div style={{ color: 'var(--text-muted)', fontStyle: 'italic', textAlign: 'center', marginTop: '20px' }}>
            No log entries to display.
          </div>
        ) : (
          filteredLogs.map((log, index) => (
            <div key={index} className="log-line">
              <span className="log-time">[{formatTime(log.timestamp)}]</span>
              <span className={`log-level-${log.level.toLowerCase()}`}>
                [{log.level}]
              </span>
              <span className="log-text">{log.message}</span>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
