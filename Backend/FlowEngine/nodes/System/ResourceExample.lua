-- @node: ResourceExample
-- @description: CPU 사용량, RAM 상태, 디스크 여유 공간 등 시스템 리소스를 모니터링하여 로그에 기록하는 예제입니다.
function run_resource_monitoring()
    log.info("Starting System Resource monitoring test...")

    -- 1. CPU Usage
    local cpu = system.cpu_usage()
    log.info("System CPU Usage: " .. string.format("%.2f%%", cpu))

    -- 2. RAM Usage
    local ram = system.ram_usage()
    if ram then
        log.info(string.format("System RAM Usage: %.2f / %.2f GB (Load: %.1f%%)", ram.usedGb, ram.totalGb, ram.load))
    else
        log.error("Failed to retrieve RAM status.")
    end

    -- 3. Disk space (C: drive)
    local diskFree = system.disk_free("C:")
    log.info("C: Partition Free Space: " .. string.format("%.2f GB", diskFree))
end
