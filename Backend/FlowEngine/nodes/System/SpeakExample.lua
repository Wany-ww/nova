-- @node: SpeakExample
-- @description: Windows SAPI TTS 음성 합성 엔진을 이용하여 텍스트를 음성으로 안내하는 예제입니다.
-- @input: text : string
function run_speak_example(text : string)
    local msg = text or "Hello. Welcome to NOVA Lua Flow Engine."
    log.info("Speaking TTS message: " .. msg)
    system.speak(msg)
end
