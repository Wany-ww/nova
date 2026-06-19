-- @node: ConsoleClearExample
-- @description: console.clear() 함수를 사용하여 콘솔 로그를 비우는 예제입니다.
function consoleClearExample(message : string)
    log.info("콘솔에 첫 번째 로그를 출력합니다: " .. tostring(message))
    time.sleep.sec(1.5)
    
    log.info("잠시 후 콘솔이 클리어됩니다...")
    time.sleep.sec(1.0)
    
    console.clear()
    time.sleep.sec(0.5)
    
    log.info("console.clear()에 의해 이전 로그가 모두 삭제되고 새로운 로그가 출력되었습니다!")
end
