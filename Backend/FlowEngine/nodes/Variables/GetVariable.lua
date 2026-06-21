-- @node: GetVariable
-- @description: global memory에 저장된 특정 이름의 변수값을 가져옵니다.
function get_var(name : string) -> val : any
    local res = variable.get(name)
    if res then
        log.info("Variable '" .. name .. "' retrieved successfully.")
    else
        log.warn("Variable '" .. name .. "' not found!")
    end
    return res
end
