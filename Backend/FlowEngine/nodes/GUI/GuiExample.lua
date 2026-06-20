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
    -- gui.dialog.create initializes a docking-enabled window in the workspace.
    -- The suffix "##dlg1" acts as a unique ID while hiding itself from visual labels.
    -- This allows duplicate dialog headers to coexist as unique instances in memory.
    local dlgName = "Dashboard##dlg1"
    gui.dialog.create(dlgName)
    
    -- Configure parent dialog dimensions (Width: 580, Height: 400).
    gui.config.set(dlgName, "dialog", "size", {580, 400})
    
    -- Configure dialog background color behind panels (Catppuccin Crust Dark: 30, 30, 46).
    -- This sets the background color of the parent dialog window frame.
    gui.config.set(dlgName, "dialog", "background_color", {30, 30, 46, 255})
    
    -- gui.dialog.show reveals the created layout on the screen and shifts focus to it.
    -- If it was previously hidden, this restores the tab or opens a floating window.
    gui.dialog.show(dlgName, true)

    -- =========================================================================
    -- 2. Create Panel Container
    -- =========================================================================
    -- Panels act as border containers to organize child widgets.
    -- Here we create "panel1##pn" as a child of the root dialog.
    gui.widget.create("panel1##pn", "panel", dlgName)
    gui.config.set("panel1##pn", "panel", "size", {560, 380})
    gui.config.set("panel1##pn", "panel", "pos", {10, 10})

    -- =========================================================================
    -- 3. Create Title Label
    -- =========================================================================
    -- Create a TextBlock label as a child of the panel widget.
    gui.widget.create("lblTitle##lb", "label", "panel1##pn")
    gui.config.set("lblTitle##lb", "label", "pos", {10, 10})
    
    -- Config key "label" modifies the visible content.
    gui.config.set("lblTitle##lb", "label", "label", "System Configuration Dashboard")
    
    -- Customize colors using {Red, Green, Blue, Alpha} structure (0-255).
    gui.config.set("lblTitle##lb", "label", "foreground_color", {137, 180, 250, 255}) -- Light blue color

    -- =========================================================================
    -- 4. Create Slider and Textbox pair
    -- =========================================================================
    -- Create a slider range bar.
    gui.widget.create("sliderValue##sl", "slider", "panel1##pn")
    gui.config.set("sliderValue##sl", "slider", "pos", {10, 40})
    
    -- Configure minimum and maximum value ranges.
    gui.config.set("sliderValue##sl", "slider", "range", {0, 100})
    
    -- Define step increments for slider movements.
    gui.config.set("sliderValue##sl", "slider", "step", 1)
    
    -- Set the initial value.
    gui.config.set("sliderValue##sl", "slider", "data", 65)
    
    -- Textboxes allow standard textual inputs.
    gui.widget.create("txtSliderVal##txt", "textinput", "panel1##pn")
    gui.config.set("txtSliderVal##txt", "textinput", "pos", {140, 40})
    gui.config.set("txtSliderVal##txt", "textinput", "size", {50, 20})
    gui.config.set("txtSliderVal##txt", "textinput", "data", "65")

    -- Sync slider modifications directly with the textinput box.
    -- Onchanged event returns the updated value of the slider widget.
    gui.config.set("sliderValue##sl", "slider", "onchanged", function(val)
        gui.config.set("txtSliderVal##txt", "textinput", "data", tostring(val))
        log.info("System speed value changed to: " .. tostring(val))
    end)

    -- =========================================================================
    -- 5. Create Checkbox and Dropdown Choice Menu
    -- =========================================================================
    -- Checkboxes offer binary true/false switches.
    gui.widget.create("chkEnable##cb", "checkbox", "panel1##pn")
    gui.config.set("chkEnable##cb", "checkbox", "pos", {10, 70})
    gui.config.set("chkEnable##cb", "checkbox", "label", "Automatic Temp Calibration")
    gui.config.set("chkEnable##cb", "checkbox", "data", true)

    -- Dropdowns offer a selection of multiple values.
    gui.widget.create("dropModes##dd", "dropdown", "panel1##pn")
    gui.config.set("dropModes##dd", "dropdown", "pos", {10, 100})
    
    -- Set the selectable options using standard array strings.
    gui.config.set("dropModes##dd", "dropdown", "menus", {"Laser Mode", "Pulse Mode", "CW Mode"})
    
    -- Set the initial selected choice index (1-based index).
    gui.config.set("dropModes##dd", "dropdown", "data", 1)

    -- =========================================================================
    -- 6. Create Progress Bar and Button
    -- =========================================================================
    -- Progress bars display completion progress metrics (0-100).
    gui.widget.create("progStatus##pg", "progress", "panel1##pn")
    gui.config.set("progStatus##pg", "progress", "pos", {10, 135})
    gui.config.set("progStatus##pg", "progress", "size", {270, 15})
    gui.config.set("progStatus##pg", "progress", "data", 35)

    -- Apply Button to update system status and refresh plots.
    gui.widget.create("btnUpdate##btn", "button", "panel1##pn")
    gui.config.set("btnUpdate##btn", "button", "pos", {180, 100})
    gui.config.set("btnUpdate##btn", "button", "size", {100, 22})
    gui.config.set("btnUpdate##btn", "button", "label", "Apply Config")
    
    -- Handle the onclick action to generate random points and update the chart.
    gui.config.set("btnUpdate##btn", "button", "onclick", function()
        -- Increment progress bar data.
        gui.config.set("progStatus##pg", "progress", "data", 85)
        
        -- Refresh 2D plot with random points.
        local randomPts = {}
        for i=1,10 do
            table.insert(randomPts, math.random(10, 90))
        end
        gui.config.set("plotGraph##pl", "plot2d", "data", randomPts)
        log.info("Configuration applied and graph refreshed.")
    end)

    -- =========================================================================
    -- 7. Create Plot2D Graph
    -- =========================================================================
    -- Plot2D renders real-time numeric line charts inside a customized panel box.
    gui.widget.create("plotGraph##pl", "plot2d", "panel1##pn")
    gui.config.set("plotGraph##pl", "plot2d", "pos", {10, 160})
    gui.config.set("plotGraph##pl", "plot2d", "size", {270, 100})
    gui.config.set("plotGraph##pl", "plot2d", "data", {10, 25, 45, 30, 60, 40, 75, 55, 90, 80})

    -- =========================================================================
    -- 8. Create Plot3D Wireframe Graph
    -- =========================================================================
    -- Plot3D projects 3D matrix datasets. Pass a nested 2D table grid.
    -- It draws a projection wireframe using coordinate algorithms.
    gui.widget.create("plot3dGraph##p3", "plot3d", "panel1##pn")
    gui.config.set("plot3dGraph##p3", "plot3d", "pos", {300, 40})
    gui.config.set("plot3dGraph##p3", "plot3d", "size", {240, 220})
    
    -- peakGrid represents the Z values of a Gaussian 3D surface peak.
    local peakGrid = {
        {10, 12, 15, 12, 10},
        {12, 20, 35, 20, 12},
        {15, 35, 80, 35, 15},
        {12, 20, 35, 20, 12},
        {10, 12, 15, 12, 10}
    }
    gui.config.set("plot3dGraph##p3", "plot3d", "data", peakGrid)

    -- =========================================================================
    -- 9. Create ColorPicker and Image Box
    -- =========================================================================
    -- ColorPicker spawns a dark-themed slider dialog to select RGB values.
    gui.widget.create("clrPicker##cp", "colorpicker", "panel1##pn")
    gui.config.set("clrPicker##cp", "colorpicker", "pos", {300, 275})
    gui.config.set("clrPicker##cp", "colorpicker", "size", {50, 22})

    -- Render an image container if a valid source Mat exists.
    -- This displays the real-time processed matrix directly inside our canvas layout.
    if src and not src:empty() then
        gui.widget.create("imgView##im", "image", "panel1##pn")
        gui.config.set("imgView##im", "image", "pos", {10, 275})
        gui.config.set("imgView##im", "image", "size", {120, 90})
        gui.config.set("imgView##im", "image", "data", src)
    end
end
