﻿using CommonControls.Common;
using CommonControls.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AssetEditor.ViewModels
{
    class SettingsViewModel : NotifyPropertyChangedImpl
    {
        public ObservableCollection<GamePathItem> GameDirectores { get; set; } = new ObservableCollection<GamePathItem>();

        GameTypeEnum _currentGame;
        public GameTypeEnum CurrentGame { get => _currentGame; set => SetAndNotify(ref _currentGame, value); }

        bool _UseTextEditorForUnknownFiles;
        public bool UseTextEditorForUnknownFiles { get => _UseTextEditorForUnknownFiles; set => SetAndNotify(ref _UseTextEditorForUnknownFiles, value); }
        
        bool _loadCaPacksByDefault;
        public bool LoadCaPacksByDefault { get => _loadCaPacksByDefault; set => SetAndNotify(ref _loadCaPacksByDefault, value); }

        bool _autoGenerateAttachmentPointsFromMeshes;
        public bool AutoGenerateAttachmentPointsFromMeshes { get => _autoGenerateAttachmentPointsFromMeshes; set => SetAndNotify(ref _autoGenerateAttachmentPointsFromMeshes, value); }

        bool _skipLoadingWemFiles;
        public bool SkipLoadingWemFiles { get => _skipLoadingWemFiles; set => SetAndNotify(ref _skipLoadingWemFiles, value); }

        bool _autoResolveMissingTextures;
        public bool AutoResolveMissingTextures { get => _autoResolveMissingTextures; set => SetAndNotify(ref _autoResolveMissingTextures, value); }


        public ICommand SaveCommand { get; set; }

        ApplicationSettingsService _settingsService;
        public SettingsViewModel(ApplicationSettingsService settingsService)
        {
            _settingsService = settingsService;

            foreach (var game in GameInformationFactory.Games)
            {
                GameDirectores.Add(
                    new GamePathItem()
                    {
                        GameName = game.DisplayName,
                        GameType = game.Type,
                        Path = _settingsService.CurrentSettings.GameDirectories.FirstOrDefault(x => x.Game == game.Type)?.Path
                    });
            }

            CurrentGame = _settingsService.CurrentSettings.CurrentGame;
            UseTextEditorForUnknownFiles = _settingsService.CurrentSettings.UseTextEditorForUnknownFiles;
            LoadCaPacksByDefault = _settingsService.CurrentSettings.LoadCaPacksByDefault;
            AutoGenerateAttachmentPointsFromMeshes = _settingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes;
            AutoResolveMissingTextures = _settingsService.CurrentSettings.AutoResolveMissingTextures;
            SkipLoadingWemFiles = _settingsService.CurrentSettings.SkipLoadingWemFiles;

            SaveCommand = new RelayCommand(OnSave);
        }

        void OnSave()
        {
            _settingsService.CurrentSettings.CurrentGame = CurrentGame;
            _settingsService.CurrentSettings.UseTextEditorForUnknownFiles = UseTextEditorForUnknownFiles;
            _settingsService.CurrentSettings.LoadCaPacksByDefault = LoadCaPacksByDefault;
            _settingsService.CurrentSettings.SkipLoadingWemFiles = SkipLoadingWemFiles;
            _settingsService.CurrentSettings.AutoResolveMissingTextures = AutoResolveMissingTextures;
            _settingsService.CurrentSettings.AutoGenerateAttachmentPointsFromMeshes = AutoGenerateAttachmentPointsFromMeshes;

            _settingsService.CurrentSettings.GameDirectories.Clear();
            foreach (var item in GameDirectores)
                _settingsService.CurrentSettings.GameDirectories.Add(new ApplicationSettings.GamePathPair() { Game = item.GameType, Path = item.Path });

            _settingsService.Save();
        }
    }

    class GamePathItem : NotifyPropertyChangedImpl
    {
        public GameTypeEnum GameType{ get; set; }

        string _gameName;
        public string GameName { get => _gameName; set => SetAndNotify(ref _gameName, value); }

        string _path;
        public string Path { get => _path; set => SetAndNotify(ref _path, value); }

        public ICommand BrowseCommand { get; set; } 

        public GamePathItem()
        {
            BrowseCommand = new RelayCommand(OnBrowse);
        }

        void OnBrowse()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Path = dialog.FileName;
                var files = Directory.GetFiles(Path);
                var packFiles = files.Count(x => System.IO.Path.GetExtension(x) == ".pack");
                var manifest = files.Count(x => x.Contains("manifest.txt"));

                if (packFiles == 0 || manifest == 0)
                    MessageBox.Show($"The selected directory contains {packFiles} packfiles and {manifest} manifest files. It is probably not a game directory");
            }
        }
    }
}
