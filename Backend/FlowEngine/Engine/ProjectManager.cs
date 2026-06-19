using System;
using System.IO;
using Microsoft.Win32;

namespace FlowEngine.Engine
{
        /// <summary>
    /// Manages file input/output dialog operations for saving and loading
    /// FlowEngine project configurations and custom UI themes.
    /// </summary>
    public static class ProjectManager
    {
                /// <summary>
        /// Opens a SaveFileDialog to write the current project flow JSON to a file.
        /// </summary>
        /// <param name="projectJson">The serialized JSON representation of the project graph</param>
        /// <returns>The path of the saved file if successful, otherwise null</returns>
        public static string? SaveProject(string projectJson)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = "project.json",
                    Title = "Save FlowEngine Project"
                };

                // ShowDialog needs to be run on STA thread (which is standard for WPF UI handlers)
                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, projectJson);
                    return dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving project: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return null;
        }

                /// <summary>
        /// Opens an OpenFileDialog to read a saved project flow JSON from a file.
        /// </summary>
        /// <param name="fileContent">The loaded JSON file content returned as an out parameter</param>
        /// <returns>The path of the loaded file if successful, otherwise null</returns>
        public static string? LoadProject(out string? fileContent)
        {
            fileContent = null;
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    Title = "Open FlowEngine Project"
                };

                if (dialog.ShowDialog() == true)
                {
                    fileContent = File.ReadAllText(dialog.FileName);
                    return dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading project: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return null;
        }

                /// <summary>
        /// Opens a SaveFileDialog to write a custom theme configuration JSON to a file.
        /// </summary>
        /// <param name="themeJson">The serialized JSON representation of the theme colors</param>
        /// <returns>The path of the saved file if successful, otherwise null</returns>
        public static string? SaveTheme(string themeJson)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Theme Files (*.theme.json)|*.theme.json|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "theme.json",
                    FileName = "custom.theme.json",
                    Title = "Save NOVA Theme"
                };

                if (dialog.ShowDialog() == true)
                {
                    File.WriteAllText(dialog.FileName, themeJson);
                    return dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving theme: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return null;
        }

                /// <summary>
        /// Opens an OpenFileDialog to read a theme configuration JSON from a file.
        /// </summary>
        /// <param name="fileContent">The loaded JSON file content returned as an out parameter</param>
        /// <returns>The path of the loaded file if successful, otherwise null</returns>
        public static string? LoadTheme(out string? fileContent)
        {
            fileContent = null;
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Theme Files (*.theme.json;*.json)|*.theme.json;*.json|All Files (*.*)|*.*",
                    DefaultExt = "theme.json",
                    Title = "Open NOVA Theme"
                };

                if (dialog.ShowDialog() == true)
                {
                    fileContent = File.ReadAllText(dialog.FileName);
                    return dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading theme: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            return null;
        }
    }
}
