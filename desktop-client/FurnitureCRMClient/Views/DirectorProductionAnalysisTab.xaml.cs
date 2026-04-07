using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FurnitureCRMClient.Models;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using OxyPlot.Legends;

namespace FurnitureCRMClient.Views
{
    public partial class DirectorProductionAnalysisTab : UserControl
    {
        private readonly AuthenticatedUser _currentUser;

        public ObservableCollection<ProductionReportOrderItem> Orders { get; set; } = new();
        public ObservableCollection<LegendItemDto> LegendItems { get; set; } = new();

        public DirectorProductionAnalysisTab(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            this.DataContext = this;
            Orders.Clear(); // Убеждаемся, что изначально нет данных
        }

        private void OpenReport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Открыть отчёт о производстве",
                Filter = "CSV файл (*.csv)|*.csv|Текстовый файл (*.txt)|*.txt|Все файлы (*.*)|*.*",
                FileName = ""
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                LoadReportFromFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadReportFromFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length == 0)
            {
                MessageBox.Show("Файл пуст.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Orders.Clear();

            // Парсим заголовок отчёта (первая строка)
            string reportTitle = lines[0];
            string periodInfo = "";
            if (reportTitle.Contains("Период:"))
            {
                var parts = reportTitle.Split(';');
                if (parts.Length >= 5)
                {
                    periodInfo = $"Период: {parts[2]} - {parts[4]}";
                }
            }
            ReportInfoText.Text = periodInfo;

            // Парсим сводку (строки 2-7)
            var summary = new Dictionary<string, string>();
            int summaryStartIndex = 2; // Пропускаем заголовок и пустую строку
            for (int i = summaryStartIndex; i < Math.Min(summaryStartIndex + 6, lines.Length); i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    break;
                var parts = lines[i].Split(';');
                if (parts.Length >= 2)
                {
                    summary[parts[0].Trim()] = parts[1].Trim();
                }
            }

            // Обновляем сводку
            TotalOrdersText.Text = summary.ContainsKey("Всего заказов") ? $"Всего заказов: {summary["Всего заказов"]}" : "";
            InProcessingText.Text = summary.ContainsKey("В обработке") ? $"В обработке: {summary["В обработке"]}" : "";
            InProductionText.Text = summary.ContainsKey("В производстве") ? $"В производстве: {summary["В производстве"]}" : "";
            CompletedText.Text = summary.ContainsKey("Выполнено") ? $"Выполнено: {summary["Выполнено"]}" : "";
            CancelledText.Text = summary.ContainsKey("Отменено") ? $"Отменено: {summary["Отменено"]}" : "";
            TotalRevenueText.Text = summary.ContainsKey("Выручка (выполненные)") ? $"Выручка: {summary["Выручка (выполненные)"]}" : "";

            // Находим строку с заголовками таблицы
            int headerIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Номер заказа") && lines[i].Contains("Дата оформления"))
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex == -1)
            {
                MessageBox.Show("Не найдена строка заголовков таблицы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Парсим данные заказов
            for (int i = headerIndex + 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                var parts = lines[i].Split(';');
                if (parts.Length < 6)
                    continue;

                try
                {
                    var order = new ProductionReportOrderItem
                    {
                        OrderId = int.Parse(parts[0].Trim()),
                        OrderDate = DateTime.ParseExact(parts[1].Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture),
                        ProductName = parts[2].Trim(),
                        Quantity = int.Parse(parts[3].Trim()),
                        Status = parts[4].Trim(),
                        TotalPrice = int.Parse(parts[5].Trim())
                    };
                    Orders.Add(order);
            }
            catch (Exception ex)
            {
                    // Пропускаем некорректные строки
                    System.Diagnostics.Debug.WriteLine($"Ошибка парсинга строки {i}: {ex.Message}");
                }
            }

            // Обновляем графики
            UpdateCharts();
        }

        private void UpdateCharts()
        {
            // График по статусам (круговая диаграмма)
            var statusModel = new PlotModel
            {
                PlotAreaBorderThickness = new OxyThickness(0),
                Padding = new OxyThickness(40) // Увеличиваем отступы для внешних подписей
            };
            // Легенда отображается через кастомный ItemsControl в XAML

            // Общая палитра из 10 приглушённых, но достаточно ярких цветов,
            // используемая для "остальных" статусов и для столбиковой диаграммы
            var palette = new[]
            {
                OxyColor.FromRgb(91, 155, 213),    // синий
                OxyColor.FromRgb(237, 125, 49),    // оранжевый
                OxyColor.FromRgb(133, 83, 166),    // фиолетовый
                OxyColor.FromRgb(255, 192, 0),     // жёлтый
                OxyColor.FromRgb(84, 130, 53),     // более тёмный зелёный (для "других", не выполненных)
                OxyColor.FromRgb(68, 114, 196),    // тёмно-синий
                OxyColor.FromRgb(165, 165, 165),   // серый
                OxyColor.FromRgb(192, 128, 64),    // тёплый коричневато-оранжевый
                OxyColor.FromRgb(255, 153, 204),   // розовый
                OxyColor.FromRgb(146, 208, 80)     // ярко-зелёный
            };

            var statusSeries = new PieSeries 
            { 
                InsideLabelPosition = 0.7, 
                AngleSpan = 360, 
                StartAngle = 0,
                InsideLabelFormat = null, // Убираем подписи внутри сектора
                OutsideLabelFormat = "{2:0}%", // Показываем только процент снаружи с линиями (палочками)
                StrokeThickness = 1
            };

            var statusGroups = Orders.GroupBy(o => o.Status).ToList();

            // Очищаем легенду перед заполнением
            LegendItems.Clear();

            int colorIndex = 0;
            foreach (var group in statusGroups)
            {
                // Специальные цвета для основных статусов:
                // - "Выполнен" (и вариации) — зелёный
                // - "Отменен" (и вариации) — красный
                // Остальные статусы — из палитры по кругу
                OxyColor color;
                if (!string.IsNullOrWhiteSpace(group.Key) &&
                    (group.Key.Equals("Выполнен", StringComparison.OrdinalIgnoreCase) ||
                     group.Key.Equals("Выполнено", StringComparison.OrdinalIgnoreCase) ||
                     group.Key.Contains("Выполн", StringComparison.OrdinalIgnoreCase)))
                {
                    color = OxyColor.FromRgb(67, 160, 71); // зелёный для выполненных
                }
                else if (!string.IsNullOrWhiteSpace(group.Key) &&
                         (group.Key.Equals("Отменен", StringComparison.OrdinalIgnoreCase) ||
                          group.Key.Equals("Отменён", StringComparison.OrdinalIgnoreCase) ||
                          group.Key.Contains("Отмен", StringComparison.OrdinalIgnoreCase)))
                {
                    color = OxyColor.FromRgb(229, 57, 53); // красный для отменённых
                }
                else
                {
                    color = palette[colorIndex % palette.Length];
                }
                // Создаём сектор с названием, которое будет отображаться в легенде
                var slice = new PieSlice(group.Key, group.Count())
                {
                    Fill = color
                };
                statusSeries.Slices.Add(slice);
                
                // Добавляем элемент в легенду
                LegendItems.Add(new LegendItemDto
                {
                    Label = group.Key,
                    Color = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B))
                });
                
                colorIndex++;
            }

            statusModel.Series.Add(statusSeries);
            StatusPiePlot.Model = statusModel;

            // График по товарам (столбчатая диаграмма)
            var productModel = new PlotModel();
            
            // Проверяем все возможные варианты статуса "Выполнено"
            var productGroups = Orders
                .Where(o => o.Status == "Выполнено" || o.Status == "Выполнен" || o.Status.Contains("Выполн"))
                .GroupBy(o => o.ProductName)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(o => o.TotalPrice) })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();

            if (productGroups.Any())
            {
                // Используем CategoryAxis для категорий и создаём столбцы через LineSeries с заливкой
                var barColors = palette;
                int barColorIndex = 0;
                
                var categoryAxis = new CategoryAxis 
                { 
                    Position = AxisPosition.Bottom, 
                    Angle = 0,
                    Title = "Товары"
                };
                
                var valueAxis = new LinearAxis 
                { 
                    Position = AxisPosition.Left, 
                    Title = "Выручка, руб.",
                    Minimum = 0
                };

                int index = 0;
                foreach (var product in productGroups)
                {
                    // Создаём столбец через LineSeries с заливкой
                    double barWidth = 0.6;
                    double xCenter = index;
                    double xLeft = xCenter - barWidth / 2;
                    double xRight = xCenter + barWidth / 2;
                    double yValue = product.Revenue;
                    
                    // Создаём LineSeries для левой стороны столбца
                    var leftLine = new LineSeries
                    {
                        Color = barColors[barColorIndex % barColors.Length],
                        StrokeThickness = 2
                    };
                    leftLine.Points.Add(new DataPoint(xLeft, 0));
                    leftLine.Points.Add(new DataPoint(xLeft, yValue));
                    
                    // Создаём LineSeries для верхней стороны столбца
                    var topLine = new LineSeries
                    {
                        Color = barColors[barColorIndex % barColors.Length],
                        StrokeThickness = 2
                    };
                    topLine.Points.Add(new DataPoint(xLeft, yValue));
                    topLine.Points.Add(new DataPoint(xRight, yValue));
                    
                    // Создаём LineSeries для правой стороны столбца
                    var rightLine = new LineSeries
                    {
                        Color = barColors[barColorIndex % barColors.Length],
                        StrokeThickness = 2
                    };
                    rightLine.Points.Add(new DataPoint(xRight, yValue));
                    rightLine.Points.Add(new DataPoint(xRight, 0));
                    
                    // Создаём AreaSeries для заливки столбца
                    var fillArea = new AreaSeries
                    {
                        Fill = barColors[barColorIndex % barColors.Length],
                        Color = OxyColors.Transparent,
                        StrokeThickness = 0
                    };
                    fillArea.Points.Add(new DataPoint(xLeft, 0));
                    fillArea.Points.Add(new DataPoint(xLeft, yValue));
                    fillArea.Points.Add(new DataPoint(xRight, yValue));
                    fillArea.Points.Add(new DataPoint(xRight, 0));
                    fillArea.Points2.Add(new DataPoint(xLeft, 0));
                    fillArea.Points2.Add(new DataPoint(xRight, 0));
                    
                    productModel.Series.Add(fillArea);
                    productModel.Series.Add(leftLine);
                    productModel.Series.Add(topLine);
                    productModel.Series.Add(rightLine);
                    categoryAxis.Labels.Add(product.Name);
                    
                    index++;
                    barColorIndex++;
                }

                productModel.Axes.Add(categoryAxis);
                productModel.Axes.Add(valueAxis);
            }
            else
            {
                // Если нет данных, создаём пустую модель
                productModel.Axes.Add(new CategoryAxis { Position = AxisPosition.Bottom, Title = "Товары" });
                productModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Выручка, руб." });
            }
            
            ProductsBarPlot.Model = productModel;
        }
    }

    public class CountToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
