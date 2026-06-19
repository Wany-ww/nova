-- @node: ImageShow
-- @description: 이미지 객체를 네이티브 윈도우 창으로 띄워서 화면에 보여줍니다.
-- @input: title : string, image : table
function imageShow(title : string, image : table)
    if not image or image:empty() then
        log.error("Image is empty, cannot show!")
        return
    end
    
    local winTitle = title or "NOVA Image View"
    log.info("Displaying window: " .. winTitle)
    cv.imshow(winTitle, image)
end
