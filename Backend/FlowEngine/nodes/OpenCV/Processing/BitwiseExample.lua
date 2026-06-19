-- @node: BitwiseExample
-- @description: 두 이미지에 대해 비트 논리 연산(AND, OR, XOR, NOT)을 수행합니다.
-- @input: src1 : table, src2 : table
-- @output: dst_and : table, dst_or : table, dst_xor : table, dst_not : table
function bitwiseExample(src1 : table, src2 : table) -> (dst_and : table, dst_or : table, dst_xor : table, dst_not : table)
    -- 이미지 입력 체크
    if not src1 or src1:empty() or not src2 or src2:empty() then
        log.error("Input source images are empty or invalid!")
        return nil, nil, nil, nil
    end

    log.info("Running bitwise operations on inputs...")

    -- Bitwise AND 연산
    local band = cv.bitwise_and(src1, src2)

    -- Bitwise OR 연산
    local bor = cv.bitwise_or(src1, src2)

    -- Bitwise XOR 연산
    local bxor = cv.bitwise_xor(src1, src2)

    -- Bitwise NOT 연산 (첫 번째 이미지 반전)
    local bnot = cv.bitwise_not(src1)

    -- 논리 연산 결과들 반환
    return band, bor, bxor, bnot
end
