-- @node: BlurExample
-- @description: GaussianBlur 및 medianBlur를 사용하여 이미지의 노이즈를 제거하고 부드럽게 필터링합니다.
-- @input: src : table, ksize : int, sigma : float
-- @output: dst_gaussian : table, dst_median : table
function blurExample(src : table, ksize : int, sigma : float) -> (dst_gaussian : table, dst_median : table)
    -- 입력 이미지가 비어있는지 확인합니다.
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil, nil
    end

    -- 필터 크기(홀수여야 함)와 시그마 값을 검증 및 설정합니다.
    local k = ksize or 5
    if k % 2 == 0 then
        k = k + 1 -- 짝수인 경우 홀수로 만듭니다.
    end
    
    local s = sigma or 1.5
    log.info("Running Gaussian Blur and Median Blur with kernel size: " .. tostring(k))

    -- Gaussian Blur 적용 (가우시안 노이즈 제거 및 스무딩)
    local gaussian = cv.GaussianBlur(src, k, k, s)

    -- Median Blur 적용 (솔트앤페퍼/점 잡음 제거)
    local median = cv.medianBlur(src, k)

    -- 필터링된 두 이미지 Mat 객체를 반환합니다.
    return gaussian, median
end
