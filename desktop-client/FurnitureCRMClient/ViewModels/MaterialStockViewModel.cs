using System;
using System.ComponentModel;
using System.Windows.Input;
using FurnitureCRMClient.Models;
using System.Collections.Generic;
using System.Linq;

namespace FurnitureCRMClient.ViewModels
{
    public class MaterialStockViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Material _material;
        public Material Material
        {
            get { return _material; }
            set
            {
                _material = value;
                OnPropertyChanged(nameof(Material));
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool IsEditMode => Material?.Артикул_сырья != 0;

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Action? CloseAction { get; set; }

        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();

        // Список разрешенных единиц измерения
        public List<string> AllowedUnits { get; } = new List<string>
        {
            "шт.", "м", "кг", "л", "пог. м", "лист", "рул"
        };

        public MaterialStockViewModel(Material material)
        {
            Material = material;
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Save(object obj)
        {
            CloseAction?.Invoke();
        }

        private bool CanSave(object obj)
        {
            ValidateAllMaterialProperties();
            return !HasErrors;
        }

        private void Cancel(object obj)
        {
            Material = null;
            CloseAction?.Invoke();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #region IDataErrorInfo Implementation

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string error = null;

                if (Material == null) return null;

                switch (columnName)
                {
                    case nameof(Material.Наименование_материала):
                        if (string.IsNullOrWhiteSpace(Material.Наименование_материала))
                            error = "Наименование материала не может быть пустым.";
                        else if (Material.Наименование_материала.Length > 100)
                            error = "Наименование материала не может превышать 100 символов.";
                        break;
                    case nameof(Material.Единица_измерения):
                        if (string.IsNullOrWhiteSpace(Material.Единица_измерения))
                            error = "Единица измерения не может быть пустой.";
                        else if (Material.Единица_измерения.Length > 20)
                            error = "Единица измерения не может превышать 20 символов.";
                        else if (!AllowedUnits.Contains(Material.Единица_измерения))
                            error = "Недопустимая единица измерения.";
                        break;
                    case nameof(Material.Количество_в_наличии):
                        if (Material.Количество_в_наличии < 0)
                            error = "Количество в наличии не может быть отрицательным.";
                        break;
                }

                bool hadError = Errors.ContainsKey(columnName);

                if (error == null)
                {
                    if (hadError)
                    {
                        Errors.Remove(columnName);
                        OnPropertyChanged(nameof(Errors));
                        OnPropertyChanged(nameof(HasErrors));
                    }
                }
                else
                {
                    if (hadError)
                    {
                        if (Errors[columnName] != error)
                        {
                            Errors[columnName] = error;
                            OnPropertyChanged(nameof(Errors));
                        }
                    }
                    else
                    {
                        Errors.Add(columnName, error);
                        OnPropertyChanged(nameof(Errors));
                        OnPropertyChanged(nameof(HasErrors));
                    }
                }

                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                return error;
            }
        }

        public bool HasErrors => Errors.Any();

        #endregion

        private void ValidateAllMaterialProperties()
        {
            if (Material == null) return;

            _ = this[nameof(Material.Наименование_материала)];
            _ = this[nameof(Material.Единица_измерения)];
            _ = this[nameof(Material.Количество_в_наличии)];

            OnPropertyChanged(nameof(HasErrors));
        }
    }

    // RelayCommand остается таким же, как в NomenclatureViewModel
    // public class RelayCommand : ICommand
    // {
    //     // ... (полная реализация RelayCommand)
    // }
}
