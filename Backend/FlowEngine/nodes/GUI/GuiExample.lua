-- @node: GuiExample
-- @description: dynamic GUI dialog and widgets 예제입니다.
-- @input: src : table
function show_gui(src : table)
    -- Loop counter variable declaration to satisfy strict static analysis scoping checks.
    -- This local declaration prevents implicit global declarations in loops.
    local i

    -- =========================================================================
    -- 1. Create and Configure Dialog Window
    -- =========================================================================
    local dlgName = "Dashboard##dlg1"
    gui.dialog.create(dlgName)
    gui.config.set(dlgName, "dialog", "size", {600, 480})
    gui.config.set(dlgName, "dialog", "background_color", {30, 30, 46, 255})
    gui.dialog.show(dlgName, true)

    -- =========================================================================
    -- 2. Create Main Panel Container
    -- =========================================================================
    gui.widget.create("panel1##pn", "panel", dlgName)
    gui.config.set("panel1##pn", "panel", "size", {580, 460})
    gui.config.set("panel1##pn", "panel", "pos", {10, 10})

    -- =========================================================================
    -- 3. Create Title Label
    -- =========================================================================
    gui.widget.create("lblTitle##lb", "label", "panel1##pn")
    gui.config.set("lblTitle##lb", "label", "pos", {15, 10})
    gui.config.set("lblTitle##lb", "label", "label", "System Configuration Dashboard")
    gui.config.set("lblTitle##lb", "label", "foreground_color", {137, 180, 250, 255})

    -- =========================================================================
    -- 4. Left Control Panel (Vertical Panel)
    -- =========================================================================
    gui.widget.create("ctrlPanel##pn", "panel", "panel1##pn")
    gui.config.set("ctrlPanel##pn", "panel", "pos", {15, 40})
    gui.config.set("ctrlPanel##pn", "panel", "size", {260, 280})
    gui.config.set("ctrlPanel##pn", "panel", "background_color", {45, 45, 70, 100})

    -- Sliders and inputs inside left panel
    gui.widget.create("lblSpeed##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblSpeed##lb", "label", "pos", {10, 10})
    gui.config.set("lblSpeed##lb", "label", "label", "Engine Speed Control")

    gui.widget.create("sliderValue##sl", "slider", "ctrlPanel##pn")
    gui.config.set("sliderValue##sl", "slider", "pos", {10, 30})
    gui.config.set("sliderValue##sl", "slider", "range", {0, 100})
    gui.config.set("sliderValue##sl", "slider", "step", 1)
    gui.config.set("sliderValue##sl", "slider", "data", 65)

    gui.widget.create("txtSliderVal##txt", "textinput", "ctrlPanel##pn")
    gui.config.set("txtSliderVal##txt", "textinput", "pos", {140, 30})
    gui.config.set("txtSliderVal##txt", "textinput", "size", {50, 20})
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

    -- Checkbox & Dropdown inside left panel
    gui.widget.create("chkEnable##cb", "checkbox", "ctrlPanel##pn")
    gui.config.set("chkEnable##cb", "checkbox", "pos", {10, 65})
    gui.config.set("chkEnable##cb", "checkbox", "label", "Automatic Calibration")
    gui.config.set("chkEnable##cb", "checkbox", "data", true)

    gui.widget.create("dropModes##dd", "dropdown", "ctrlPanel##pn")
    gui.config.set("dropModes##dd", "dropdown", "pos", {10, 95})
    gui.config.set("dropModes##dd", "dropdown", "menus", {"Safe Mode", "Turbo Mode", "ECO Mode"})
    gui.config.set("dropModes##dd", "dropdown", "data", 1)

    -- ColorPicker inside left panel
    gui.widget.create("lblPicker##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblPicker##lb", "label", "pos", {10, 135})
    gui.config.set("lblPicker##lb", "label", "label", "Theme Color Picker")

    gui.widget.create("clrPicker##cp", "colorpicker", "ctrlPanel##pn")
    gui.config.set("clrPicker##cp", "colorpicker", "pos", {10, 155})
    gui.config.set("clrPicker##cp", "colorpicker", "size", {60, 22})

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

    -- Progress bar inside left panel
    gui.widget.create("progStatus##pg", "progress", "ctrlPanel##pn")
    gui.config.set("progStatus##pg", "progress", "pos", {10, 195})
    gui.config.set("progStatus##pg", "progress", "size", {180, 15})
    gui.config.set("progStatus##pg", "progress", "data", 35)

    -- =========================================================================
    -- 5. Right Panel (Visualizations - Plots)
    -- =========================================================================
    gui.widget.create("visualPanel##pn", "panel", "panel1##pn")
    gui.config.set("visualPanel##pn", "panel", "pos", {290, 40})
    gui.config.set("visualPanel##pn", "panel", "size", {275, 280})

    -- Plot2D
    gui.widget.create("plotGraph##pl", "plot2d", "visualPanel##pn")
    gui.config.set("plotGraph##pl", "plot2d", "pos", {10, 10})
    gui.config.set("plotGraph##pl", "plot2d", "size", {250, 110})
    gui.config.set("plotGraph##pl", "plot2d", "data", {10, 25, 45, 30, 60, 40, 75, 55, 90, 80})

    -- Plot3D with Right-click Rotate Note
    gui.widget.create("plot3dGraph##p3", "plot3d", "visualPanel##pn")
    gui.config.set("plot3dGraph##p3", "plot3d", "pos", {10, 130})
    gui.config.set("plot3dGraph##p3", "plot3d", "size", {250, 140})
    
    local peakGrid = {
        {10, 12, 15, 12, 10},
        {12, 20, 35, 20, 12},
        {15, 35, 80, 35, 15},
        {12, 20, 35, 20, 12},
        {10, 12, 15, 12, 10}
    }
    gui.config.set("plot3dGraph##p3", "plot3d", "data", peakGrid)

    -- =========================================================================
    -- 6. Horizontal Button Bar Panel (demonstrating horizontal arrangement!)
    -- =========================================================================
    gui.widget.create("bottomBar##pn", "panel", "panel1##pn")
    gui.config.set("bottomBar##pn", "panel", "pos", {15, 335})
    gui.config.set("bottomBar##pn", "panel", "size", {550, 45})
    -- Set horizontal layout alignment to true!
    gui.config.set("bottomBar##pn", "panel", "horizontal", true)

    -- Add buttons sequentially. Because horizontal is true, they stack left-to-right!
    gui.widget.create("btnUpdate##btn", "button", "bottomBar##pn")
    gui.config.set("btnUpdate##btn", "button", "size", {90, 25})
    gui.config.set("btnUpdate##btn", "button", "label", "Apply Config")

    gui.widget.create("btnReset##btn", "button", "bottomBar##pn")
    gui.config.set("btnReset##btn", "button", "size", {90, 25})
    gui.config.set("btnReset##btn", "button", "label", "Reset Stats")

    gui.widget.create("btnHelp##btn", "button", "bottomBar##pn")
    gui.config.set("btnHelp##btn", "button", "size", {90, 25})
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
        log.info("Help: Right-click and drag on the 3D Plot to rotate the camera angles!")
    end)

    -- =========================================================================
    -- 7. Live Camera Feed Image View (optional input)
    -- =========================================================================
    -- Check if src is passed and verify it is a valid userdata (MatWrapper) object
    if src and type(src) == "userdata" and not src:empty() then
        gui.widget.create("imgView##im", "image", "panel1##pn")
        gui.config.set("imgView##im", "image", "pos", {15, 390})
        gui.config.set("imgView##im", "image", "size", {90, 60})
        gui.config.set("imgView##im", "image", "data", src)
    end
end
