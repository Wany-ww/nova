-- @node: GuiExample
-- @description: dynamic GUI dialog and widgets 예제입니다.
function show_gui()
    -- Loop counter variable declaration to satisfy strict static analysis scoping checks.
    local i

    -- ==========================================
    -- 1. Create Dialog Window
    -- ==========================================
    -- gui.dialog.create initializes a docking-enabled window in the workspace.
    -- The suffix "##dlg1" acts as a unique ID while hiding itself from visual labels.
    local dlgName = "Dashboard##dlg1"
    gui.dialog.create(dlgName)
    
    -- gui.dialog.show reveals the created layout on the screen and shifts focus to it.
    gui.dialog.show(dlgName, true)

    -- ==========================================
    -- 2. Create Panel Container
    -- ==========================================
    -- Panels act as border containers to organize child widgets.
    -- Here we create "panel1##pn" as a child of the root dialog.
    gui.widget.create("panel1##pn", "panel", dlgName)
    
    -- Set width (360) and height (240) in pixels.
    gui.config.set("panel1##pn", "panel", "size", {360, 240})
    
    -- Set relative position {x, y} from the top-left of the parent dialog.
    gui.config.set("panel1##pn", "panel", "pos", {10, 10})

    -- ==========================================
    -- 3. Create Title Label
    -- ==========================================
    -- Create a TextBlock label as a child of the panel widget.
    gui.widget.create("lblTitle##lb", "label", "panel1##pn")
    gui.config.set("lblTitle##lb", "label", "pos", {10, 10})
    
    -- Config key "label" modifies the visible content.
    gui.config.set("lblTitle##lb", "label", "label", "Control Panel")
    
    -- Customize colors using {Red, Green, Blue, Alpha} structure (0-255).
    gui.config.set("lblTitle##lb", "label", "foreground_color", {137, 180, 250, 255})

    -- ==========================================
    -- 4. Create Slider Widget
    -- ==========================================
    -- Create a slider range bar.
    gui.widget.create("sliderValue##sl", "slider", "panel1##pn")
    gui.config.set("sliderValue##sl", "slider", "pos", {10, 35})
    
    -- Configure minimum and maximum value ranges.
    gui.config.set("sliderValue##sl", "slider", "range", {0, 100})
    
    -- Define step increments for slider movements.
    gui.config.set("sliderValue##sl", "slider", "step", 1)
    
    -- Set the initial value.
    gui.config.set("sliderValue##sl", "slider", "data", 50)
    
    -- ==========================================
    -- 5. Create Textbox for tracking
    -- ==========================================
    -- Textboxes allow standard textual inputs.
    gui.widget.create("txtSliderVal##txt", "textinput", "panel1##pn")
    gui.config.set("txtSliderVal##txt", "textinput", "pos", {140, 35})
    gui.config.set("txtSliderVal##txt", "textinput", "size", {50, 20})
    gui.config.set("txtSliderVal##txt", "textinput", "data", "50")

    -- Sync slider modifications directly with the textinput box via onchanged event.
    gui.config.set("sliderValue##sl", "slider", "onchanged", function(val)
        gui.config.set("txtSliderVal##txt", "textinput", "data", tostring(val))
        log.info("Slider value changed to: " .. tostring(val))
    end)

    -- ==========================================
    -- 6. Create Checkbox Widget
    -- ==========================================
    -- Checkboxes offer binary true/false switches.
    gui.widget.create("chkEnable##cb", "checkbox", "panel1##pn")
    gui.config.set("chkEnable##cb", "checkbox", "pos", {10, 65})
    gui.config.set("chkEnable##cb", "checkbox", "label", "Enable Laser")
    gui.config.set("chkEnable##cb", "checkbox", "data", false)
    gui.config.set("chkEnable##cb", "checkbox", "onchanged", function(checked)
        log.info("Laser Enabled: " .. tostring(checked))
    end)

    -- ==========================================
    -- 7. Create Dropdown Choice Menu
    -- ==========================================
    -- Dropdowns offer a selection of multiple values.
    gui.widget.create("dropModes##dd", "dropdown", "panel1##pn")
    gui.config.set("dropModes##dd", "dropdown", "pos", {10, 95})
    
    -- Set the selectable options using standard array strings.
    gui.config.set("dropModes##dd", "dropdown", "menus", {"Idle", "Normal", "Turbo"})
    
    -- Set the initial selected choice index (1-based index).
    gui.config.set("dropModes##dd", "dropdown", "data", 2)
    gui.config.set("dropModes##dd", "dropdown", "onchanged", function(idx)
        local modes = {"Idle", "Normal", "Turbo"}
        log.info("Mode changed to: " .. modes[idx])
    end)

    -- ==========================================
    -- 8. Create Plot2D Graph
    -- ==========================================
    -- Plot2D renders real-time numeric line charts inside a customized panel box.
    gui.widget.create("plotGraph##pl", "plot2d", "panel1##pn")
    gui.config.set("plotGraph##pl", "plot2d", "pos", {10, 130})
    gui.config.set("plotGraph##pl", "plot2d", "size", {150, 80})
    
    -- Pass a list of numbers to plot as coordinates on the Y axis.
    gui.config.set("plotGraph##pl", "plot2d", "data", {10, 30, 15, 45, 20, 60, 5, 80})

    -- ==========================================
    -- 9. Create ColorPicker Button
    -- ==========================================
    -- ColorPicker spawns a dark-themed slider dialog to select RGB values.
    gui.widget.create("clrPicker##cp", "colorpicker", "panel1##pn")
    gui.config.set("clrPicker##cp", "colorpicker", "pos", {180, 130})
    gui.config.set("clrPicker##cp", "colorpicker", "size", {40, 22})
    gui.config.set("clrPicker##cp", "colorpicker", "onchanged", function(colorTbl)
        log.info("Selected color R:" .. tostring(colorTbl[1]) .. " G:" .. tostring(colorTbl[2]) .. " B:" .. tostring(colorTbl[3]))
    end)

    -- ==========================================
    -- 10. Create Update Button
    -- ==========================================
    -- Trigger events and chart refreshes using a simple interactive Button.
    gui.widget.create("btnUpdate##btn", "button", "panel1##pn")
    gui.config.set("btnUpdate##btn", "button", "pos", {180, 95})
    gui.config.set("btnUpdate##btn", "button", "size", {80, 22})
    gui.config.set("btnUpdate##btn", "button", "label", "Refresh Plot")
    
    -- Handle the onclick action to generate random points and update the chart.
    gui.config.set("btnUpdate##btn", "button", "onclick", function()
        local randomPts = {}
        for i=1,10 do
            table.insert(randomPts, math.random(10, 90))
        end
        gui.config.set("plotGraph##pl", "plot2d", "data", randomPts)
        log.info("Plot refreshed with random values.")
    end)
end
