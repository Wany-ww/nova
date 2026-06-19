-- @node: FtpExample
-- @description: FTP 서버에 파일을 업로드하거나 다운로드하는 예제입니다. (실제 작동하려면 구동 중인 FTP 서버 정보가 필요합니다)
function run_ftp_example()
    log.info("Starting FTP client API test...")
    
    -- 테스트용 임시 로컬 파일 생성
    local tempFile = "temp_ftp_test.txt"
    local fs = io.open(tempFile, 'w')
    if fs then
        fs:write("Hello FTP server! This is a test file.")
        fs:close()
    end
    
    local host = "127.0.0.1"
    local port = 21
    local user = "anonymous"
    local pass = "anonymous"
    local remoteFile = "/uploads/temp_ftp_test.txt"
    local downloadDest = "downloads/temp_ftp_downloaded.txt"
    
    log.info("Attempting to upload local file " .. tempFile .. " to ftp://" .. host .. ":" .. tostring(port) .. remoteFile)
    local upOk = ftp.upload(host, port, user, pass, tempFile, remoteFile)
    
    if upOk then
        log.info("FTP Upload Succeeded!")
        
        log.info("Attempting to download file from FTP to " .. downloadDest)
        local downOk = ftp.download(host, port, user, pass, remoteFile, downloadDest)
        if downOk then
            log.info("FTP Download Succeeded! File saved to " .. downloadDest)
            filesystem.remove(downloadDest)
        else
            log.error("FTP Download Failed.")
        end
    else
        -- 로컬 테스트 환경에 FTP 서버가 준비되지 않은 경우 실패하는 것이 정상입니다.
        log.warn("FTP Upload failed. Ensure FTP server is running on " .. host .. ":" .. tostring(port))
    end
    
    -- 임시 파일 삭제
    filesystem.remove(tempFile)
end
