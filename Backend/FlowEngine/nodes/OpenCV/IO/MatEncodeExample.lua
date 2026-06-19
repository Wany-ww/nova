-- @node: MatEncodeExample
-- @description: OpenCV Mat 이미지를 바이너리(PNG)로 인코딩/디코딩하고 소켓을 통해 전송 및 수신하는 종합 예제입니다.
function run_mat_encode_example()
    log.info("Starting Mat Encode/Decode and socket transfer test...")
    
    -- 1. 100x100 크기의 간단한 검은색 Mat 이미지 생성 및 그리기
    local mat = cv.Mat(100, 100, cv.CV_8UC3)
    cv.rectangle(mat, 10, 10, 90, 90, {255, 0, 0}, 2) -- 파란색 사각형 그리기
    
    -- 2. PNG 형식 바이트 배열로 인코딩
    local pngBytes = cv.imencode(".png", mat)
    log.info("Encoded Mat to PNG. Bytes count: " .. tostring(#pngBytes))
    
    -- 3. 루프백 소켓 통신을 이용한 이미지 전송 테스트
    local port = 12349
    local server = tcp.server.create(port)
    local client = tcp.client.connect("127.0.0.1", port)
    
    if server and client then
        server:set_timeout(2000)
        client:set_timeout(2000)
        
        log.info("Client sending PNG bytes over TCP...")
        client:transmit(pngBytes)
        
        -- 수신 확인
        if server:has_data() or server:is_connected() then
            log.info("Server detected incoming data.")
            local rxBytes = server:receive()
            log.info("Server received PNG bytes. Count: " .. tostring(#rxBytes))
            
            -- 디코딩하여 이미지 윈도우로 화면 출력
            local decodedMat = cv.imdecode(rxBytes)
            if decodedMat then
                cv.imshow("Server Decoded Frame", decodedMat)
                log.info("Decoded image frame and displayed successfully.")
                decodedMat:release()
            else
                log.error("Failed to decode image from received bytes")
            end
        else
            log.warn("Server did not receive data in time")
        end
        
        client:Dispose()
        server:Dispose()
    else
        log.error("Failed to create loopback sockets for transfer test")
    end
    
    mat:release()
end
