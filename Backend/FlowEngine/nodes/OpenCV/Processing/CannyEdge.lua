-- @node: CannyEdge
-- @description: Canny 알고리즘을 사용하여 이미지의 외곽선(엣지)을 검출합니다.
-- @input: src : table, threshold1 : float, threshold2 : float
-- @output: dst : table
function cannyEdge(src : table, threshold1 : float, threshold2 : float) -> dst : table
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil
    end
    
    local t1 = threshold1 or 50.0
    local t2 = threshold2 or 150.0
    
    log.info("Running Canny edge detection: " .. tostring(t1) .. ", " .. tostring(t2))
    local dest = cv.Canny(src, t1, t2)
    return dest
end
