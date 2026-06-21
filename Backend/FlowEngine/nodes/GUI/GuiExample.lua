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
    gui.config.set("panel1##pn", "panel", "border_radius", 12)

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
    gui.config.set("ctrlPanel##pn", "panel", "border_radius", 8)

    -- Slider control and textinput sync
    gui.widget.create("lblSpeed##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblSpeed##lb", "label", "pos", {10, 10})
    gui.config.set("lblSpeed##lb", "label", "label", "Engine Speed Control")
    gui.config.set("lblSpeed##lb", "label", "foreground_color", {205, 214, 244, 255})

    gui.widget.create("sliderValue##sl", "slider", "ctrlPanel##pn")
    gui.config.set("sliderValue##sl", "slider", "pos", {10, 30})
    gui.config.set("sliderValue##sl", "slider", "size", {130, 20})
    gui.config.set("sliderValue##sl", "slider", "range", {0, 100})
    gui.config.set("sliderValue##sl", "slider", "step", 1)
    gui.config.set("sliderValue##sl", "slider", "data", 65)
    gui.config.set("sliderValue##sl", "slider", "foreground_color", {137, 180, 250, 255})

    gui.widget.create("txtSliderVal##txt", "textinput", "ctrlPanel##pn")
    gui.config.set("txtSliderVal##txt", "textinput", "pos", {150, 30})
    gui.config.set("txtSliderVal##txt", "textinput", "size", {70, 20})
    gui.config.set("txtSliderVal##txt", "textinput", "data", "65")
    gui.config.set("txtSliderVal##txt", "textinput", "foreground_color", {166, 227, 161, 255})
    gui.config.set("txtSliderVal##txt", "textinput", "border_radius", 4)
    gui.config.set("txtSliderVal##txt", "textinput", "hover_color", {49, 50, 68, 255})

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
    gui.config.set("chkEnable##cb", "checkbox", "foreground_color", {245, 194, 231, 255})

    -- Dropdown Control
    gui.widget.create("lblDropdown##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblDropdown##lb", "label", "pos", {10, 105})
    gui.config.set("lblDropdown##lb", "label", "label", "Select Operation Mode:")
    gui.config.set("lblDropdown##lb", "label", "foreground_color", {205, 214, 244, 255})

    gui.widget.create("dropModes##dd", "dropdown", "ctrlPanel##pn")
    gui.config.set("dropModes##dd", "dropdown", "pos", {10, 125})
    gui.config.set("dropModes##dd", "dropdown", "size", {210, 22})
    gui.config.set("dropModes##dd", "dropdown", "menus", {"Safe Mode", "Turbo Mode", "ECO Mode"})
    gui.config.set("dropModes##dd", "dropdown", "data", 1)
    gui.config.set("dropModes##dd", "dropdown", "foreground_color", {249, 226, 175, 255})
    gui.config.set("dropModes##dd", "dropdown", "border_radius", 4)
    gui.config.set("dropModes##dd", "dropdown", "hover_color", {49, 50, 68, 255})

    -- ColorPicker Control
    gui.widget.create("lblPicker##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblPicker##lb", "label", "pos", {10, 170})
    gui.config.set("lblPicker##lb", "label", "label", "Interface Color Theme:")
    gui.config.set("lblPicker##lb", "label", "foreground_color", {205, 214, 244, 255})

    gui.widget.create("clrPicker##cp", "colorpicker", "ctrlPanel##pn")
    gui.config.set("clrPicker##cp", "colorpicker", "pos", {10, 190})
    gui.config.set("clrPicker##cp", "colorpicker", "size", {210, 22})
    gui.config.set("clrPicker##cp", "colorpicker", "border_radius", 4)
    gui.config.set("clrPicker##cp", "colorpicker", "hover_color", {49, 50, 68, 255})

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
    gui.config.set("lblProgress##lb", "label", "foreground_color", {205, 214, 244, 255})

    gui.widget.create("progStatus##pg", "progress", "ctrlPanel##pn")
    gui.config.set("progStatus##pg", "progress", "pos", {10, 255})
    gui.config.set("progStatus##pg", "progress", "size", {210, 18})
    gui.config.set("progStatus##pg", "progress", "data", 35)
    gui.config.set("progStatus##pg", "progress", "foreground_color", {137, 220, 235, 255})
    gui.config.set("progStatus##pg", "progress", "border_radius", 4)

    -- RadioButton Controls (Mutually exclusive plot styling)
    gui.widget.create("lblStyle##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblStyle##lb", "label", "pos", {10, 285})
    gui.config.set("lblStyle##lb", "label", "label", "Select Series Plot Style:")
    gui.config.set("lblStyle##lb", "label", "foreground_color", {205, 214, 244, 255})

    gui.widget.create("Line Style##rb", "radiobutton", "ctrlPanel##pn")
    gui.config.set("Line Style##rb", "radiobutton", "pos", {10, 305})
    gui.config.set("Line Style##rb", "radiobutton", "data", true)
    gui.config.set("Line Style##rb", "radiobutton", "foreground_color", {137, 180, 250, 255})

    gui.widget.create("Scatter Style##rb", "radiobutton", "ctrlPanel##pn")
    gui.config.set("Scatter Style##rb", "radiobutton", "pos", {110, 305})
    gui.config.set("Scatter Style##rb", "radiobutton", "foreground_color", {245, 194, 231, 255})

    gui.widget.create("Bar Style##rb", "radiobutton", "ctrlPanel##pn")
    gui.config.set("Bar Style##rb", "radiobutton", "pos", {10, 327})
    gui.config.set("Bar Style##rb", "radiobutton", "foreground_color", {166, 227, 161, 255})

    -- Style selection callbacks
    gui.config.set("Line Style##rb", "radiobutton", "onchanged", function(checked)
        if checked then
            gui.config.set("Sensor A##ln", "plotline", "plot_type", "line")
            gui.config.set("Threshold##ln", "plotline", "plot_type", "line")
            log.info("Plot style: Line")
        end
    end)
    gui.config.set("Scatter Style##rb", "radiobutton", "onchanged", function(checked)
        if checked then
            gui.config.set("Sensor A##ln", "plotline", "plot_type", "scatter")
            gui.config.set("Threshold##ln", "plotline", "plot_type", "scatter")
            log.info("Plot style: Scatter")
        end
    end)
    gui.config.set("Bar Style##rb", "radiobutton", "onchanged", function(checked)
        if checked then
            gui.config.set("Sensor A##ln", "plotline", "plot_type", "bar")
            gui.config.set("Threshold##ln", "plotline", "plot_type", "bar")
            log.info("Plot style: Bar Chart")
        end
    end)

    -- TextArea Control (Multi-line input log notes)
    gui.widget.create("lblNotes##lb", "label", "ctrlPanel##pn")
    gui.config.set("lblNotes##lb", "label", "pos", {10, 357})
    gui.config.set("lblNotes##lb", "label", "label", "Telemetry Notes:")
    gui.config.set("lblNotes##lb", "label", "foreground_color", {205, 214, 244, 255})

    gui.widget.create("notesArea##txt", "textarea", "ctrlPanel##pn")
    gui.config.set("notesArea##txt", "textarea", "pos", {10, 377})
    gui.config.set("notesArea##txt", "textarea", "size", {210, 45})
    gui.config.set("notesArea##txt", "textarea", "data", "Enter dashboard notes here...\n[Session Initialized]")
    gui.config.set("notesArea##txt", "textarea", "foreground_color", {205, 214, 244, 255})
    gui.config.set("notesArea##txt", "textarea", "border_radius", 4)
    gui.config.set("notesArea##txt", "textarea", "hover_color", {49, 50, 68, 255})
    gui.config.set("notesArea##txt", "textarea", "onchanged", function(text)
        log.info("User notes updated: " .. text)
    end)

    -- =========================================================================
    -- 5. Right Visualizations Panel (Plots column - width 495, height 430)
    -- =========================================================================
    gui.widget.create("visualPanel##pn", "panel", "panel1##pn")
    gui.config.set("visualPanel##pn", "panel", "pos", {270, 40})
    gui.config.set("visualPanel##pn", "panel", "size", {495, 430})
    gui.config.set("visualPanel##pn", "panel", "border_radius", 8)

    -- Plot2D Control with Legend Config
    gui.widget.create("lblPlot2d##lb", "label", "visualPanel##pn")
    gui.config.set("lblPlot2d##lb", "label", "pos", {10, 10})
    gui.config.set("lblPlot2d##lb", "label", "label", "Real-time Telemetry (2D Plot - Multiple Series)")
    gui.config.set("lblPlot2d##lb", "label", "foreground_color", {245, 224, 220, 255})

    gui.widget.create("plotGraph##pl", "plot2d", "visualPanel##pn")
    gui.config.set("plotGraph##pl", "plot2d", "pos", {10, 30})
    gui.config.set("plotGraph##pl", "plot2d", "size", {475, 160})
    gui.config.set("plotGraph##pl", "plot2d", "legend_text_color", {249, 226, 175, 255})
    gui.config.set("plotGraph##pl", "plot2d", "title", "Engine Telemetry History")
    gui.config.set("plotGraph##pl", "plot2d", "title_font_size", 11)
    gui.config.set("plotGraph##pl", "plot2d", "title_color", {137, 180, 250, 255})
    gui.config.set("plotGraph##pl", "plot2d", "border_radius", 6)
    gui.config.set("plotGraph##pl", "plot2d", "grid_visible_x", true)
    gui.config.set("plotGraph##pl", "plot2d", "grid_visible_y", true)
    gui.config.set("plotGraph##pl", "plot2d", "grid_color_x", {70, 70, 95, 255})
    gui.config.set("plotGraph##pl", "plot2d", "grid_color_y", {90, 70, 95, 255})
    gui.config.set("plotGraph##pl", "plot2d", "range_x", { 0.0, 12.0 })
    gui.config.set("plotGraph##pl", "plot2d", "range_y", { 0.0, 100.0 })
    gui.config.set("plotGraph##pl", "plot2d", "tick_interval_x", 2.0)
    gui.config.set("plotGraph##pl", "plot2d", "tick_interval_y", 20.0)

    gui.widget.create("Sensor A##ln", "plotline", "plotGraph##pl")
    gui.config.set("Sensor A##ln", "plotline", "foreground_color", {137, 180, 250, 255})
    gui.config.set("Sensor A##ln", "plotline", "line_thickness", 2.5)
    gui.config.set("Sensor A##ln", "plotline", "marker_style", "circle")
    gui.config.set("Sensor A##ln", "plotline", "marker_size", 6.0)
    gui.config.set("Sensor A##ln", "plotline", "data", {
        x = { 0.0, 1.2, 2.5, 3.8, 5.0, 6.2, 7.5, 8.8, 10.0, 11.2 },
        y = { 10, 25, 45, 30, 60, 40, 75, 55, 90, 80 }
    })

    gui.widget.create("Threshold##ln", "plotline", "plotGraph##pl")
    gui.config.set("Threshold##ln", "plotline", "foreground_color", {243, 139, 168, 255})
    gui.config.set("Threshold##ln", "plotline", "line_thickness", 1.5)
    gui.config.set("Threshold##ln", "plotline", "line_style", "dashed")
    gui.config.set("Threshold##ln", "plotline", "data", {
        x = { 0.0, 2.0, 4.0, 6.0, 8.0, 10.0, 12.0 },
        y = { 45, 45, 50, 50, 55, 55, 60 }
    })

    -- Plot3D Control with Legend Config (and Camera rotation + scroll zoom)
    gui.widget.create("lblPlot3d##lb", "label", "visualPanel##pn")
    gui.config.set("lblPlot3d##lb", "label", "pos", {10, 210})
    gui.config.set("lblPlot3d##lb", "label", "label", "Peak Distribution (3D Plot - Rotate/Zoom)")
    gui.config.set("lblPlot3d##lb", "label", "foreground_color", {245, 224, 220, 255})

    gui.widget.create("plot3dGraph##p3", "plot3d", "visualPanel##pn")
    gui.config.set("plot3dGraph##p3", "plot3d", "pos", {10, 230})
    gui.config.set("plot3dGraph##p3", "plot3d", "size", {475, 190})
    gui.config.set("plot3dGraph##p3", "plot3d", "legend_text_color", {249, 226, 175, 255})
    gui.config.set("plot3dGraph##p3", "plot3d", "title", "Spatial Distribution Cage")
    gui.config.set("plot3dGraph##p3", "plot3d", "title_font_size", 11)
    gui.config.set("plot3dGraph##p3", "plot3d", "title_color", {166, 227, 161, 255})
    gui.config.set("plot3dGraph##p3", "plot3d", "border_radius", 6)
    gui.config.set("plot3dGraph##p3", "plot3d", "grid_visible_x", true)
    gui.config.set("plot3dGraph##p3", "plot3d", "grid_visible_y", true)
    gui.config.set("plot3dGraph##p3", "plot3d", "grid_visible_z", true)
    gui.config.set("plot3dGraph##p3", "plot3d", "grid_color_x", {70, 70, 95, 255})
    gui.config.set("plot3dGraph##p3", "plot3d", "grid_color_y", {70, 70, 95, 255})
    gui.config.set("plot3dGraph##p3", "plot3d", "grid_color_z", {166, 227, 161, 100})
    gui.config.set("plot3dGraph##p3", "plot3d", "range_x", { 0.0, 4.0 })
    gui.config.set("plot3dGraph##p3", "plot3d", "range_y", { 0.0, 4.0 })
    gui.config.set("plot3dGraph##p3", "plot3d", "range_z", { 0.0, 100.0 })
    gui.config.set("plot3dGraph##p3", "plot3d", "tick_interval_x", 1.0)
    gui.config.set("plot3dGraph##p3", "plot3d", "tick_interval_y", 1.0)
    gui.config.set("plot3dGraph##p3", "plot3d", "tick_interval_z", 25.0)
    gui.config.set("plot3dGraph##p3", "plot3d", "snaps_orientation", "vertical")
    gui.config.set("plot3dGraph##p3", "plot3d", "snaps_text_color", {137, 180, 250, 255})
    gui.config.set("plot3dGraph##p3", "plot3d", "snaps_background_color", {45, 45, 70, 200})

    
    local peakGrid1 = {
        {10, 12, 15, 12, 10},
        {12, 20, 35, 20, 12},
        {15, 35, 80, 35, 15},
        {12, 20, 35, 20, 12},
        {10, 12, 15, 12, 10}
    }
    gui.widget.create("Channel Alpha##ln", "plotline", "plot3dGraph##p3")
    gui.config.set("Channel Alpha##ln", "plotline", "foreground_color", {137, 180, 250, 255})
    gui.config.set("Channel Alpha##ln", "plotline", "data", peakGrid1)

    local peakGrid2 = {
        {20, 18, 15, 18, 20},
        {18, 25, 30, 25, 18},
        {15, 30, 50, 30, 15},
        {18, 25, 30, 25, 18},
        {20, 18, 15, 18, 20}
    }
    gui.widget.create("Channel Beta##ln", "plotline", "plot3dGraph##p3")
    gui.config.set("Channel Beta##ln", "plotline", "foreground_color", {166, 227, 161, 255})
    gui.config.set("Channel Beta##ln", "plotline", "data", peakGrid2)

    -- 3D Path Plotline using 1D coordinates (x, y, z)
    gui.widget.create("Robot Path##ln", "plotline", "plot3dGraph##p3")
    gui.config.set("Robot Path##ln", "plotline", "foreground_color", {249, 226, 175, 255})
    gui.config.set("Robot Path##ln", "plotline", "line_thickness", 3.0)
    gui.config.set("Robot Path##ln", "plotline", "line_style", "dotted")
    gui.config.set("Robot Path##ln", "plotline", "data", {
        x = { 0.0, 1.0, 2.0, 3.0, 4.0 },
        y = { 0.0, 1.0, 2.0, 1.0, 0.0 },
        z = { 10.0, 30.0, 60.0, 40.0, 15.0 }
    })

    -- =========================================================================
    -- 6. Horizontal Button Bar Panel (demonstrating horizontal arrangement!)
    -- =========================================================================
    gui.widget.create("bottomBar##pn", "panel", "panel1##pn")
    gui.config.set("bottomBar##pn", "panel", "pos", {15, 485})
    gui.config.set("bottomBar##pn", "panel", "size", {480, 45})
    gui.config.set("bottomBar##pn", "panel", "horizontal", true)
    gui.config.set("bottomBar##pn", "panel", "border_radius", 6)

    -- Add buttons sequentially. Because horizontal is true, they stack left-to-right!
    gui.widget.create("btnUpdate##btn", "button", "bottomBar##pn")
    gui.config.set("btnUpdate##btn", "button", "size", {120, 25})
    gui.config.set("btnUpdate##btn", "button", "label", "Apply Config")
    gui.config.set("btnUpdate##btn", "button", "foreground_color", {166, 227, 161, 255})
    gui.config.set("btnUpdate##btn", "button", "border_radius", 6)
    gui.config.set("btnUpdate##btn", "button", "background_color", {166, 227, 161, 25})
    gui.config.set("btnUpdate##btn", "button", "hover_color", {166, 227, 161, 80})

    gui.widget.create("btnReset##btn", "button", "bottomBar##pn")
    gui.config.set("btnReset##btn", "button", "size", {120, 25})
    gui.config.set("btnReset##btn", "button", "label", "Reset Stats")
    gui.config.set("btnReset##btn", "button", "foreground_color", {243, 139, 168, 255})
    gui.config.set("btnReset##btn", "button", "border_radius", 6)
    gui.config.set("btnReset##btn", "button", "background_color", {243, 139, 168, 25})
    gui.config.set("btnReset##btn", "button", "hover_color", {243, 139, 168, 80})

    gui.widget.create("btnHelp##btn", "button", "bottomBar##pn")
    gui.config.set("btnHelp##btn", "button", "size", {120, 25})
    gui.config.set("btnHelp##btn", "button", "label", "Show Help")
    gui.config.set("btnHelp##btn", "button", "foreground_color", {137, 220, 235, 255})
    gui.config.set("btnHelp##btn", "button", "border_radius", 6)
    gui.config.set("btnHelp##btn", "button", "background_color", {137, 220, 235, 25})
    gui.config.set("btnHelp##btn", "button", "hover_color", {137, 220, 235, 80})

    -- Setup event listeners for the horizontal layout buttons
    gui.config.set("btnUpdate##btn", "button", "onclick", function()
        gui.config.set("progStatus##pg", "progress", "data", 85)
        local randomY1 = {}
        local randomY2 = {}
        for i=1,10 do
            table.insert(randomY1, math.random(10, 90))
        end
        for i=1,7 do
            table.insert(randomY2, math.random(40, 60))
        end

        gui.config.set("Sensor A##ln", "plotline", "data", {
            x = { 0.0, 1.2, 2.5, 3.8, 5.0, 6.2, 7.5, 8.8, 10.0, 11.2 },
            y = randomY1
        })
        gui.config.set("Threshold##ln", "plotline", "data", {
            x = { 0.0, 2.0, 4.0, 6.0, 8.0, 10.0, 12.0 },
            y = randomY2
        })

        local newGrid1 = {}
        local newGrid2 = {}
        for r=1,5 do
            local row1 = {}
            local row2 = {}
            for c=1,5 do
                table.insert(row1, math.random(5, 75))
                table.insert(row2, math.random(10, 60))
            end
            table.insert(newGrid1, row1)
            table.insert(newGrid2, row2)
        end
        gui.config.set("Channel Alpha##ln", "plotline", "data", newGrid1)
        gui.config.set("Channel Beta##ln", "plotline", "data", newGrid2)

        local randomZ = {}
        for i=1,5 do
            table.insert(randomZ, math.random(10, 80))
        end
        gui.config.set("Robot Path##ln", "plotline", "data", {
            x = { 0.0, 1.0, 2.0, 3.0, 4.0 },
            y = { 0.0, 1.0, 2.0, 1.0, 0.0 },
            z = randomZ
        })

        log.info("System configuration successfully applied.")
    end)

    gui.config.set("btnReset##btn", "button", "onclick", function()
        gui.config.set("progStatus##pg", "progress", "data", 0)
        gui.config.set("Sensor A##ln", "plotline", "data", { x = {0}, y = {0} })
        gui.config.set("Threshold##ln", "plotline", "data", { x = {0}, y = {0} })

        local zeroGrid = {
            {0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0}
        }
        gui.config.set("Channel Alpha##ln", "plotline", "data", zeroGrid)
        gui.config.set("Channel Beta##ln", "plotline", "data", zeroGrid)
        gui.config.set("Robot Path##ln", "plotline", "data", { x = {0}, y = {0}, z = {0} })
        log.warn("Statistics reset to baseline.")
    end)

    gui.config.set("btnHelp##btn", "button", "onclick", function()
        log.info("Help: Right-click and drag on the 3D Plot to rotate the camera angles! Use Scroll Wheel to Zoom in/out.")
    end)

    -- =========================================================================
    -- 7. Live Camera Feed Image View / Dummy image if no input
    -- =========================================================================
    local displayImg = src
    local needRelease = false
    if not (displayImg and type(displayImg) == "userdata" and not displayImg:empty()) then
        -- Create a dummy matrix to showcase the image widget!
        displayImg = cv.Mat(120, 180, cv.CV_8UC3)
        needRelease = true
        -- Draw a nice placeholder pattern on it
        cv.rectangle(displayImg, 5, 5, 175, 115, {137, 180, 250}, -1) -- filled background light blue
        cv.rectangle(displayImg, 10, 10, 170, 110, {30, 30, 46}, -1)  -- filled inner dark blue
        cv.circle(displayImg, 90, 60, 25, {245, 194, 231}, -1)       -- pink circle
        cv.circle(displayImg, 90, 60, 15, {166, 227, 161}, -1)       -- green circle
    end

    gui.widget.create("lblImage##lb", "label", "panel1##pn")
    gui.config.set("lblImage##lb", "label", "pos", {510, 485})
    gui.config.set("lblImage##lb", "label", "label", "Visual Feed:")
    gui.config.set("lblImage##lb", "label", "foreground_color", {205, 214, 244, 255})

    gui.widget.create("imgView##im", "image", "panel1##pn")
    gui.config.set("imgView##im", "image", "pos", {590, 485})
    gui.config.set("imgView##im", "image", "size", {120, 80})
    gui.config.set("imgView##im", "image", "border_radius", 6)
    gui.config.set("imgView##im", "image", "data", displayImg)

    if needRelease then
        displayImg:release()
    end
end
