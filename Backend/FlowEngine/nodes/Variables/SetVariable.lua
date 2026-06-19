-- @node: SetVariable
-- @description: global memory에 특정 이름의 변수값을 저장합니다.
function set_var(name : string, val : string)
    variable.set(name, val)
end
