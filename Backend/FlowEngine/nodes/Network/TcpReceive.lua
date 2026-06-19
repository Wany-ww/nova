-- @node: TcpReceive
-- @description: TCP 서버를 열고 클라이언트의 연결 및 패킷 수신을 대기하는 예제입니다.
function run_tcp_receive()
    local port = 12345
    local server = tcp.server.create(port)
    if server == nil then
        log.error("Failed to start TCP server on port " .. tostring(port))
        return
    end

    server:set_timeout(5000) -- 5초 대기
    log.info("TCP server listening on port " .. tostring(port) .. "...")
    
    local data = server:receive()
    if data and #data > 0 then
        local msg = ""
        for i = 1, #data do
            msg = msg .. string.char(data[i])
        end
        log.info("Received TCP packet: " .. msg)
    else
        log.warn("No data received or timeout occurred")
    end
    
    server:Dispose()
end
