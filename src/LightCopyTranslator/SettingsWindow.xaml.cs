using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LightCopyTranslator.Services;

namespace LightCopyTranslator;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly ObservableCollection<ModelConfig> _models;

    public event Action? SettingsSaved;

    public SettingsWindow(AppConfig config)
    {
        _config = config;
        InitializeComponent();
        _models = new ObservableCollection<ModelConfig>(CloneModels(config.Models));
        ModelsGrid.ItemsSource = _models;
        ShowSourcePanelCheckBox.IsChecked = _config.Ui.ShowSourcePanel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _config.Ui.ShowSourcePanel = ShowSourcePanelCheckBox.IsChecked == true;
        _config.Models = _models
            .Select(model => new ModelConfig
            {
                Name = model.Name?.Trim() ?? "",
                ApiKey = model.ApiKey?.Trim() ?? "",
                Model = model.Model?.Trim() ?? "",
                BaseUrl = string.IsNullOrWhiteSpace(model.BaseUrl)
                    ? "https://openrouter.ai/api/v1"
                    : model.BaseUrl.Trim(),
                Enabled = model.Enabled
            })
            .Where(model => !string.IsNullOrWhiteSpace(model.Name) || !string.IsNullOrWhiteSpace(model.Model))
            .ToList();

        if (_config.Models.Count == 0)
        {
            _config.Models =
            [
                new ModelConfig
                {
                    Name = "DeepSeek",
                    Model = "deepseek/deepseek-chat"
                }
            ];
        }

        ConfigService.Save(_config);
        SettingsSaved?.Invoke();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AddModel_Click(object sender, RoutedEventArgs e)
    {
        _models.Add(new ModelConfig
        {
            Name = "New Model",
            Model = "deepseek/deepseek-chat",
            BaseUrl = "https://openrouter.ai/api/v1",
            Enabled = true
        });
    }

    private void RemoveModel_Click(object sender, RoutedEventArgs e)
    {
        if (ModelsGrid.SelectedItems.Count == 0)
        {
            return;
        }

        var toRemove = ModelsGrid.SelectedItems.Cast<ModelConfig>().ToList();
        foreach (var model in toRemove)
        {
            _models.Remove(model);
        }
    }

    private static IEnumerable<ModelConfig> CloneModels(IEnumerable<ModelConfig> models)
    {
        foreach (var model in models)
        {
            yield return new ModelConfig
            {
                Name = model.Name,
                ApiKey = model.ApiKey,
                Model = model.Model,
                BaseUrl = model.BaseUrl,
                Enabled = model.Enabled
            };
        }
    }
}
