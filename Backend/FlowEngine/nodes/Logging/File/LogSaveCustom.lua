-- @node: LogSaveCustom
-- @description: log.save(true, file)을 이용해 지정한 파일 경로로 실시간 저장하는 예제입니다.
function logSaveCustom(fileName : string)
    local targetFile = fileName
    if targetFile == nil or targetFile == "" then
        targetFile = "custom_flow_log.txt"
    end
    
    log.info("사용자 지정 경로로 로그 저장을 시도합니다: " .. targetFile)
    
    -- 지정된 파일 이름으로 저장 시작
    log.save(true, targetFile)
    
    log.info("[Custom Save] 특정 파일 경로로 저장이 시작되었습니다.")
    log.warn("[Custom Save] 이 파일은 실행 파일과 동일한 디렉토리에 생성되거나 지정된 상대/절대 경로에 위치합니다.")
    
    -- 저장 종료
    log.save(false)
    log.info("[Custom Save] 사용자 지정 로그 저장이 완료되었습니다.")
end
