using System;
using System.Text.Json.Serialization;
using System.ComponentModel; // Добавляем
using System.Runtime.CompilerServices; // Добавляем

namespace FurnitureCRMClient.Models
{
    public class Order : INotifyPropertyChanged // Добавляем INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [JsonPropertyName("orderId")]
        public int OrderId { get; set; }
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }
        [JsonPropertyName("quantity")]
        private int _quantity = 1; // Делаем поле, чтобы можно было вызывать OnPropertyChanged
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                }
            }
        }
        // Дата оформления задаётся на стороне БД (DEFAULT DATETIME('now','localtime')),
        // поэтому при создании заказа мы не отправляем это поле.
        [JsonPropertyName("orderDate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? OrderDate { get; set; }
        [JsonPropertyName("staffId")]
        public int StaffId { get; set; }
        [JsonPropertyName("completionDate")]
        public DateTime? CompletionDate { get; set; }
        [JsonPropertyName("clientId")]
        public int ClientId { get; set; }
        [JsonPropertyName("totalPrice")]
        private int _totalPrice; // Делаем поле для TotalPrice
        public int TotalPrice
        {
            get => _totalPrice;
            set
            {
                if (_totalPrice != value)
                {
                    _totalPrice = value;
                    OnPropertyChanged();
                }
            }
        }
        [JsonPropertyName("status")]
        public string Status { get; set; } = "В обработке";

        // Дополнительные свойства для отображения в UI (не мапятся напрямую к API)
        [JsonIgnore]
        public string ProductName { get; set; } = string.Empty; 
        [JsonIgnore]
        public string StaffFullName { get; set; } = string.Empty;
        [JsonIgnore]
        public string ClientFullName { get; set; } = string.Empty;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
