-- @node: InputExample
-- @description: 마우스 및 키보드 자동화 입력을 수행하는 예제입니다.
function run_input_example()
    log.info("Starting input automation simulation...")
    
    -- 1. 마우스 좌표 이동 (500, 300)
    log.info("Moving mouse to (500, 300)")
    input.mouse_move(500, 300)
    time.sleep.milli(500)
    
    -- 2. 마우스 클릭
    log.info("Clicking left mouse button")
    input.mouse_click("left")
    time.sleep.milli(500)
    
    -- 3. 가상 키보드 타이핑
    log.info("Typing text input...")
    input.key_type("Hello from NOVA Flow Engine!")
    time.sleep.milli(500)
    
    -- 4. 특정 키 입력 (예: Enter 키 - 가상 키코드 13)
    log.info("Pressing Enter key")
    input.key_press(13)
end
