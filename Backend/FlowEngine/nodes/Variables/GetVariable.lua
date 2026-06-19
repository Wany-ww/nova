-- @node: GetVariable
-- @description: global memory에 저장된 특정 이름의 변수값을 로그에 출력합니다.
function get_var(name : string)
    local val = variable.get(name)
    if val then
        log.info("Variable '" .. name .. "' = " .. tostring(val))
    else
        log.warn("Variable '" .. name .. "' not found!")
    end
end
