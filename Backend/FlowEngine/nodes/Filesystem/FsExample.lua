-- @node: FsExample
-- @description: filesystem API의 폴더 생성, 존재 여부 확인, 복사 및 삭제를 수행하는 예제입니다.
function run_fs_example()
    local cur = filesystem.current()
    log.info("Current directory: " .. cur)
    
    local testDir = "temp_fs_example"
    local copyDir = "temp_fs_example_copy"
    
    -- 폴더 생성
    filesystem.create(testDir .. "/nested")
    log.info("Created directory: " .. testDir .. "/nested")
    
    -- 존재 여부 확인
    local exists = filesystem.is_exist(testDir)
    log.info("Directory exists: " .. tostring(exists))
    
    -- 폴더 복사
    filesystem.copy(testDir, copyDir)
    log.info("Copied directory from " .. testDir .. " to " .. copyDir)
    
    -- 폴더 삭제
    filesystem.remove(testDir)
    filesystem.remove(copyDir)
    log.info("Removed directories successfully")
end
