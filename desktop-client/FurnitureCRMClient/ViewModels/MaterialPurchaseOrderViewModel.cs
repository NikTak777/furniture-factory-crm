using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using FurnitureCRMClient.Models;
using System.Collections.ObjectModel; // Добавляем для ObservableCollection

namespace FurnitureCRMClient.ViewModels
{
    public class MaterialPurchaseOrderViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private MaterialOrder _order;
        public MaterialOrder Order
        {
            get => _order;
            set
            {
                _order = value;
                OnPropertyChanged(nameof(Order));
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                // При изменении Order, обновляем SelectedMaterial
                if (_order != null)
                {
                    SelectedMaterial = _allMaterialsList.FirstOrDefault(m => m.Артикул_сырья == _order.Артикул_сырья);
                }
                else
                {
                    SelectedMaterial = null;
                }
            }
        }

        private List<Material> _allMaterialsList; // Полный список материалов
        private ObservableCollection<Material> _filteredMaterialsList; // Отфильтрованный список для ComboBox
        public ObservableCollection<Material> FilteredMaterialsList
        {
            get => _filteredMaterialsList;
            set
            {
                _filteredMaterialsList = value;
                OnPropertyChanged(nameof(FilteredMaterialsList));
            }
        }

        private string _searchText = string.Empty; // Текст для поиска
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    FilterMaterials(); // Вызываем фильтрацию при изменении текста поиска
                }
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public Action? CloseAction { get; set; }
        public Dictionary<string, string> Errors { get; } = new();

        public bool IsEditMode => Order?.Номер_заказа != 0;
        public bool HasErrors => Errors.Any();

        public List<string> StatusOptions { get; } = new List<string> { "Ожидает поставки", "Доставлен", "Отменен" };

        // Список существующих поставщиков (для подсказок в ComboBox)
        public List<string> ExistingSuppliers { get; }

        // Новое свойство для привязки текста количества
        private string _quantityText = string.Empty;
        public string QuantityText
        {
            get => _quantityText;
            set
            {
                if (_quantityText != value)
                {
                    _quantityText = value;
                    OnPropertyChanged(nameof(QuantityText));
                    
                    // Попытка распарсить и обновить Order.Количество
                    if (int.TryParse(value, out int parsedQuantity) && parsedQuantity >= 1)
                    {
                        if (Order != null) Order.Количество = parsedQuantity;
                    }
                    else
                    {
                        if (Order != null) Order.Количество = 0; // Сброс для активации валидации
                    }
                    ValidateAllProperties(); // Перепроверить все свойства, включая количество
                }
            }
        }

        public MaterialPurchaseOrderViewModel(MaterialOrder order, List<Material> materials, IEnumerable<string>? existingSuppliers = null)
        {
            _allMaterialsList = materials; // Сохраняем полный список
            FilteredMaterialsList = new ObservableCollection<Material>(_allMaterialsList);
            ExistingSuppliers = existingSuppliers?
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList()
                ?? new List<string>();

            Order = order; // Это вызовет сеттер Order, который обновит SelectedMaterial и SearchText
            QuantityText = order?.Количество.ToString() ?? string.Empty; // Инициализация QuantityText

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void FilterMaterials()
        {
            FilteredMaterialsList.Clear();
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var material in _allMaterialsList)
                {
                    FilteredMaterialsList.Add(material);
                }
            }
            else
            {
                var lowerSearchText = SearchText.ToLower();
                foreach (var material in _allMaterialsList.Where(m => m.Наименование_материала.ToLower().Contains(lowerSearchText)))
                {
                    FilteredMaterialsList.Add(material);
                }
            }
        }

        private Material? _selectedMaterial; // Новый свойство для привязки к SelectedItem
        public Material? SelectedMaterial
        {
            get => _selectedMaterial;
            set
            {
                if (_selectedMaterial != value)
                {
                    _selectedMaterial = value;
                    OnPropertyChanged(nameof(SelectedMaterial));

                    // Обновляем Order.Артикул_сырья и SearchText на основе выбора
                    if (_selectedMaterial != null)
                    {
                        if (Order != null)
                        {
                            Order.Артикул_сырья = _selectedMaterial.Артикул_сырья; // Обновляем Order.Артикул_сырья
                        }
                        
                        // Обновляем SearchText только если он отличается, чтобы избежать рекурсии
                        if (_searchText != _selectedMaterial.Наименование_материала)
                        {
                            _searchText = _selectedMaterial.Наименование_материала;
                            OnPropertyChanged(nameof(SearchText)); // Уведомляем UI об изменении текста ComboBox
                        }
                    }
                    else
                    {
                        if (Order != null)
                        {
                            Order.Артикул_сырья = 0; // Если ничего не выбрано, сбрасываем
                        }
                        
                        if (!string.IsNullOrEmpty(_searchText))
                        {
                            _searchText = string.Empty;
                            OnPropertyChanged(nameof(SearchText)); // Очищаем текст ComboBox
                        }
                    }
                    ValidateMaterialSelection(); // Повторная валидация после изменения выбора
                }
            }
        }

        private void Save(object obj)
        {
            // Перед сохранением убедимся, что Артикул_сырья установлен корректно
            ValidateMaterialSelection();
            if (HasErrors) return; // Не сохраняем, если есть ошибки
            
            CloseAction?.Invoke();
        }

        private bool CanSave(object obj)
        {
            ValidateAllProperties();
            // Дополнительная проверка на валидность выбранного сырья
            ValidateMaterialSelection();
            return !HasErrors;
        }

        private void ValidateMaterialSelection()
        {
            // Вызываем валидацию для Артикул_сырья, чтобы обновить состояние ошибок
            _ = this[nameof(Order.Артикул_сырья)]; 
        }

        private void Cancel(object obj)
        {
            Order = null;
            CloseAction?.Invoke();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // Валидация
        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                string? error = null;
                if (Order == null) return null;
                switch (columnName)
                {
                    case nameof(Order.Поставщик):
                        if (string.IsNullOrWhiteSpace(Order.Поставщик))
                            error = "Поставщик не может быть пустым.";
                        else if (Order.Поставщик.Length > 100)
                            error = "Поставщик — не более 100 символов.";
                        break;
                    case nameof(Order.Артикул_сырья):
                        // Валидация на основе SelectedMaterial и SearchText
                        if (SelectedMaterial == null || SelectedMaterial.Артикул_сырья <= 0)
                        {
                            error = "Выберите существующее сырьё из списка.";
                        }
                        else if (!SelectedMaterial.Наименование_материала.Equals(SearchText, StringComparison.OrdinalIgnoreCase))
                        {
                            error = "Название сырья не соответствует выбранному элементу.";
                        }
                        break;
                    case nameof(QuantityText):
                        if (string.IsNullOrWhiteSpace(QuantityText))
                        {
                            error = "Количество не может быть пустым.";
                        }
                        else if (!int.TryParse(QuantityText, out int quantity) || quantity < 1)
                        {
                            error = "Количество должно быть положительным числом.";
                        }
                        break;
                    case nameof(Order.Статус):
                        if (!StatusOptions.Contains(Order.Статус))
                            error = "Недопустимый статус заказа.";
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

        private void ValidateAllProperties()
        {
            if (Order == null) return;
            _ = this[nameof(Order.Поставщик)];
            _ = this[nameof(Order.Артикул_сырья)]; // Теперь это будет использовать новую валидацию
            _ = this[nameof(QuantityText)]; // Добавляем валидацию для QuantityText
            _ = this[nameof(Order.Статус)];
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
