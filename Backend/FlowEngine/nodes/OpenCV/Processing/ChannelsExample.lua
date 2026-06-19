-- @node: ChannelsExample
-- @description: 다채널 이미지(BGR)를 단일 채널들로 분리하고 특정 채널을 조합하여 병합합니다.
-- @input: src : table
-- @output: dst_blue : table, dst_green : table, dst_red : table, dst_merged : table
function channelsExample(src : table) -> (dst_blue : table, dst_green : table, dst_red : table, dst_merged : table)
    -- 입력 체크
    if not src or src:empty() then
        log.error("Input image is null or empty!")
        return nil, nil, nil, nil
    end

    log.info("Splitting image into B, G, R channels...")

    -- 이미지 채널 분리 (BGR 순서)
    local channels = cv.split(src)
    local b = channels[1]
    local g = channels[2]
    local r = channels[3]

    -- 채널들의 유효성 확인
    if not b or not g or not r then
        log.error("Failed to split channels")
        return nil, nil, nil, nil
    end

    -- Blue와 Red 채널을 서로 맞바꿔서 새로운 이미지 병합해보기 (RGB 필터 효과)
    local merged = cv.merge({ r, g, b })

    -- 개별 분리된 채널들과 병합된 결과 이미지 반환
    return b, g, r, merged
end
