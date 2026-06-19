-- @node: CryptoExample
-- @description: SHA-256/MD5 해시 생성 및 Base64 인코딩/디코딩 기능을 테스트하는 예제입니다.
-- @input: message : string
-- @output: sha256_hash : string, md5_hash : string, b64_encoded : string
function run_crypto_example(message : string) -> sha256_hash : string, md5_hash : string, b64_encoded : string
    local text = message or "NOVA Scripting Core"
    log.info("Original Message: " .. text)
    
    local shaVal = crypto.sha256(text)
    local md5Val = crypto.md5(text)
    local encVal = crypto.base64_encode(text)
    local decVal = crypto.base64_decode(encVal)
    
    log.info("SHA-256 Hash: " .. shaVal)
    log.info("MD5 Hash: " .. md5Val)
    log.info("Base64 Encoded: " .. encVal)
    log.info("Base64 Decoded Check: " .. decVal)
    
    return shaVal, md5Val, encVal
end
