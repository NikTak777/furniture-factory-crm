using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;
using OxyPlot;
using OxyPlot.Series;
using System.Windows.Media;

namespace FurnitureCRMClient.Views
{
    public partial class MaterialNeedsAnalysisUserControl : UserControl, INotifyPropertyChanged
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public ObservableCollection<MaterialNeedItemDto> MaterialNeeds { get; set; } = new();
        public ObservableCollection<LegendItemDto> LegendItems { get; set; } = new();
        public ObservableCollection<LegendItemDto> TopLegendItems { get; set; } = new();

        // Палитра из 10 цветов, согласованная с вкладкой "Анализ отчётов о производстве"
        private static readonly OxyColor[] Palette = new[]
        {
            OxyColor.FromRgb(91, 155, 213),    // синий
            OxyColor.FromRgb(237, 125, 49),    // оранжевый
            OxyColor.FromRgb(133, 83, 166),    // фиолетовый
            OxyColor.FromRgb(255, 192, 0),     // жёлтый
            OxyColor.FromRgb(84, 130, 53),     // более тёмный зелёный
            OxyColor.FromRgb(68, 114, 196),    // тёмно-синий
            OxyColor.FromRgb(165, 165, 165),   // серый
            OxyColor.FromRgb(192, 128, 64),    // тёплый коричневато-оранжевый
            OxyColor.FromRgb(255, 153, 204),   // розовый
            OxyColor.FromRgb(146, 208, 80)     // ярко-зелёный
        };

        private bool _hasData = false;
        public bool HasData
        {
            get => _hasData;
            set
            {
                if (_hasData != value)
                {
                    _hasData = value;
                    OnPropertyChanged(nameof(HasData));
                }
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public MaterialNeedsAnalysisUserControl(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            this.DataContext = this;
            HasData = false; // Изначально данных нет
        }

        private async void RefreshAnalysis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var from = FromDatePicker.SelectedDate;
                var to = ToDatePicker.SelectedDate;

                string url = $"{ApiBaseUrl}reports/material-needs";
                var queryParts = new System.Collections.Generic.List<string>();
                if (from.HasValue)
                    queryParts.Add("from=" + from.Value.ToString("yyyy-MM-dd"));
                if (to.HasValue)
                    queryParts.Add("to=" + to.Value.ToString("yyyy-MM-dd"));
                if (queryParts.Count > 0)
                    url += "?" + string.Join("&", queryParts);

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    HasData = false;
                    var errorText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errorText))
                        MessageBox.Show(errorText, "Ошибка получения анализа потребности", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        MessageBox.Show($"Ошибка получения анализа потребности: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var items = JsonSerializer.Deserialize<System.Collections.Generic.List<MaterialNeedItemDto>>(json, options) ?? new();

                MaterialNeeds.Clear();
                foreach (var item in items)
                    MaterialNeeds.Add(item);

                BuildPieChart();
                HasData = MaterialNeeds.Count > 0;

                // Топ-10 материалов по потреблению за последние 3 месяца
                var usageResponse = await _httpClient.GetAsync($"{ApiBaseUrl}reports/material-usage");
                if (usageResponse.IsSuccessStatusCode)
                {
                    var usageJson = await usageResponse.Content.ReadAsStringAsync();
                    var usageItems = JsonSerializer.Deserialize<System.Collections.Generic.List<MaterialUsageItemDto>>(usageJson, options) ?? new();
                    BuildTopMaterialsChart(usageItems);
                }
            }
            catch (HttpRequestException ex)
            {
                HasData = false;
                MessageBox.Show($"Ошибка связи с сервером при получении анализа потребности: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                HasData = false;
                MessageBox.Show($"Ошибка обработки данных анализа потребности: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                HasData = false;
                MessageBox.Show($"Непредвиденная ошибка при получении анализа потребности: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildPieChart()
        {
            var model = new PlotModel();

            var pie = new PieSeries
            {
                StrokeThickness = 1,
                InsideLabelPosition = 0.7,
                AngleSpan = 360,
                StartAngle = 0,
                // Подписи внутри с названием материала
                InsideLabelFormat = "{0}",
                OutsideLabelFormat = null
            };

            // Берём только материалы с положительной потребностью
            var positiveItems = new System.Collections.Generic.List<MaterialNeedItemDto>();
            foreach (var item in MaterialNeeds)
            {
                if (item.RequiredQuantity > 0)
                    positiveItems.Add(item);
            }

            if (positiveItems.Count == 0)
            {
                NeedsPiePlot.Model = model;
                return;
            }

            double totalRequired = 0;
            foreach (var item in positiveItems)
                totalRequired += item.RequiredQuantity;

            if (totalRequired <= 0)
            {
                NeedsPiePlot.Model = model;
                return;
            }

            const double threshold = 0.03; // 3%
            double otherTotal = 0;

            LegendItems.Clear();

            int colorIndex = 0;

            foreach (var item in positiveItems)
            {
                double fraction = item.RequiredQuantity / totalRequired;
                if (fraction < threshold)
                {
                    otherTotal += item.RequiredQuantity;
                }
                else
                {
                    var color = Palette[colorIndex % Palette.Length];
                    pie.Slices.Add(new PieSlice(item.MaterialName, item.RequiredQuantity)
                    {
                        Fill = color
                    });
                    LegendItems.Add(new LegendItemDto
                    {
                        Label = item.MaterialName,
                        Color = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))
                    });
                    colorIndex++;
                }
            }

            if (otherTotal > 0)
            {
                var color = Palette[colorIndex % Palette.Length];
                var otherSlice = new PieSlice("Другое", otherTotal) { Fill = color };
                pie.Slices.Add(otherSlice);
                LegendItems.Add(new LegendItemDto
                {
                    Label = "Другое",
                    Color = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))
                });
            }

            model.Series.Add(pie);
            NeedsPiePlot.Model = model;
        }

        private void BuildTopMaterialsChart(System.Collections.Generic.List<MaterialUsageItemDto> usageItems)
        {
            var model = new PlotModel();

            var pie = new PieSeries
            {
                StrokeThickness = 1,
                InsideLabelPosition = 0.7,
                AngleSpan = 360,
                StartAngle = 0,
                // внутри показываем только процент доли, подписи — в легенде
                // {2} — процент от суммы в OxyPlot PieSeries
                InsideLabelFormat = "{2:0}%",
                OutsideLabelFormat = null
            };

            TopLegendItems.Clear();

            var filtered = new System.Collections.Generic.List<MaterialUsageItemDto>();
            foreach (var u in usageItems)
            {
                if (u.TotalRequired > 0)
                    filtered.Add(u);
            }

            if (filtered.Count == 0)
            {
                TopMaterialsPiePlot.Model = model;
                return;
            }

            // Сортируем по объёму и берём топ-9, остальное в "Другое"
            filtered.Sort((a, b) => b.TotalRequired.CompareTo(a.TotalRequired));

            double total = 0;
            foreach (var u in filtered)
                total += u.TotalRequired;

            if (total <= 0)
            {
                TopMaterialsPiePlot.Model = model;
                return;
            }

            int colorIndex = 0;

            double otherTotal = 0;

            for (int i = 0; i < filtered.Count; i++)
            {
                var u = filtered[i];
                if (i < 9)
                {
                    var color = Palette[colorIndex % Palette.Length];
                    pie.Slices.Add(new PieSlice(u.MaterialName, u.TotalRequired)
                    {
                        Fill = color
                    });
                    TopLegendItems.Add(new LegendItemDto
                    {
                        Label = u.MaterialName,
                        Color = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))
                    });
                    colorIndex++;
                }
                else
                {
                    otherTotal += u.TotalRequired;
                }
            }

            if (otherTotal > 0)
            {
                var color = Palette[colorIndex % Palette.Length];
                pie.Slices.Add(new PieSlice("Другое", otherTotal)
                {
                    Fill = color
                });
                TopLegendItems.Add(new LegendItemDto
                {
                    Label = "Другое",
                    Color = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))
                });
            }

            model.Series.Add(pie);
            TopMaterialsPiePlot.Model = model;
        }
    }

    public class MaterialNeedItemDto
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int QuantityAvailable { get; set; }
        public int RequiredQuantity { get; set; }
        public int Deficit { get; set; }
    }

    public class LegendItemDto
    {
        public string Label { get; set; } = string.Empty;
        public SolidColorBrush Color { get; set; } = Brushes.Transparent;
    }

    public class MaterialUsageItemDto
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int TotalRequired { get; set; }
    }
}


