using System;
using System.ComponentModel;
using System.Windows.Input;
using FurnitureCRMClient.Models;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;

namespace FurnitureCRMClient.ViewModels
{
    public class NomenclatureViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Nomenclature _nomenclature;
        public Nomenclature Nomenclature
        {
            get { return _nomenclature; }
            set
            {
                _nomenclature = value;
                OnPropertyChanged(nameof(Nomenclature));
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Action? CloseAction { get; set; }

        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();

        // Список разрешенных категорий
        public List<string> AllowedCategories { get; } = new List<string>
        {
            "Мебель для дома", "Офисная мебель", "Кухонная мебель",
            "Детская мебель", "Мягкая мебель", "Корпусная мебель",
            "Спальная мебель", "Мебель для ванной"
        };

        public ObservableCollection<SelectableMaterialViewModel> AvailableMaterials { get; set; }

        // Отфильтрованный список материалов для отображения (с учётом поиска)
        public ObservableCollection<SelectableMaterialViewModel> FilteredMaterials { get; set; } = new ObservableCollection<SelectableMaterialViewModel>();

        private string _materialSearchText = string.Empty;
        public string MaterialSearchText
        {
            get => _materialSearchText;
            set
            {
                if (_materialSearchText != value)
                {
                    _materialSearchText = value;
                    OnPropertyChanged(nameof(MaterialSearchText));
                    ApplyMaterialFilter();
                }
            }
        }

        public NomenclatureViewModel(Nomenclature nomenclature)
        {
            Nomenclature = nomenclature;
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);

            AvailableMaterials = new ObservableCollection<SelectableMaterialViewModel>();

            LoadAvailableMaterials();
        }

        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        private async void LoadAvailableMaterials()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}materials");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var materials = System.Text.Json.JsonSerializer.Deserialize<List<Material>>(jsonString, options);

                AvailableMaterials.Clear();
                if (materials != null)
                {
                    foreach (var material in materials)
                    {
                        // Проверяем, есть ли этот материал уже в спецификации номенклатуры
                        SpecificationClient? existingSpec = Nomenclature.Specifications?.FirstOrDefault(s => s.Артикул_сырья == material.Артикул_сырья);
                        bool isSelected = existingSpec != null;
                        int quantity = existingSpec?.Количество ?? 1; // Используем количество из спецификации или 1 по умолчанию

                        AvailableMaterials.Add(new SelectableMaterialViewModel(material, isSelected, quantity));
                    }
                }

                // После загрузки обновляем отфильтрованный список
                ApplyMaterialFilter();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки материалов: {ex.Message}");
            }
        }

        private void InitializeSelectedSpecifications()
        {
            // Этот метод теперь не нужен, так как логика инициализации перенесена в LoadAvailableMaterials
        }

        private void Save(object obj)
        {
            Nomenclature.Specifications = AvailableMaterials
                .Where(sm => sm.IsSelected)
                .Select(sm => new SpecificationClient
                {
                    Артикул_сырья = sm.Material.Артикул_сырья,
                    Количество = sm.Quantity // Используем свойство Quantity
                }).ToList();
            CloseAction?.Invoke();
        }

        private bool CanSave(object obj)
        {
            ValidateAllNomenclatureProperties();
            ValidateMaterialsSelected(); // Добавляем вызов метода проверки материалов
            return !HasErrors;
        }

        private void Cancel(object obj)
        {
            Nomenclature = null;
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

                if (Nomenclature == null) return null;

                switch (columnName)
                {
                    case nameof(Nomenclature.Наименование):
                        if (string.IsNullOrWhiteSpace(Nomenclature.Наименование))
                            error = "Наименование не может быть пустым.";
                        else if (Nomenclature.Наименование.Length > 100)
                            error = "Наименование не может превышать 100 символов.";
                        break;
                    case nameof(Nomenclature.Категория):
                        if (string.IsNullOrWhiteSpace(Nomenclature.Категория))
                            error = "Категория не может быть пустой.";
                        else if (Nomenclature.Категория.Length > 50)
                            error = "Категория не может превышать 50 символов.";
                        else if (!AllowedCategories.Contains(Nomenclature.Категория))
                            error = "Недопустимая категория.";
                        break;
                    case nameof(Nomenclature.Цвет):
                        if (string.IsNullOrWhiteSpace(Nomenclature.Цвет))
                            error = "Цвет не может быть пустым.";
                        else if (Nomenclature.Цвет.Length > 50)
                            error = "Цвет не может превышать 50 символов.";
                        break;
                    case nameof(Nomenclature.Размеры):
                        if (string.IsNullOrWhiteSpace(Nomenclature.Размеры))
                            error = "Размеры не могут быть пустыми.";
                        else if (Nomenclature.Размеры.Length > 50)
                            error = "Размеры не может превышать 50 символов.";
                        break;
                    case nameof(Nomenclature.Стоимость):
                        if (Nomenclature.Стоимость <= 0)
                            error = "Стоимость должна быть больше 0.";
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

        private void ValidateAllNomenclatureProperties()
        {
            if (Nomenclature == null) return;

            _ = this[nameof(Nomenclature.Наименование)];
            _ = this[nameof(Nomenclature.Категория)];
            _ = this[nameof(Nomenclature.Цвет)];
            _ = this[nameof(Nomenclature.Размеры)];
            _ = this[nameof(Nomenclature.Стоимость)];
            
            OnPropertyChanged(nameof(HasErrors));
        }

        private void ValidateMaterialsSelected()
        {
            const string materialsErrorKey = "MaterialsSelected";
            if (AvailableMaterials == null || !AvailableMaterials.Any(sm => sm.IsSelected))
            {
                if (!Errors.ContainsKey(materialsErrorKey))
                {
                    Errors.Add(materialsErrorKey, "Необходимо выбрать хотя бы одно сырьё.");
                    OnPropertyChanged(nameof(Errors));
                    OnPropertyChanged(nameof(HasErrors));
                }
            }
            else
            {
                if (Errors.ContainsKey(materialsErrorKey))
                {
                    Errors.Remove(materialsErrorKey);
                    OnPropertyChanged(nameof(Errors));
                    OnPropertyChanged(nameof(HasErrors));
                }
            }
        }

        /// <summary>
        /// Применяет фильтр по названию материала к списку отображаемых элементов.
        /// </summary>
        private void ApplyMaterialFilter()
        {
            if (FilteredMaterials == null)
                FilteredMaterials = new ObservableCollection<SelectableMaterialViewModel>();

            FilteredMaterials.Clear();

            if (AvailableMaterials == null)
                return;

            var search = _materialSearchText?.Trim();
            IEnumerable<SelectableMaterialViewModel> source = AvailableMaterials;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                source = source.Where(sm =>
                    !string.IsNullOrWhiteSpace(sm.Material.Наименование_материала) &&
                    sm.Material.Наименование_материала.ToLower().Contains(lower));
            }

            foreach (var item in source)
            {
                FilteredMaterials.Add(item);
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class SelectableMaterialViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Material Material { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                    CommandManager.InvalidateRequerySuggested(); // 🔹 обновляем кнопку "Сохранить"
                }
            }
        }

        private int _quantity = 1; // Добавляем свойство для количества
        public int Quantity
        {
            get => _quantity;
            set
            {
                // Не допускаем нулевые и отрицательные значения количества.
                int normalized = value < 1 ? 1 : value;

                if (_quantity != normalized)
                {
                    _quantity = normalized;
                    OnPropertyChanged(nameof(Quantity));
                }
            }
        }

        public SelectableMaterialViewModel(Material material, bool isSelected = false, int quantity = 1)
        {
            Material = material;
            IsSelected = isSelected;
            Quantity = quantity; // Устанавливаем начальное количество
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
