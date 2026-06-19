-- @node: SystemExample
-- @description: 외부 시스템 프로세스를 실행하고 출력 결과를 얻어오는 예제입니다.
function run_system_example()
    log.info("Starting System Process execution test...")
    
    -- cmd.exe로 간단한 디렉토리 목록 조회 명령 실행
    local cmd = "cmd.exe"
    local args = { "/c", "dir" }
    
    log.info("Running: " .. cmd .. " " .. args[1] .. " " .. args[2])
    local stdout, exit_code = system.run(cmd, args)
    
    log.info("Process Exit Code: " .. tostring(exit_code))
    
    if exit_code == 0 then
        -- 앞쪽 일부 내용만 출력
        local preview = string.sub(stdout, 1, 500)
        log.info("Process Output Preview:\n" .. preview .. "\n...")
    else
        log.error("Process execution failed:\n" .. stdout)
    end
end
