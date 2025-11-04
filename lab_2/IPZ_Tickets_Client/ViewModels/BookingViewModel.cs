using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using IPZ_Tickets_Client.Models;

namespace IPZ_Tickets_Client.ViewModels
{
    public class BookingViewModel : INotifyPropertyChanged
    {
        public Ticket Ticket { get; set; }
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public BookingViewModel(Ticket ticket)
        {
            Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket));
            ConfirmCommand = new RelayCommand(_ => Confirm());
            CancelCommand = new RelayCommand(window => ((Window)window).Close());
        }

        private void Confirm()
        {
            try
            {
                // Тут має бути виклик платіжного модуля/серверу. Для ЛР2 — повідомлення.
                MessageBox.Show($"Квиток (ID {Ticket.Id}) успішно заброньовано.", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                // Закрити вікно:
                foreach (Window w in Application.Current.Windows)
                    if (w is Views.BookingWindow) { w.Close(); break; }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при бронюванні: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
