-- @node: DrawText
-- @description: 이미지 위에 지정한 텍스트 문구를 출력합니다.
-- @input: src : table, text : string, x : int, y : int, scale : float
-- @output: dst : table
function drawText(src : table, text : string, x : int, y : int, scale : float) -> dst : table
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil
    end
    
    local dest = src:clone()
    local displayText = text or "OpenCV Text"
    local posX = x or 50
    local posY = y or 50
    local fontScale = scale or 1.0
    
    -- Put white text (BGR: {255, 255, 255}, thickness = 2)
    cv.putText(dest, displayText, posX, posY, fontScale, {255, 255, 255}, 2)
    return dest
end
