﻿using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using OfficialPlugins.Compiler.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Managers;
using System.Windows;
using OfficialPlugins.Compiler.CodeGeneration;
using System.Net.Sockets;
using OfficialPlugins.Compiler.Managers;
using FlatRedBall.Glue.Controls;
using System.ComponentModel;
using FlatRedBall.Glue.IO;
using Newtonsoft.Json;
using OfficialPlugins.Compiler.Models;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPluginsCore.Compiler.ViewModels;
using OfficialPluginsCore.Compiler.Managers;
using System.Diagnostics;
using System.Timers;
using Glue;
using OfficialPluginsCore.Compiler.CommandReceiving;
using FlatRedBall.Glue.Elements;
using OfficialPlugins.Compiler.Dtos;
using OfficialPlugins.Compiler.CommandSending;
using System.Runtime.InteropServices;
using OfficialPlugins.GameHost.Views;

namespace OfficialPlugins.Compiler
{
    [Export(typeof(PluginBase))]
    public class MainPlugin : PluginBase
    {
        #region Fields/Properties


        Compiler compiler;
        Runner runner;

        public CompilerViewModel CompilerViewModel { get; private set; }
        public GlueViewSettingsViewModel GlueViewSettingsViewModel { get; private set; }
        public MainControl MainControl { get; private set; } 

        public static CompilerViewModel MainViewModel { get; private set; }



        PluginTab buildTab;
        PluginTab glueViewSettingsTab;

        Game1GlueControlGenerator game1GlueControlGenerator;

        public override string FriendlyName => "Glue Compiler";

        public override Version Version
        {
            get
            {
                // 0.4 introduces:
                // - multicore building
                // - Removed warnings and information when building - now we just show start, end, and errors
                // - If an error occurs, a popup appears telling the user that the game crashed, and to open Visual Studio
                // 0.5
                // - Support for running content-only builds
                // 0.6
                // - Added VS 2017 support
                // 0.7
                // - Added a list of MSBuild locations
                return new Version(0, 7);
            }
        }

        FilePath JsonSettingsFilePath => GlueState.Self.ProjectSpecificSettingsFolder + "CompilerSettings.json";

        bool ignoreViewModelChanges = false;

        Timer timer;


        #endregion

        #region Startup

        public override void StartUp()
        {
            CreateControl();

            CreateToolbar();

            RefreshManager.Self.InitializeEvents(this.MainControl.PrintOutput, this.MainControl.PrintOutput);

            Output.Initialize(this.MainControl.PrintOutput);


            compiler = Compiler.Self;
            runner = Runner.Self;

            runner.AfterSuccessfulRun += async () =>
            {
                // If we aren't generating the code, we shouldn't try to move the game to Glue since the borders can't be adjusted
                if(CompilerViewModel.IsGenerateGlueControlManagerInGame1Checked)
                {
                    MoveGameToHost();
                }
                
                await SendGlueViewSettingsToGame();

            };

            game1GlueControlGenerator = new Game1GlueControlGenerator();
            this.RegisterCodeGenerator(game1GlueControlGenerator);

            this.RegisterCodeGenerator(new CompilerPluginElementCodeGenerator());


            #region Start the timer

            var timerFrequency = 250; // ms
            timer = new Timer(timerFrequency);
            timer.Elapsed += HandleTimerElapsed;
            timer.SynchronizingObject = MainGlueWindow.Self;
            timer.Start();

            #endregion


            // winforms stuff is here:
            // https://social.msdn.microsoft.com/Forums/en-US/f6e28fe1-03b2-4df5-8cfd-7107c2b6d780/hosting-external-application-in-windowsformhost?forum=wpf
            gameHostView = new GameHostView();
            gameHostView.DataContext = CompilerViewModel;

            pluginTab = base.CreateTab(gameHostView, "Game", TabLocation.Center);
            pluginTab.CanClose = false;
            pluginTab.AfterHide += (_, __) => TryKillGame();
            //pluginTab = base.CreateAndAddTab(GameHostView, "Game Contrll", TabLocation.Bottom);

            // do this after creating the compiler, view model, and control
            AssignEvents();

            this.ReactToLoadedGlux += () => pluginTab.Show();
            this.ReactToUnloadedGlux += () => pluginTab.Hide();

            GameHostController.Self.Initialize(gameHostView, MainControl, 
                CompilerViewModel, 
                GlueViewSettingsViewModel,
                glueViewSettingsTab);
        }

        private void AssignEvents()
        {
            var manager = new FileChangeManager(MainControl, compiler, CompilerViewModel);
            this.ReactToFileChangeHandler += manager.HandleFileChanged;
            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleGluxUnloaded;
            this.ReactToNewFileHandler += RefreshManager.Self.HandleNewFile;

            this.ReactToCodeFileChange += RefreshManager.Self.HandleFileChanged;
            this.NewEntityCreated += RefreshManager.Self.HandleNewEntityCreated;


            this.NewScreenCreated += (newScreen) =>
            {
                ToolbarController.Self.HandleNewScreenCreated(newScreen);
                RefreshManager.Self.HandleNewScreenCreated();
            };
            this.ReactToScreenRemoved += ToolbarController.Self.HandleScreenRemoved;
            // todo - handle startup changed...
            this.ReactToNewObjectHandler += RefreshManager.Self.HandleNewObjectCreated;
            this.ReactToObjectRemoved += async (owner, nos) =>
                await RefreshManager.Self.HandleObjectRemoved(owner, nos);
            this.ReactToElementVariableChange += RefreshManager.Self.HandleVariableChanged;
            this.ReactToNamedObjectChangedValue += (string changedMember, object oldValue, NamedObjectSave namedObject) => 
                RefreshManager.Self.HandleNamedObjectValueChanged(changedMember, oldValue, namedObject, Dtos.AssignOrRecordOnly.Assign);
            this.ReactToChangedStartupScreen += ToolbarController.Self.ReactToChangedStartupScreen;
            this.ReactToItemSelectHandler += RefreshManager.Self.HandleItemSelected;
            this.ReactToObjectContainerChanged += RefreshManager.Self.HandleObjectContainerChanged;
            // If a variable is added, that may be used later to control initialization.
            // The game won't reflect that until it has been restarted, so let's just take 
            // care of it now. For variable removal I don't know if any restart is needed...
            this.ReactToVariableAdded += RefreshManager.Self.HandleVariableAdded;
            this.ReactToStateCreated += RefreshManager.Self.HandleStateCreated;
            this.ReactToStateVariableChanged += RefreshManager.Self.HandleStateVariableChanged;
            //this.ReactToMainWindowMoved += gameHostView.ReactToMainWindowMoved;
            this.ReactToMainWindowResizeEnd += gameHostView.ReactToMainWindowResizeEnd;
            this.TryHandleTreeNodeDoubleClicked += RefreshManager.Self.HandleTreeNodeDoubleClicked;
        }


        #endregion

        #region Public events (called externally)

        public async Task BuildAndRun()
        {
            if (CompilerViewModel.IsToolbarPlayButtonEnabled)
            {
                GlueCommands.Self.DialogCommands.FocusTab("Build");
                var succeeded = await GameHostController.Self.Compile();

                if (succeeded)
                {
                    bool hasErrors = GetIfHasErrors();
                    if (hasErrors)
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";

                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, async () => await runner.Run(preventFocus: false));
                    }
                    else
                    {
                        PluginManager.ReceiveOutput("Building succeeded. Running project...");

                        await runner.Run(preventFocus: false);
                    }
                }
                else
                {
                    PluginManager.ReceiveError("Building failed. See \"Build\" tab for more information.");
                }
            }
        }

        public bool GetIfIsRunningInEditMode()
        {
            return CompilerViewModel.IsEditChecked && CompilerViewModel.IsRunning;
        }

        public async void MakeGameBorderless(bool isBorderless)
        {
            var dto = new Dtos.SetBorderlessDto
            {
                IsBorderless = isBorderless
            };

            await CommandSending.CommandSender
                .Send(dto, GlueViewSettingsViewModel.PortNumber);
        }

        #endregion

        System.Threading.SemaphoreSlim getCommandsSemaphore = new System.Threading.SemaphoreSlim(1);
        private async void HandleTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var isBusy = await getCommandsSemaphore.WaitAsync(0);
            if(!isBusy)
            {
                try
                {
                    if(CompilerViewModel.IsEditChecked)
                    {
                        var gameToGlueCommandsAsString = await CommandSending.CommandSender
                            .SendCommand("GetCommands", GlueViewSettingsViewModel.PortNumber);

                        if (!string.IsNullOrEmpty(gameToGlueCommandsAsString))
                        {
                            CommandReceiver.HandleCommandsFromGame(gameToGlueCommandsAsString, GlueViewSettingsViewModel.PortNumber);
                        }
                    }
                }
                catch
                {
                    // it's okay
                }
                finally
                {
                    getCommandsSemaphore.Release();
                }
            }

        }
        private void HandleGluxUnloaded()
        {
            CompilerViewModel.CompileContentButtonVisibility = Visibility.Collapsed;
            CompilerViewModel.HasLoadedGlux = false;

            ToolbarController.Self.HandleGluxUnloaded();
        }

        private CompilerSettingsModel LoadOrCreateCompilerSettings()
        {
            CompilerSettingsModel compilerSettings = new CompilerSettingsModel();
            var filePath = JsonSettingsFilePath;
            if (filePath.Exists())
            {
                try
                {
                    var text = System.IO.File.ReadAllText(filePath.FullPath);
                    compilerSettings = JsonConvert.DeserializeObject<CompilerSettingsModel>(text);
                }
                catch
                {
                    // do nothing, it'll just get wiped out and re-saved later
                }
            }

            return compilerSettings;
        }

        private bool IsFrbNewEnough()
        {
            var mainProject = GlueState.Self.CurrentMainProject;
            if(mainProject.IsFrbSourceLinked())
            {
                return true;
            }
            else
            {
                return GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode;
            }
        }

        private void HandleGluxLoaded()
        {
            UpdateCompileContentVisibility();

            var model = LoadOrCreateCompilerSettings();
            ignoreViewModelChanges = true;
            GlueViewSettingsViewModel.SetFrom(model);
            CompilerViewModel.IsGenerateGlueControlManagerInGame1Checked = GlueViewSettingsViewModel.EnableGlueViewEdit;
            ignoreViewModelChanges = false;

            CompilerViewModel.IsGluxVersionNewEnoughForGlueControlGeneration =
                GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.AddedGeneratedGame1;
            CompilerViewModel.HasLoadedGlux = true;

            game1GlueControlGenerator.PortNumber = model.PortNumber;
            game1GlueControlGenerator.IsGlueControlManagerGenerationEnabled = model.GenerateGlueControlManagerCode && IsFrbNewEnough();

            RefreshManager.Self.PortNumber = model.PortNumber;

            ToolbarController.Self.HandleGluxLoaded();

            if(IsFrbNewEnough())
            {
                TaskManager.Self.Add(() => EmbeddedCodeManager.EmbedAll(model.GenerateGlueControlManagerCode), "Generate Glue Control Code");
            }

            GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded("Newtonsoft.Json", "12.0.3");
        }

        private void UpdateCompileContentVisibility()
        {
            bool shouldShowCompileContentButton = false;

            if (GlueState.Self.CurrentMainProject != null)
            {
                shouldShowCompileContentButton = GlueState.Self.CurrentMainProject != GlueState.Self.CurrentMainContentProject;

                if (!shouldShowCompileContentButton)
                {
                    foreach (var mainSyncedProject in GlueState.Self.SyncedProjects)
                    {
                        if (mainSyncedProject != mainSyncedProject.ContentProject)
                        {
                            shouldShowCompileContentButton = true;
                            break;
                        }
                    }
                }

            }

            if (shouldShowCompileContentButton)
            {
                CompilerViewModel.CompileContentButtonVisibility = Visibility.Visible;
            }
            else
            {
                CompilerViewModel.CompileContentButtonVisibility = Visibility.Collapsed;
            }
        }

        private void CreateToolbar()
        {
            var toolbar = new RunnerToolbar();
            toolbar.RunClicked += HandleToolbarRunClicked;

            ToolbarController.Self.Initialize(toolbar);

            toolbar.DataContext = ToolbarController.Self.GetViewModel();

            base.AddToToolBar(toolbar, "Standard");
        }

        private async void HandleToolbarRunClicked(object sender, EventArgs e)
        {
            await BuildAndRun();
        }


        private void CreateControl()
        {
            CompilerViewModel = new CompilerViewModel();
            CompilerViewModel.Configuration = "Debug";
            GlueViewSettingsViewModel = new GlueViewSettingsViewModel();
            GlueViewSettingsViewModel.PropertyChanged += HandleGlueViewSettingsViewModelPropertyChanged;
            CompilerViewModel.PropertyChanged += HandleCompilerViewModelPropertyChanged;

            MainViewModel = CompilerViewModel;

            MainControl = new MainControl();
            MainControl.DataContext = CompilerViewModel;

            Runner.Self.ViewModel = CompilerViewModel;
            RefreshManager.Self.ViewModel = CompilerViewModel;
            RefreshManager.Self.GlueViewSettingsViewModel = GlueViewSettingsViewModel;

            VariableSendingManager.Self.ViewModel = CompilerViewModel;
            VariableSendingManager.Self.GlueViewSettingsViewModel = GlueViewSettingsViewModel;


            buildTab = base.CreateTab(MainControl, "Build", TabLocation.Bottom);
            buildTab.Show();


            var glueViewSettingsView = new Views.GlueViewSettings();
            glueViewSettingsView.ViewModel = GlueViewSettingsViewModel;

            glueViewSettingsTab = base.CreateTab(glueViewSettingsView, "GlueView Settings");

            AssignControlEvents();
        }

        private async void HandleGlueViewSettingsViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //////////Early Out////////////////////
            if (ignoreViewModelChanges)
            {
                return;
            }

            /////////End Early Out//////////////// 
            var propertyName = e.PropertyName;
            switch(propertyName)
            {
                case nameof(ViewModels.GlueViewSettingsViewModel.PortNumber):
                case nameof(ViewModels.GlueViewSettingsViewModel.EnableGlueViewEdit):
                    CompilerViewModel.IsGenerateGlueControlManagerInGame1Checked = GlueViewSettingsViewModel.EnableGlueViewEdit;
                    await HandlePortOrGenerateCheckedChanged(propertyName);
                    break;
                case nameof(ViewModels.GlueViewSettingsViewModel.GridSize):
                case nameof(ViewModels.GlueViewSettingsViewModel.ShowScreenBoundsWhenViewingEntities):
                    await SendGlueViewSettingsToGame();
                    break;
            }


            SaveCompilerSettingsModel();

        }

        private async Task SendGlueViewSettingsToGame()
        {
            var dto = new Dtos.GlueViewSettingsDto
            {
                GridSize = GlueViewSettingsViewModel.GridSize,
                ShowScreenBoundsWhenViewingEntities = GlueViewSettingsViewModel.ShowScreenBoundsWhenViewingEntities
            };

            await CommandSender.Send(dto, GlueViewSettingsViewModel.PortNumber);
        }

        private async void HandleCompilerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //////////Early Out////////////////////
            if (ignoreViewModelChanges)
            {
                return;
            }

            /////////End Early Out////////////////
            var propertyName = e.PropertyName;

            switch (propertyName)
            {

                case nameof(ViewModels.CompilerViewModel.CurrentGameSpeed):
                    var speedPercentage = int.Parse(CompilerViewModel.CurrentGameSpeed.Substring(0, CompilerViewModel.CurrentGameSpeed.Length - 1));
                    await CommandSender.Send(new SetSpeedDto
                    {
                        SpeedPercentage = speedPercentage
                    }, GlueViewSettingsViewModel.PortNumber);
                    
                    break;
                case nameof(ViewModels.CompilerViewModel.EffectiveIsRebuildAndRestartEnabled):
                    RefreshManager.Self.IsExplicitlySetRebuildAndRestartEnabled = CompilerViewModel.EffectiveIsRebuildAndRestartEnabled;
                    break;
                case nameof(ViewModels.CompilerViewModel.IsToolbarPlayButtonEnabled):
                    ToolbarController.Self.SetEnabled(CompilerViewModel.IsToolbarPlayButtonEnabled);
                    break;
                case nameof(ViewModels.CompilerViewModel.IsRunning):
                    //CommandSender.CancelConnect();
                    break;
                case nameof(ViewModels.CompilerViewModel.PlayOrEdit):

                    var inEditMode = CompilerViewModel.PlayOrEdit == PlayOrEdit.Edit;
                    await CommandSending.CommandSender.Send(
                        new Dtos.SetEditMode { IsInEditMode = inEditMode },
                        GlueViewSettingsViewModel.PortNumber);

                    if (inEditMode)
                    {
                        var currentEntity = GlueCommands.Self.DoOnUiThread<EntitySave>(() => GlueState.Self.CurrentEntitySave);
                        if(currentEntity != null)
                        {
                            await GlueCommands.Self.DoOnUiThread(async () => await RefreshManager.Self.PushGlueSelectionToGame());
                        }
                        else
                        {
                            var screenName = await CommandSending.CommandSender.GetScreenName(GlueViewSettingsViewModel.PortNumber);

                            if (!string.IsNullOrEmpty(screenName))
                            {
                                var glueScreenName =
                                    string.Join('\\', screenName.Split('.').Skip(1).ToArray());

                                var screen = ObjectFinder.Self.GetScreenSave(glueScreenName);

                                if (screen != null)
                                {
                                    await GlueCommands.Self.DoOnUiThread(async () =>
                                    {
                                        if(GlueState.Self.CurrentElement != screen)
                                        {
                                            GlueState.Self.CurrentElement = screen;
                                        }
                                        else
                                        {
                                            // the screens are the same, so push the object selection from Glue to the game:
                                            await RefreshManager.Self.PushGlueSelectionToGame();
                                        }
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        // the user is viewing an entity, so force the screen
                        if(GlueState.Self.CurrentEntitySave != null)
                        {
                            // push the selection to game
                            var startupScreen = ObjectFinder.Self.GetScreenSave(GlueState.Self.CurrentGlueProject.StartUpScreen);
                            await RefreshManager.Self.PushGlueSelectionToGame(forcedElement: startupScreen);
                        }
                    }


                    break;
            }


        }

        private async Task HandlePortOrGenerateCheckedChanged(string propertyName)
        {
            MainControl.PrintOutput("Applying changes");
            game1GlueControlGenerator.IsGlueControlManagerGenerationEnabled = GlueViewSettingsViewModel.EnableGlueViewEdit && IsFrbNewEnough();
            game1GlueControlGenerator.PortNumber = GlueViewSettingsViewModel.PortNumber;
            RefreshManager.Self.PortNumber = GlueViewSettingsViewModel.PortNumber;
            GlueCommands.Self.GenerateCodeCommands.GenerateGame1();
            if (IsFrbNewEnough())
            {
                TaskManager.Self.Add(() => EmbeddedCodeManager.EmbedAll(GlueViewSettingsViewModel.EnableGlueViewEdit), "Generate Glue Control Code");
            }

            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.NugetPackageInCsproj)
            {
                GlueCommands.Self.ProjectCommands.AddNugetIfNotAdded("Newtonsoft.Json", "12.0.3");
            }

            RefreshManager.Self.StopAndRestartTask($"{propertyName} changed");

            MainControl.PrintOutput("Waiting for tasks to finish...");
            await TaskManager.Self.WaitForAllTasksFinished();
            MainControl.PrintOutput("Finishined adding/generating code for GlueControlManager");
        }

        private void SaveCompilerSettingsModel()
        {
            var model = new CompilerSettingsModel();
            GlueViewSettingsViewModel.SetModel(model);
            try
            {
                var text = JsonConvert.SerializeObject(model);
                GlueCommands.Self.TryMultipleTimes(() =>
                {
                    System.IO.Directory.CreateDirectory(JsonSettingsFilePath.GetDirectoryContainingThis().FullPath);
                    System.IO.File.WriteAllText(JsonSettingsFilePath.FullPath, text);
                });
            }
            catch
            {
                // no big deal if it fails
            }
        }

        private void AssignControlEvents()
        {
            MainControl.BuildClicked += async (not, used) =>
            {
                await GameHostController.Self.Compile();
            };


            MainControl.BuildContentClicked += delegate
            {
                BuildContent(OutputSuccessOrFailure);
            };

            MainControl.RunClicked += async (not, used) =>
            {
                var succeeded = await GameHostController.Self.Compile();
                if (succeeded)
                {
                    if (succeeded)
                    {
                        await runner.Run(preventFocus: false);
                    }
                    else
                    {
                        var runAnywayMessage = "Your project has content errors. To fix them, see the Errors tab. You can still run the game but you may experience crashes. Run anyway?";

                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(runAnywayMessage, async () => await runner.Run(preventFocus: false));
                    }
                }
            };



        }


        private static bool GetIfHasErrors()
        {
            var errorPlugin = PluginManager.AllPluginContainers
                                .FirstOrDefault(item => item.Plugin is ErrorPlugin.MainErrorPlugin)?.Plugin as ErrorPlugin.MainErrorPlugin;

            var hasErrors = errorPlugin?.HasErrors == true;
            return hasErrors;
        }

        private void OutputSuccessOrFailure(bool succeeded)
        {
            if (succeeded)
            {
                MainControl.PrintOutput($"{DateTime.Now.ToLongTimeString()} Build succeeded");
            }
            else
            {
                MainControl.PrintOutput($"{DateTime.Now.ToLongTimeString()} Build failed");

            }
        }

        private void BuildContent(Action<bool> afterCompile = null)
        {
            compiler.BuildContent(MainControl.PrintOutput, MainControl.PrintOutput, afterCompile, CompilerViewModel.Configuration);
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            TryKillGame();
            return true;
        }


        public async void ShowState(string stateName, string categoryName)
        {
            await RefreshManager.Self.PushGlueSelectionToGame(categoryName, stateName);
        }





        #region DLLImports
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        // from https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf?view=netframeworkdesktop-4.8
        internal const int
          WS_CHILD = 0x40000000,
          WS_VISIBLE = 0x10000000,
          LBS_NOTIFY = 0x00000001,
          HOST_ID = 0x00000002,
          LISTBOX_ID = 0x00000001,
          WS_VSCROLL = 0x00200000,
          WS_BORDER = 0x00800000;

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        #endregion

        #region Fields/Properties


        PluginTab pluginTab;
        GameHostView gameHostView;

        Process gameProcess;

        #endregion

        public async void MoveGameToHost()
        {
            gameProcess = Runner.Self.TryFindGameProcess();
            var handle = gameProcess?.MainWindowHandle;

            if (handle != null)
            {
                await gameHostView.EmbedHwnd(handle.Value);
            }
        }


        private void TryKillGame()
        {
            if (gameProcess != null)
            {
                try
                {
                    gameProcess?.Kill();
                }
                catch
                {
                    // no biggie, It hink
                }
            }
        }



    }

}
