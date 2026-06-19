-- @node: CsvExample
-- @description: 2차원 테이블 데이터를 CSV 파일로 작성하고, 다시 읽어와 확인하는 예제입니다.
-- @input: filePath : string
-- @output: success : bool
function run_csv_example(filePath : string) -> success : bool
    local path = filePath or "save/csv_test.csv"
    log.info("CSV Test: File path is " .. path)
    
    -- 1. Create a 2D Lua table array for CSV data
    local dataToWrite = {
        { "ID", "Title", "Score", "Active" },
        { "1", "Test Node A", "95.5", "true" },
        { "2", "Test Node B, containing a comma", "82", "false" },
        { "3", "Test Node \"C\" with quotes", "60", "true" }
    }
    
    log.info("Writing 2D table data to CSV file...")
    local ok = csv.write(path, dataToWrite)
    if not ok then
        log.error("Failed to write CSV file.")
        return false
    end
    
    -- 2. Read back from the CSV file
    log.info("Reading data back from CSV file...")
    local dataRead = csv.read(path)
    if #dataRead == 0 then
        log.error("Failed to read CSV or file is empty.")
        return false
    end
    
    log.info("Parsed CSV row count: " .. tostring(#dataRead))
    for r = 1, #dataRead do
        local cols = dataRead[r]
        local colTexts = {}
        for c = 1, #cols do
            table.insert(colTexts, "[" .. c .. "]: " .. tostring(cols[c]))
        end
        log.info("Row " .. r .. " -> " .. table.concat(colTexts, " | "))
    end
    
    return true
end
