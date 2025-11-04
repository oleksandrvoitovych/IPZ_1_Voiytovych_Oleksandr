using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using IPZ_Tickets_Client.Models;
using IPZ_Tickets_Client.Views;

namespace IPZ_Tickets_Client.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _from;
        private string _to;
        private DateTime _date = DateTime.Today;
        public string From { get => _from; set { _from = value; OnPropertyChanged(nameof(From)); } }
        public string To { get => _to; set { _to = value; OnPropertyChanged(nameof(To)); } }
        public DateTime Date { get => _date; set { _date = value; OnPropertyChanged(nameof(Date)); } }

        public ObservableCollection<Ticket> SearchResults { get; } = new ObservableCollection<Ticket>();

        private Ticket _selectedTicket;
        public Ticket SelectedTicket { get => _selectedTicket; set { _selectedTicket = value; OnPropertyChanged(nameof(SelectedTicket)); } }

        public ICommand SearchCommand { get; }
        public ICommand OpenBookingCommand { get; }

        public MainViewModel()
        {
            SearchCommand = new RelayCommand(_ => Search(), _ => CanSearch());
            OpenBookingCommand = new RelayCommand(_ => OpenBooking(), _ => SelectedTicket != null);
        }

        private bool CanSearch() => !string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(To);

        private void Search()
        {
            try
            {
                // Очистити попередні результати
                SearchResults.Clear();

                // --- ТУТ має бути виклик до сервера. Для ЛР2 робимо мок-дані:
                SearchResults.Add(new Ticket { Id = 1, From = From, To = To, Date = Date.ToShortDateString(), Time = "08:00", Seat = 12, Price = 199.50m });
                SearchResults.Add(new Ticket { Id = 2, From = From, To = To, Date = Date.ToShortDateString(), Time = "12:30", Seat = 5, Price = 249.00m });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при пошуку: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenBooking()
        {
            try
            {
                var vm = new BookingViewModel(SelectedTicket);
                var wnd = new BookingWindow { DataContext = vm };
                wnd.Owner = Application.Current.MainWindow;
                wnd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося відкрити бронювання: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
