-- @node: LoopTestNode
-- @description: 실행할 때마다 카운터를 1씩 증가시켜 누적 합을 계산합니다.
-- @input: value : float, accumulator : float
-- @output: sum : float
function loopTest(value : float, accumulator : float) -> sum : float
    local currentAccum = accumulator or 0
    return currentAccum + value
end
