﻿using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IDialogCommands
    {
        #region File

        Task<ReferencedFileSave> ShowAddNewFileDialogAsync(AddNewFileViewModel viewModel = null, GlueElement element = null);

        #endregion

        #region Entity

        void ShowAddNewEntityDialog(AddEntityViewModel viewModel = null);

        AddEntityViewModel CreateAddNewEntityViewModel();

        #endregion

        #region NamedObjectSave

        void AskToRemoveObject(NamedObjectSave namedObjectToRemove, bool saveAndRegenerate = true);

        void AskToRemoveObjectList(List<NamedObjectSave> namedObjectsToRemove, bool saveAndRegenerate = true);


        Task<NamedObjectSave> ShowAddNewObjectDialog(FlatRedBall.Glue.ViewModels.AddObjectViewModel addObjectViewModel = null);

        #endregion

        #region Screen

        void ShowAddNewScreenDialog();

        #endregion

        #region Event

        void ShowAddNewEventDialog(NamedObjectSave eventOwner);
        void ShowAddNewEventDialog(AddEventViewModel viewModel);
        void ShowAddNewEventDialog(GlueElement glueElement);

        #endregion

        void ShowLoadProjectDialog();

        #region StateSave

        void ShowAddNewStateDialog();


        #endregion

        #region StateSaveCategory

        void ShowAddNewCategoryDialog();

        #endregion

        #region Message Box

        void ShowMessageBox(string message, string caption = "");
        System.Windows.MessageBoxResult ShowYesNoMessageBox(string message, string caption = "Confirm", Action yesAction = null, Action noAction = null);

        #endregion

        // Not sure where this belongs, but doing this so it's not part of the main window:
        System.Windows.Forms.PropertyGrid PropertyGrid { get; set; }
        int ManagedThreadId { get; set; }
        Action<string> SetTitle { get; set; }
        Action CloseMainWindow { get; set; }

        Func<bool> IsMainWindowDisposed { get; set; }
        IWin32Window Win32Window { get; set; }

        void FocusTab(string dialogTitle);


        void ShowAddNewVariableDialog(Controls.CustomVariableType variableType = Controls.CustomVariableType.Exposed, 
            string tunnelingObject = "",
            string tunneledVariableName = "", GlueElement container = null);


        void FocusOnTreeView();
        void MoveToCursor(System.Windows.Window window);

        void GoToDefinitionOfSelection();


    }
}
