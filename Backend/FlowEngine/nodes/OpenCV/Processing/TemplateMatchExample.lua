-- @node: TemplateMatchExample
-- @description: 소스 이미지에서 템플릿 이미지를 검색하고 최대 일치 지점을 찾아 사각형을 그립니다.
-- @input: src : table, template : table
-- @output: dst_result : table, max_val : float, max_x : int, max_y : int
function templateMatchExample(src : table, template : table) -> (dst_result : table, max_val : float, max_x : int, max_y : int)
    -- 입력 체크
    if not src or src:empty() or not template or template:empty() then
        log.error("Source or template image is empty!")
        return nil, 0.0, 0, 0
    end

    log.info("Matching template utilizing Normalized Cross-Correlation...")

    -- 템플릿 매칭 수행 (cv.TM_CCOEFF_NORMED = 5)
    local matchMap = cv.matchTemplate(src, template, cv.TM_CCOEFF_NORMED)

    -- 최댓값 및 최댓값 위치 찾기
    local results = cv.minMaxLoc(matchMap)
    
    -- matchMap 임시 객체 해제
    matchMap:release()

    local confidence = results.maxVal
    local pt = results.maxLoc
    log.info("Best match confidence: " .. string.format("%.4f", confidence) .. " at (" .. tostring(pt.x) .. ", " .. tostring(pt.y) .. ")")

    -- 원본 복제본에 매칭 지점 상자 그리기
    local output = cv.Mat(src) -- clone
    cv.rectangle(output, pt.x, pt.y, pt.x + template.width, pt.y + template.height, {0, 255, 0}, 2)

    return output, confidence, pt.x, pt.y
end
