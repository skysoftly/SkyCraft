using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SkyCraft.Launcher;

namespace SkyCraft.Helpers;

public static class FileHelper
{
    public static async Task<string> OnBrowseFolderClickAsync()
    {
        try
        {
            var window = App.MainWindow;
            if (window?.StorageProvider is not { } provider)
                return string.Empty;
            
            // Асинхронный вызов не блокирует UI
            var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Выберите папку для установки",
                AllowMultiple = false
            });
            
            if (folders.Count >= 1)
            {
                string folderPath = folders[0].Path.LocalPath;
                
                return folderPath.TrimEnd('\\', '/');
            }
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            });
        }
        
        return string.Empty;
    }
    
    
    public static async Task<List<string>> OnBrowseModsClickAsync()
    {
        try
        {
            var window = App.MainWindow;
            if (window?.StorageProvider is not { } provider)
                return new List<string>();
            
            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите моды (.jar)",
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("Jar файлы")
                    {
                        Patterns = new[] { "*.jar" },
                        MimeTypes = new[] { "application/java-archive" }
                    }
                }
            });
            
            var selectedFiles = new List<string>();
            foreach (var file in files)
            {
                string filePath = file.Path.LocalPath;
                if (filePath.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                {
                    selectedFiles.Add(filePath);
                }
            }
            
            return selectedFiles;
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            });
        }
        
        return new List<string>();
    }

    // Метод для выбора одного мода
    public static async Task<string> OnBrowseModClickAsync()
    {
        var files = await OnBrowseModsClickAsync();
        return files.Count > 0 ? files[0] : string.Empty;
    }

    // Метод для копирования модов в папку инстанса
    public static async Task<List<string>> CopyModsToInstanceAsync(List<string> modFiles, string instancePath)
    {
        var copiedFiles = new List<string>();
        
        if (string.IsNullOrEmpty(instancePath))
            return copiedFiles;
        
        string modsFolder = Path.Combine(instancePath, "mods");
        Directory.CreateDirectory(modsFolder);
        
        foreach (var modFile in modFiles)
        {
            try
            {
                string fileName = Path.GetFileName(modFile);
                string destPath = Path.Combine(modsFolder, fileName);
                
                // Если файл уже существует - перезаписываем
                File.Copy(modFile, destPath, true);
                copiedFiles.Add(destPath);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка копирования {modFile}: {ex.Message}");
            }
        }
        
        return copiedFiles;
    }
}