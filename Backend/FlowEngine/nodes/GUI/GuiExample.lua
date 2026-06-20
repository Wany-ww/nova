-- @node: GuiExample
-- @description: dynamic GUI dialog and widgets 예제입니다.
-- @input: src : table
function show_gui(src : table)
    -- Loop counter variable declaration to satisfy strict static analysis scoping checks.
    -- This local declaration prevents implicit global declarations in loops.
    local i

    -- =========================================================================
    -- 1. Create and Configure Dialog Window (Resized to 800x600)
    -- =========================================================================
    local dlgName = "Dashboard##dlg1"
    gui.dialog.create(dlgName)
    gui.config.set(dlgName, "dialog", "size", {800, 600})
    gui.config.set(dlgName, "dialog", "background_color", {30, 30, 46, 255})
    gui.dialog.show(dlgName, true)

    -- =========================================================================
    -- 2. Create Main Panel Container (Resized to 780x580)
    -- =========================================================================
    gui.widget.create("panel1##pn", "panel", dlgName)
    gui.config.set("panel1##pn", "panel", "size", {780, 580})
    gui.config.set("panel1##pn", "panel", "pos", {10, 10})

    -- =========================================================================
    -- 3. Create Title Label (Visual Enhancement)
    -- =========================================================================
    gui.widget.create("lblTitle##lb", "label", "panel1##pn")
    gui.config.set("lblTitle##lb", "label", "pos", {15, 10})
    gui.config.set("lblTitle##lb", "label", "label", "System Configuration Dashboard")
    gui.config.set("lblTitle##lb", "label", "foreground_color", {137, 180, 250, 255})

    -- =========================================================================
    -- 4. Left Control Panel (Vertical Panel - width 240, height 430)
    -- =========================================================================
    gui.widget.create("ctrlPanel##pn", "panel", "panel1##pn")
    gui.config.set("ctrlPanel##pn", "panel", "pos", {15, 40})
    gui.config.set("ctrlPanel##pn", "panel", "size", {240, 430})
    gui.config.set("ctrlPanel##pn", "panel", "background_color", {45, 45, 70, 100})

    -- Slider control and textinput sync
    gui.widget.create("lblSpeed##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblSpeed##lb", "label", "pos", {10, 10})
    gui.config.set("lblSpeed##lb", "label", "label", "Engine Speed Control")

    gui.widget.create("sliderValue##sl", "slider", "ctrlPanel##pn")
    gui.config.set("sliderValue##sl", "slider", "pos", {10, 30})
    gui.config.set("sliderValue##sl", "slider", "size", {130, 20})
    gui.config.set("sliderValue##sl", "slider", "range", {0, 100})
    gui.config.set("sliderValue##sl", "slider", "step", 1)
    gui.config.set("sliderValue##sl", "slider", "data", 65)

    gui.widget.create("txtSliderVal##txt", "textinput", "ctrlPanel##pn")
    gui.config.set("txtSliderVal##txt", "textinput", "pos", {150, 30})
    gui.config.set("txtSliderVal##txt", "textinput", "size", {70, 20})
    gui.config.set("txtSliderVal##txt", "textinput", "data", "65")

    -- Slider-Textinput Sync
    gui.config.set("sliderValue##sl", "slider", "onchanged", function(val)
        gui.config.set("txtSliderVal##txt", "textinput", "data", tostring(val))
        log.info("Slider changed: " .. tostring(val))
    end)

    gui.config.set("txtSliderVal##txt", "textinput", "onchanged", function(val)
        local num = tonumber(val)
        if num and num >= 0 and num <= 100 then
            gui.config.set("sliderValue##sl", "slider", "data", num)
            log.info("TextInput changed slider to: " .. tostring(num))
        end
    end)

    -- Checkbox Control
    gui.widget.create("chkEnable##cb", "checkbox", "ctrlPanel##pn")
    gui.config.set("chkEnable##cb", "checkbox", "pos", {10, 70})
    gui.config.set("chkEnable##cb", "checkbox", "label", "Automatic Calibration")
    gui.config.set("chkEnable##cb", "checkbox", "data", true)

    -- Dropdown Control
    gui.widget.create("lblDropdown##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblDropdown##lb", "label", "pos", {10, 105})
    gui.config.set("lblDropdown##lb", "label", "label", "Select Operation Mode:")

    gui.widget.create("dropModes##dd", "dropdown", "ctrlPanel##pn")
    gui.config.set("dropModes##dd", "dropdown", "pos", {10, 125})
    gui.config.set("dropModes##dd", "dropdown", "size", {210, 22})
    gui.config.set("dropModes##dd", "dropdown", "menus", {"Safe Mode", "Turbo Mode", "ECO Mode"})
    gui.config.set("dropModes##dd", "dropdown", "data", 1)

    -- ColorPicker Control
    gui.widget.create("lblPicker##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblPicker##lb", "label", "pos", {10, 170})
    gui.config.set("lblPicker##lb", "label", "label", "Interface Color Theme:")

    gui.widget.create("clrPicker##cp", "colorpicker", "ctrlPanel##pn")
    gui.config.set("clrPicker##cp", "colorpicker", "pos", {10, 190})
    gui.config.set("clrPicker##cp", "colorpicker", "size", {210, 22})

    -- Dynamically update backgrounds and labels when color is picked
    gui.config.set("clrPicker##cp", "colorpicker", "onchanged", function(colorTable)
        if colorTable and type(colorTable) == "table" then
            local r = colorTable[1] or 255
            local g = colorTable[2] or 255
            local b = colorTable[3] or 255
            local a = colorTable[4] or 255
            
            gui.config.set(dlgName, "dialog", "background_color", {r, g, b, a})
            gui.config.set("lblTitle##lb", "label", "foreground_color", {r, g, b, 255})
            log.info("Theme color updated to RGB: " .. r .. "," .. g .. "," .. b)
        end
    end)

    -- Progress Bar Control
    gui.widget.create("lblProgress##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblProgress##lb", "label", "pos", {10, 235})
    gui.config.set("lblProgress##lb", "label", "label", "Calibration Progress:")

    gui.widget.create("progStatus##pg", "progress", "ctrlPanel##pn")
    gui.config.set("progStatus##pg", "progress", "pos", {10, 255})
    gui.config.set("progStatus##pg", "progress", "size", {210, 18})
    gui.config.set("progStatus##pg", "progress", "data", 35)

    -- =========================================================================
    -- 5. Right Visualizations Panel (Plots column - width 495, height 430)
    -- =========================================================================
    gui.widget.create("visualPanel##pn", "panel", "panel1##pn")
    gui.config.set("visualPanel##pn", "panel", "pos", {270, 40})
    gui.config.set("visualPanel##pn", "panel", "size", {495, 430})

    -- Plot2D Control with Legend Config
    gui.widget.create("lblPlot2d##lb", "label", "visualPanel##pn")
    gui.config.set("lblPlot2d##lb", "label", "pos", {10, 10})
    gui.config.set("lblPlot2d##lb", "label", "label", "Real-time Telemetry (2D Plot)")

    gui.widget.create("plotGraph##pl", "plot2d", "visualPanel##pn")
    gui.config.set("plotGraph##pl", "plot2d", "pos", {10, 30})
    gui.config.set("plotGraph##pl", "plot2d", "size", {475, 160})
    gui.config.set("plotGraph##pl", "plot2d", "data", {10, 25, 45, 30, 60, 40, 75, 55, 90, 80})
    gui.config.set("plotGraph##pl", "plot2d", "legend", "Telemetry (X-axis: Epochs, Y-axis: Value)")

    -- Plot3D Control with Legend Config (and Camera rotation + scroll zoom)
    gui.widget.create("lblPlot3d##lb", "label", "visualPanel##pn")
    gui.config.set("lblPlot3d##lb", "label", "pos", {10, 210})
    gui.config.set("lblPlot3d##lb", "label", "label", "Peak Distribution (3D Plot - Right-drag to Rotate, Wheel to Zoom)")

    gui.widget.create("plot3dGraph##p3", "plot3d", "visualPanel##pn")
    gui.config.set("plot3dGraph##p3", "plot3d", "pos", {10, 230})
    gui.config.set("plot3dGraph##p3", "plot3d", "size", {475, 190})
    
    local peakGrid = {
        {10, 12, 15, 12, 10},
        {12, 20, 35, 20, 12},
        {15, 35, 80, 35, 15},
        {12, 20, 35, 20, 12},
        {10, 12, 15, 12, 10}
    }
    gui.config.set("plot3dGraph##p3", "plot3d", "data", peakGrid)
    gui.config.set("plot3dGraph##p3", "plot3d", "legend", "Peak Density (3D Wireframe)")

    -- =========================================================================
    -- 6. Horizontal Button Bar Panel (demonstrating horizontal arrangement!)
    -- =========================================================================
    gui.widget.create("bottomBar##pn", "panel", "panel1##pn")
    gui.config.set("bottomBar##pn", "panel", "pos", {15, 485})
    gui.config.set("bottomBar##pn", "panel", "size", {480, 45})
    gui.config.set("bottomBar##pn", "panel", "horizontal", true)

    -- Add buttons sequentially. Because horizontal is true, they stack left-to-right!
    gui.widget.create("btnUpdate##btn", "button", "bottomBar##pn")
    gui.config.set("btnUpdate##btn", "button", "size", {120, 25})
    gui.config.set("btnUpdate##btn", "button", "label", "Apply Config")

    gui.widget.create("btnReset##btn", "button", "bottomBar##pn")
    gui.config.set("btnReset##btn", "button", "size", {120, 25})
    gui.config.set("btnReset##btn", "button", "label", "Reset Stats")

    gui.widget.create("btnHelp##btn", "button", "bottomBar##pn")
    gui.config.set("btnHelp##btn", "button", "size", {120, 25})
    gui.config.set("btnHelp##btn", "button", "label", "Show Help")

    -- Setup event listeners for the horizontal layout buttons
    gui.config.set("btnUpdate##btn", "button", "onclick", function()
        gui.config.set("progStatus##pg", "progress", "data", 85)
        local randomPts = {}
        for i=1,10 do
            table.insert(randomPts, math.random(10, 90))
        end
        gui.config.set("plotGraph##pl", "plot2d", "data", randomPts)
        log.info("System configuration successfully applied.")
    end)

    gui.config.set("btnReset##btn", "button", "onclick", function()
        gui.config.set("progStatus##pg", "progress", "data", 0)
        gui.config.set("plotGraph##pl", "plot2d", "data", {0, 0, 0, 0, 0, 0, 0, 0, 0, 0})
        log.warn("Statistics reset to baseline.")
    end)

    gui.config.set("btnHelp##btn", "button", "onclick", function()
        log.info("Help: Right-click and drag on the 3D Plot to rotate the camera angles! Use Scroll Wheel to Zoom in/out.")
    end)

    -- =========================================================================
    -- 7. Live Camera Feed Image View / Dummy image if no input
    -- =========================================================================
    local displayImg = src
    if not (displayImg and type(displayImg) == "userdata" and not displayImg:empty()) then
        -- Create a dummy matrix to showcase the image widget!
        displayImg = cv.Mat(120, 180, cv.CV_8UC3)
        -- Draw a nice placeholder pattern on it
        cv.rectangle(displayImg, 5, 5, 175, 115, {137, 180, 250}, -1) -- filled background light blue
        cv.rectangle(displayImg, 10, 10, 170, 110, {30, 30, 46}, -1)  -- filled inner dark blue
        cv.circle(displayImg, 90, 60, 25, {245, 194, 231}, -1)       -- pink circle
        cv.circle(displayImg, 90, 60, 15, {166, 227, 161}, -1)       -- green circle
    end

    gui.widget.create("lblImage##lb", "label", "panel1##pn")
    gui.config.set("lblImage##lb", "label", "pos", {510, 485})
    gui.config.set("lblImage##lb", "label", "label", "Visual Feed:")

    gui.widget.create("imgView##im", "image", "panel1##pn")
    gui.config.set("imgView##im", "image", "pos", {590, 485})
    gui.config.set("imgView##im", "image", "size", {120, 80})
    gui.config.set("imgView##im", "image", "data", displayImg)
end
