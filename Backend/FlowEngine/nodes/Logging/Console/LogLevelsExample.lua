-- @node: LogLevelsExample
-- @description: log.info, log.warn, log.error 등 다양한 로그 레벨을 사용하는 예제입니다.
function logLevelsExample(value : string)
    print("일반 print 함수로 출력하는 정보 로그 (INFO 레벨로 분류됩니다)")
    time.sleep.sec(0.5)
    
    log.info("[INFO] 프로세스가 정상 시작되었습니다. 입력값: " .. tostring(value))
    time.sleep.sec(0.8)
    
    log.warn("[WARN] 경고성 로그 예시입니다. 리소스 사용량이 높을 때 사용될 수 있습니다.")
    time.sleep.sec(0.8)
    
    log.error("[ERROR] 에러 발생 로그 예시입니다. 예외가 발생하거나 실행이 불가능할 때 사용됩니다.")
end
