-- @node: ImageReadWrite
-- @description: 이미지 파일을 읽고 저장하고 화면에 팝업창으로 표시합니다.
-- @input: filepath : string, savepath : string
function imageReadWrite(filepath : string, savepath : string)
    log.info("Loading image from: " .. filepath)
    local img = cv.imread(filepath)
    if img:empty() then
        log.error("Failed to load image!")
        return
    end
    
    log.info("Image loaded successfully. Size: " .. tostring(img.width) .. "x" .. tostring(img.height))
    cv.imshow("Loaded Image", img)
    
    if savepath and savepath ~= "" then
        log.info("Saving image to: " .. savepath)
        local success = cv.imwrite(savepath, img)
        if success then
            log.info("Image saved successfully.")
        else
            log.error("Failed to save image!")
        end
    end
    img:release()
end
