-- @node: DrawShapes
-- @description: 이미지 위에 사각형(빨강), 원(초록), 선(파랑) 등의 도형을 그립니다.
-- @input: src : table, drawRect : bool, drawCircle : bool, drawLine : bool
-- @output: dst : table
function drawShapes(src : table, drawRect : bool, drawCircle : bool, drawLine : bool) -> dst : table
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil
    end
    
    local dest = src:clone()
    
    if drawRect then
        -- Draw a red rectangle (BGR: {255, 0, 0})
        cv.rectangle(dest, 30, 30, 180, 180, {255, 0, 0}, 2)
    end
    
    if drawCircle then
        -- Draw a green filled circle (BGR: {0, 255, 0}, thickness = -1)
        cv.circle(dest, 300, 150, 50, {0, 255, 0}, -1)
    end
    
    if drawLine then
        -- Draw a blue line (BGR: {0, 0, 255})
        cv.line(dest, 10, 220, 400, 220, {0, 0, 255}, 3)
    end
    
    return dest
end
