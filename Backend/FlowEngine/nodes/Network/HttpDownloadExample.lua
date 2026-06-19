-- @node: HttpDownloadExample
-- @description: 웹 상의 원격 파일을 지정된 로컬 경로로 다운로드하는 예제입니다.
-- @input: url : string, destPath : string
-- @output: success : bool
function run_http_download(url : string, destPath : string) -> success : bool
    local targetUrl = url or "https://picsum.photos/200/300"
    local targetDest = destPath or "save/download_test.jpg"
    
    log.info("Downloading file from: " .. targetUrl)
    log.info("Saving file to: " .. targetDest)
    
    local ok = http.download(targetUrl, targetDest)
    if ok then
        log.info("Download completed successfully!")
        return true
    else
        log.error("Failed to download file.")
        return false
    end
end
