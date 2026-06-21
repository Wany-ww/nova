-- Camera API Verification Node

function main()
    local i
    log.info("Starting Camera API Verification...")

    -- 1. Create a simulated camera
    local cam = camera.create("sim", "", 0)
    cam:init()
    log.info("Simulated camera initialized.")

    -- 2. Test Bayer settings
    cam:set_bayer(1)
    local bayer = cam:get_bayer()
    log.info("Bayer pattern set & retrieved: " .. tostring(bayer))

    -- 3. Load simulated image
    -- Since we don't have a specific test.jpg inside Reference yet, we can use 
    -- the global path to copy test.jpg or just load a dummy raw image if we generate it.
    -- For testing, we will check if loading is successful.
    local ok = cam:load_image("test.jpg", 640, 480, 8)
    log.info("Load dummy image status (should be false for cfg): " .. tostring(ok))

    -- Let's copy a real test image from developments folder to Reference folder for this test
    -- The Developments/nova/test.jpg can be copied or we can generate a small RAW image.
    -- Let's test with a generated RAW file! We can write a raw file in C# or Lua, or python.
    -- For now, let's proceed with simulating the capture loop.
    cam:run()
    log.info("Camera loop started. Running real-time display dialog...")

    -- Wait to capture a few frames
    for i = 1, 5 do
        time.sleep.ms(100) -- Use correct time.sleep.ms API
        local frames = cam:get_frame("rgb", 1)
        if frames and #frames > 0 then
            log.info("Captured frame " .. i .. " size: " .. tostring(frames[1].width) .. "x" .. tostring(frames[1].height))
        else
            log.info("No frame captured at iteration " .. i)
        end
    end

    -- 4. Stop and release
    cam:stop()
    cam:close()
    log.info("Camera test completed and resources released!")
end
