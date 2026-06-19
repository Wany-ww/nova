-- @node: NotifyExample
-- @description: 작업표시줄 시스템 트레이에 알림 말풍선(Balloon Tip)을 띄우는 예제입니다.
function run_notify_example()
    log.info("Displaying tray notifications...")
    
    -- 1. Info Notification
    system.notify("NOVA Engine 알림", "Flow 실행이 완료되었습니다.", "info")
    
    -- 약간의 시간 차를 두고 경고/에러 알림도 가능합니다
    time.sleep.milli(1000)
    system.notify("NOVA 경고", "시스템 온도가 정상 범위를 초과했습니다.", "warning")
end
