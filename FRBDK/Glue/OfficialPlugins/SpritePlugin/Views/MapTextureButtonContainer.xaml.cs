﻿using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.Extensions;
using OfficialPlugins.SpritePlugin.ViewModels;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.SpritePlugin.Views
{
    /// <summary>
    /// Interaction logic for MapTextureButtonContainer.xaml
    /// </summary>
    public partial class MapTextureButtonContainer : UserControl, IDataUi
    {
        #region Fields/Properties

        static TextureCoordinateSelectionViewModel LastViewModel;
        public InstanceMember InstanceMember { get; set; }
        public bool SuppressSettingProperty { get; set; }

        #endregion

        public MapTextureButtonContainer()
        {
            InitializeComponent();
        }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {

        }

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            result = null;
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            return ApplyValueResult.Success;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            var currentNos = GlueState.Self.CurrentNamedObjectSave;
            var currentElement = GlueState.Self.CurrentElement;

            ReferencedFileSave textureRfs = GetTextureReferencedFileSave(currentNos, currentElement);

            if (textureRfs != null)
            {
                TextureCoordinateSelectionWindow window;
                TextureCoordinateSelectionViewModel viewModel;
                float left, top, right, bottom;

                window = new TextureCoordinateSelectionWindow();
                var fullFile = GlueCommands.Self.FileCommands.GetFilePath(textureRfs);
                window.TextureFilePath = fullFile;

                viewModel = GetNewViewModel(currentNos, currentElement, window, out left, out top, out right, out bottom);
                window.DataContext = viewModel;

                if(LastViewModel != null)
                {
                    viewModel.WindowX = LastViewModel.WindowX;
                    viewModel.WindowY = LastViewModel.WindowY;
                    viewModel.WindowWidth = LastViewModel.WindowWidth;
                    viewModel.WindowHeight = LastViewModel.WindowHeight;
                    window.ShiftWindowOntoScreen(); //Things could have changed and last position is off screen now
                    viewModel.CellHeight = LastViewModel.CellHeight;
                    viewModel.CellWidth = LastViewModel.CellWidth;
                }
                else
                {
                    window.Width = 300;
                    window.Height = 500;
                    GlueCommands.Self.DialogCommands.MoveToCursor(window);

                    //better way to have viewmodel update itself from window?
                    viewModel.WindowWidth = window.Width;
                    viewModel.WindowHeight = window.Height;
                    viewModel.WindowX = window.Left;
                    viewModel.WindowY = window.Top;
                }

                var result = window.ShowDialog();

                if (result == true)
                {
                    ApplyViewModel(currentNos, currentElement, viewModel, left, top, right, bottom);
                }

                LastViewModel = viewModel;
            }
        }

        private static TextureCoordinateSelectionViewModel GetNewViewModel(NamedObjectSave currentNos, GlueElement currentElement, 
            TextureCoordinateSelectionWindow window, out float left, out float top, out float right, out float bottom)
        {
            var viewModel = new TextureCoordinateSelectionViewModel();
            left = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement,
                nameof(Sprite.LeftTexturePixel)) as float? ?? 0;
            top = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement,
                nameof(Sprite.TopTexturePixel)) as float? ?? 0;
            float defaultWidth = 256;
            float defaultHeight = 256;
            if (window.Texture != null)
            {
                defaultWidth = window.Texture.Width;
                defaultHeight = window.Texture.Height;
            }
            viewModel.TextureHeight = defaultHeight;
            viewModel.TextureWidth = defaultWidth;

            right = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement,
                nameof(Sprite.RightTexturePixel)) as float? ?? defaultWidth;
            if (right == 0)
            {
                right = defaultWidth;
            }

            bottom = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement,
                nameof(Sprite.BottomTexturePixel)) as float? ?? defaultHeight;
            if (bottom == 0)
            {
                bottom = defaultHeight;
            }

            viewModel.LeftTexturePixel = (int)left;
            viewModel.TopTexturePixel = (int)top;
            viewModel.SelectedWidthPixels = (int)(right - left);
            viewModel.SelectedHeightPixels = (int)(bottom - top);
            return viewModel;
        }

        private static ReferencedFileSave GetTextureReferencedFileSave(NamedObjectSave currentNos, GlueElement currentElement)
        {
            ReferencedFileSave textureRfs = null;

            if (currentNos != null && currentElement != null)
            {
                var textureValue = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement, "Texture") as string;

                if (textureValue != null)
                {
                    textureRfs = currentElement.GetReferencedFileSaveRecursively(textureValue);
                }

            }

            return textureRfs;
        }

        private static async void ApplyViewModel(NamedObjectSave currentNos, GlueElement currentElement, TextureCoordinateSelectionViewModel viewModel, 
            float oldLeft, float oldTop, float oldRight, float oldBottom)
        {
            bool didAnyChange = false;
            if (viewModel.LeftTexturePixelInt != (int)oldLeft)
            {
                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(currentNos,
                    nameof(Sprite.LeftTexturePixel),
                    (float)viewModel.LeftTexturePixelInt,
                    performSaveAndGenerateCode: false, updateUi: true);
                didAnyChange = true;
            }
            if (viewModel.TopTexturePixelInt != (int)oldTop)
            {
                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(currentNos,
                    nameof(Sprite.TopTexturePixel),
                    (float)viewModel.TopTexturePixelInt,
                    performSaveAndGenerateCode: false, updateUi: true);
                didAnyChange = true;
            }
            if (viewModel.SelectedWidthPixelsInt != (int)(oldRight - oldLeft))
            {
                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(currentNos,
                    nameof(Sprite.RightTexturePixel),
                    (float)(viewModel.LeftTexturePixelInt + viewModel.SelectedWidthPixelsInt),
                    performSaveAndGenerateCode: false, updateUi: true);
                didAnyChange = true;
            }
            if (viewModel.SelectedHeightPixelsInt != (int)(oldBottom - oldTop))
            {
                await GlueCommands.Self.GluxCommands.SetVariableOnAsync(currentNos,
                    nameof(Sprite.BottomTexturePixel),
                    (float)(viewModel.TopTexturePixelInt + viewModel.SelectedHeightPixelsInt),
                    performSaveAndGenerateCode: false, updateUi: true);
                didAnyChange = true;
            }

            if (didAnyChange)
            {
                GlueCommands.Self.GluxCommands.SaveProjectAndElements();
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(currentElement);
            }
        }
    }
}
