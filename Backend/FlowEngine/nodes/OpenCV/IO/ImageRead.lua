-- @node: ImageRead
-- @description: 지정된 경로에서 이미지 파일을 읽어서 이미지 객체로 반환합니다.
-- @input: filepath : string
-- @output: image : table
function imageRead(filepath : string) -> image : table
    log.info("Reading image from path: " .. filepath)
    
    local img = cv.imread(filepath)
    if img:empty() then
        log.error("Failed to read image! Path might be invalid.")
        return nil
    end
    
    log.info("Image read successfully: " .. tostring(img.width) .. "x" .. tostring(img.height))
    return img
end
