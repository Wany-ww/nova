-- @node: TcpSend
-- @description: TCP 클라이언트로 서버에 접속하여 패킷을 전송하는 예제입니다.
function run_tcp_send()
    local ip = "127.0.0.1"
    local port = 12345
    local client = tcp.client.connect(ip, port)
    if client == nil then
        log.error("Failed to connect to TCP server at " .. ip .. ":" .. tostring(port))
        return
    end

    client:set_timeout(2000)
    
    -- 패킷 데이터 (byte array table)
    local data = {72, 101, 108, 108, 111, 33} -- "Hello!"
    client:transmit(data)
    log.info("Successfully sent TCP packet to " .. ip .. ":" .. tostring(port))
    
    client:Dispose()
end
