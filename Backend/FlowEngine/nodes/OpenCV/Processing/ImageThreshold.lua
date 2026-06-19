-- @node: ImageThreshold
-- @description: 그레이스케일 이미지에 임계값 이진화 처리를 수행합니다 (Binary = 0, Otsu = 8).
-- @input: src : table, thresh : float, maxval : float, type : int
-- @output: dst : table, retval : float
function imageThreshold(src : table, thresh : float, maxval : float, type : int) -> dst : table, retval : float
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil, 0.0
    end
    
    local thresholdVal = thresh or 127.0
    local maximumVal = maxval or 255.0
    local thresholdType = type or cv.THRESH_BINARY
    
    log.info("Applying Threshold: thresh=" .. tostring(thresholdVal) .. ", maxval=" .. tostring(maximumVal))
    local res = cv.threshold(src, thresholdVal, maximumVal, thresholdType)
    return res[2], res[1]
end
