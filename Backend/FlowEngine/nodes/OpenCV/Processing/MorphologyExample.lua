-- @node: MorphologyExample
-- @description: structuring element(구조 요소) 커널을 생성하여 침식(Erosion) 및 팽창(Dilation) 연산을 수행합니다.
-- @input: src : table, shape : int, kw : int, kh : int, iter : int
-- @output: dst_eroded : table, dst_dilated : table
function morphologyExample(src : table, shape : int, kw : int, kh : int, iter : int) -> (dst_eroded : table, dst_dilated : table)
    -- 입력 이미지 유효성 검사
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil, nil
    end

    -- 커널 형상 (Default: MORPH_RECT = 0)
    local s_shape = shape or cv.MORPH_RECT
    local w = kw or 3
    local h = kh or 3
    local iterations = iter or 1

    log.info("Generating structuring element kernel: shape=" .. tostring(s_shape) .. ", size=" .. tostring(w) .. "x" .. tostring(h))

    -- 구조 요소(커널) 생성
    local kernel = cv.getStructuringElement(s_shape, w, h)

    -- 침식 연산 (Erosion: 어두운 영역 확장, 노이즈 제거)
    local eroded = cv.erode(src, kernel, iterations)

    -- 팽창 연산 (Dilation: 밝은 영역 확장, 구멍 채우기)
    local dilated = cv.dilate(src, kernel, iterations)

    -- 사용이 끝난 unmanaged 커널 자원을 해제합니다.
    kernel:release()

    -- 결과 이미지 객체 반환
    return eroded, dilated
end
