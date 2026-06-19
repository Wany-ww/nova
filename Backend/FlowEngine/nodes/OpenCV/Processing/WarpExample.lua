-- @node: WarpExample
-- @description: 이미지 회전 변환 행렬을 구하고 아핀 기하 변환(warpAffine)을 수행합니다.
-- @input: src : table, angle : float, scale : float
-- @output: dst : table
function warpExample(src : table, angle : float, scale : float) -> dst : table
    -- 입력 이미지 확인
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil
    end

    -- 이미지 중심 계산
    local cx = src.width / 2.0
    local cy = src.height / 2.0
    local rotAngle = angle or 30.0
    local rotScale = scale or 1.0

    log.info("Calculating rotation matrix: center=(" .. tostring(cx) .. "," .. tostring(cy) .. "), angle=" .. tostring(rotAngle) .. ", scale=" .. tostring(rotScale))

    -- 회전 변환 행렬 M 구하기
    local M = cv.getRotationMatrix2D(cx, cy, rotAngle, rotScale)

    -- warpAffine을 이용하여 회전 적용
    local dst = cv.warpAffine(src, M, src.width, src.height)

    -- M 자원 해제
    M:release()

    -- 아핀 변환된 이미지 반환
    return dst
end
