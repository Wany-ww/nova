export interface BridgeMessage {
  type: string;
  requestId?: string;
  payload?: any;
  error?: string | null;
}

type EventCallback = (payload: any) => void;

class WebViewBridge {
  private pendingRequests = new Map<string, { resolve: (val: any) => void; reject: (err: any) => void }>();
  private eventListeners = new Map<string, Set<EventCallback>>();

  constructor() {
    if (typeof window !== 'undefined') {
      // Attach the global message handler that C# will call
      (window as any).onMessageFromHost = (messageJson: string | BridgeMessage) => {
        try {
          const msg: BridgeMessage = typeof messageJson === 'string' ? JSON.parse(messageJson) : messageJson;
          this.handleIncomingMessage(msg);
        } catch (err) {
          console.error("Failed to parse message from host:", err, messageJson);
        }
      };
    }
  }

  private handleIncomingMessage(msg: BridgeMessage) {
    // 1. Check if it's a response to a pending request
    if (msg.requestId && this.pendingRequests.has(msg.requestId)) {
      const { resolve, reject } = this.pendingRequests.get(msg.requestId)!;
      this.pendingRequests.delete(msg.requestId);
      if (msg.error) {
        reject(new Error(msg.error));
      } else {
        resolve(msg.payload);
      }
      return;
    }

    // 2. Otherwise, treat it as an event (e.g., node_executed, log_printed)
    const listeners = this.eventListeners.get(msg.type);
    if (listeners) {
      listeners.forEach(cb => {
        try {
          cb(msg.payload);
        } catch (e) {
          console.error(`Error in event listener for ${msg.type}:`, e);
        }
      });
    }
  }

  /**
   * Sends a request to the C# backend and returns a promise that resolves with the response.
   */
  public sendRequest(type: string, payload?: any): Promise<any> {
    const requestId = Math.random().toString(36).substring(2, 11);
    const message: BridgeMessage = { type, requestId, payload };

    return new Promise((resolve, reject) => {
      this.pendingRequests.set(requestId, { resolve, reject });

      // Check if running inside WebView2
      if ((window as any).chrome?.webview?.postMessage) {
        (window as any).chrome.webview.postMessage(JSON.stringify(message));
      } else {
        // Mock environment for browser debugging/testing
        console.warn(`[Mock Mode] Sending request: ${type}`, payload);
        setTimeout(() => {
          this.mockResponse(type, requestId, payload, resolve, reject);
        }, 300);
      }
    });
  }

  /**
   * Subscribes to backend events.
   */
  public on(type: string, callback: EventCallback) {
    if (!this.eventListeners.has(type)) {
      this.eventListeners.set(type, new Set());
    }
    this.eventListeners.get(type)!.add(callback);
  }

  /**
   * Unsubscribes from backend events.
   */
  public off(type: string, callback: EventCallback) {
    const listeners = this.eventListeners.get(type);
    if (listeners) {
      listeners.delete(callback);
      if (listeners.size === 0) {
        this.eventListeners.delete(type);
      }
    }
  }

  /**
   * Mock responses for development in standard browser
   */
  private mockResponse(type: string, _requestId: string, payload: any, resolve: any, _reject: any) {
    if (type === 'GET_NODE_LIBRARY') {
      resolve([
        {
          id: 'AddOperation',
          name: 'AddOperation',
          description: '두 실수를 더합니다.',
          inputs: [
            { name: 'a', type: 'float', defaultValue: 0 },
            { name: 'b', type: 'float', defaultValue: 0 }
          ],
          outputs: [
            { name: 'c', type: 'float' }
          ],
          script: '-- @node: AddOperation\n-- @description: 두 실수를 더합니다.\nfunction add(a : float, b : float) -> c : float\n    return a + b\nend'
        },
        {
          id: 'MultiplyOperation',
          name: 'MultiplyOperation',
          description: '두 실수를 곱합니다.',
          inputs: [
            { name: 'a', type: 'float', defaultValue: 0 },
            { name: 'b', type: 'float', defaultValue: 0 }
          ],
          outputs: [
            { name: 'c', type: 'float' }
          ],
          script: '-- @node: MultiplyOperation\nfunction multiply(a : float, b : float) -> c : float\n    return a * b\nend'
        },
        {
          id: 'PrintNode',
          name: 'PrintNode',
          description: '값을 콘솔에 출력합니다.',
          inputs: [
            { name: 'value', type: 'string', defaultValue: '' }
          ],
          outputs: [],
          script: '-- @node: PrintNode\nfunction printNode(value : string)\n    print("[Lua] " .. tostring(value))\nend'
        }
      ]);
    } else if (type === 'SAVE_PROJECT' || type === 'LOAD_PROJECT') {
      resolve({ success: true });
    } else if (type === 'RUN_FLOW') {
      // Simulate execution logs and completion
      let tick = 0;
      const interval = setInterval(() => {
        tick++;
        if (tick === 1) {
          this.handleIncomingMessage({
            type: 'LOG_PRINTED',
            payload: { level: 'INFO', message: 'Flow execution started.' }
          });
        } else if (tick === 2) {
          this.handleIncomingMessage({
            type: 'LOG_PRINTED',
            payload: { level: 'INFO', message: '[Lua] 30.5' }
          });
          this.handleIncomingMessage({
            type: 'NODE_STATE_CHANGED',
            payload: { nodeId: payload.nodes[0]?.id, cnt: 1, state: 'RUNNING' }
          });
        } else {
          clearInterval(interval);
          this.handleIncomingMessage({
            type: 'LOG_PRINTED',
            payload: { level: 'INFO', message: 'Flow execution finished.' }
          });
          this.handleIncomingMessage({
            type: 'NODE_STATE_CHANGED',
            payload: { nodeId: payload.nodes[0]?.id, cnt: 1, state: 'IDLE' }
          });
          resolve({ outputs: { c: 30.5 } });
        }
      }, 500);
    } else {
      resolve({});
    }
  }
}

export const bridge = new WebViewBridge();
