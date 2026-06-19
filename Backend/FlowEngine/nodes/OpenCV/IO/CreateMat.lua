-- @node: CreateMat
-- @description: 지정된 크기와 채널 수(8UC1, 8UC3, 8UC4)의 빈 이미지를 생성합니다.
-- @input: rows : int, cols : int, channels : int
-- @output: image : table
function createMat(rows : int, cols : int, channels : int) -> image : table
    log.info("Creating Mat with size: " .. tostring(cols) .. "x" .. tostring(rows) .. ", channels: " .. tostring(channels))
    
    local typeCode = cv.CV_8UC3
    if channels == 1 then
        typeCode = cv.CV_8UC1
    elseif channels == 4 then
        typeCode = cv.CV_8UC4
    end
    
    local img = cv.Mat(rows, cols, typeCode)
    return img
end
