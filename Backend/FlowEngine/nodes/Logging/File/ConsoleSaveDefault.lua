-- @node: ConsoleSaveDefault
-- @description: console.save(true)를 이용해 기본 경로(save/YYYYMMDD_log.txt)로 실시간 저장하는 예제입니다.
function consoleSaveDefault()
    log.info("로그 저장을 시작하기 전 메시지입니다. (이 메시지는 파일에 저장되지 않습니다)")
    time.sleep.sec(0.5)
    
    -- console.save(true)를 사용하여 년월일_log.txt 파일로 저장합니다.
    console.save(true)
    log.info("[Console Default Save] 실시간 저장이 시작되었습니다. 이 줄부터 텍스트 파일에 기록됩니다.")
    time.sleep.sec(0.5)
    
    log.warn("[Console Default Save] 경고 로그 역시 실시간으로 파일에 기록됩니다.")
    time.sleep.sec(0.5)
    
    -- 저장을 중단합니다.
    console.save(false)
    log.info("[Console Default Save] 실시간 저장이 중지되었습니다. 이 메시지는 저장되지 않습니다.")
end
