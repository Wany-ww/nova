-- @node: ContourExample
-- @description: 이미지에서 외곽선(Contour)을 찾고 각 외곽선별로 경계 사각형(Bounding Box)을 계산하여 그립니다.
-- @input: src : table, thresh_val : float
-- @output: dst : table, contour_count : int
function contourExample(src : table, thresh_val : float) -> (dst : table, contour_count : int)
    -- 이미지 체크
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil, 0
    end

    local th = thresh_val or 128.0
    log.info("Binarizing image and finding contours: threshold=" .. tostring(th))

    -- 1. 그레이스케일 변환 및 이진화 수행
    local gray = cv.cvtColor(src, cv.COLOR_BGR2GRAY)
    local bin = cv.threshold(gray, th, 255.0, cv.THRESH_BINARY)[2]

    -- 2. 외곽선 검출 (RETR_EXTERNAL = 0, CHAIN_APPROX_SIMPLE = 2)
    local contours = cv.findContours(bin, cv.RETR_EXTERNAL, cv.CHAIN_APPROX_SIMPLE)
    local count = #contours
    log.info("Found contours count: " .. tostring(count))

    -- 원본 복제본에 외곽선 및 바운딩 박스 그리기
    local output = cv.Mat(src) -- clone
    
    -- 외곽선 그리기 (녹색선, 두께 2)
    cv.drawContours(output, contours, -1, {0, 255, 0}, 2)

    -- 각 외곽선별 Bounding Box 계산 및 그리기 (빨간색선)
    for i = 1, count do
        local rect = cv.boundingRect(contours[i])
        cv.rectangle(output, rect.x, rect.y, rect.x + rect.width, rect.y + rect.height, {255, 0, 0}, 1)
    end

    -- 임시 Mat 자원 정리
    gray:release()
    bin:release()

    return output, count
end
