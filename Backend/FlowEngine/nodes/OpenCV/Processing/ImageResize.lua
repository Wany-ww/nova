-- @node: ImageResize
-- @description: 이미지의 크기(해상도)를 지정한 가로/세로 크기로 조절합니다.
-- @input: src : table, width : int, height : int
-- @output: dst : table
function imageResize(src : table, width : int, height : int) -> dst : table
    if not src or src:empty() then
        log.error("Input image is empty!")
        return nil
    end
    
    local w = width or 320
    local h = height or 240
    
    log.info("Resizing image to: " .. tostring(w) .. "x" .. tostring(h))
    local dest = cv.resize(src, w, h)
    return dest
end
