-- @node: HttpExample
-- @description: HTTP GET/POST 통신 및 JSON 파싱/직렬화를 수행하는 예제입니다.
function run_http_example()
    log.info("Starting HTTP and JSON API test...")
    
    -- 1. HTTP GET & JSON Parse
    local url = "https://httpbin.org/get?name=NOVA&version=2"
    local headers = { ["User-Agent"] = "NOVA-FlowEngine" }
    
    log.info("Sending GET request to " .. url)
    local res = http.get(url, headers)
    
    if res.status == 200 then
        log.info("Response Status: " .. tostring(res.status))
        
        -- JSON 파싱
        local data = json.parse(res.body)
        if data and data.args then
            log.info("Parsed Args: name: " .. tostring(data.args.name) .. ", version: " .. tostring(data.args.version))
        else
            log.warn("Failed to parse args from response body")
        end
    else
        log.error("GET request failed. Status: " .. tostring(res.status) .. ", Error: " .. tostring(res.error))
    end
    
    -- 2. JSON Stringify & HTTP POST
    local payload = {
        title = "Hello NOVA",
        tags = { "flow", "engine", "lua" },
        enabled = true
    }
    local jsonStr = json.stringify(payload)
    log.info("Serialized JSON: " .. jsonStr)
    
    local postUrl = "https://httpbin.org/post"
    local postHeaders = { ["Content-Type"] = "application/json" }
    
    log.info("Sending POST request to " .. postUrl)
    local postRes = http.post(postUrl, jsonStr, postHeaders)
    
    if postRes.status == 200 then
        log.info("POST Status: " .. tostring(postRes.status))
        
        local postData = json.parse(postRes.body)
        if postData and postData.json then
            log.info("Received echoed tags count: " .. tostring(#postData.json.tags))
        end
    else
        log.error("POST request failed. Status: " .. tostring(postRes.status) .. ", Error: " .. tostring(postRes.error))
    end
end
