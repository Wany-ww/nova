-- @node: ColorConvert
-- @description: 입력 이미지의 색상 공간을 변환합니다 (예: BGR -> Gray = 6).
-- @input: src : table, code : int
-- @output: dst : table
function colorConvert(src : table, code : int) -> dst : table
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil
    end
    
    local conversionCode = code or cv.COLOR_BGR2GRAY
    log.info("Converting color space using code: " .. tostring(conversionCode))
    local dest = cv.cvtColor(src, conversionCode)
    return dest
end
