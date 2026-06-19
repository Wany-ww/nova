-- @node: PrintNode
-- @description: 입력 값을 콘솔 로그에 출력합니다.
function printNode(value : string)
    log.info("[Info Log] Value is " .. tostring(value))
    time.sleep.sec(1)
    log.warn("[Warn Log] Value is " .. tostring(value))
    time.sleep.ms(500)
    log.error("[Error Log] Value is " .. tostring(value))
end
