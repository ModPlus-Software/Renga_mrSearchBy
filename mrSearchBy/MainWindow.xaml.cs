namespace mrSearchBy
{
    using System.Windows;

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static MainWindow _instance;
        private static bool _isOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetFunctionLocalName(ModPlusConnector.Instance);

            Loaded += (sender, args) =>
            {
                _instance = this;
                _isOpen = true;
            };
            Closed += (sender, args) =>
            {
                _instance = null;
                _isOpen = false;
            };
        }

        /// <summary>
        /// Экземпляр окна уже открыт
        /// </summary>
        public static bool IsOpen
        {
            get
            {
                if (_isOpen)
                {
                    if (_instance.WindowState == WindowState.Minimized)
                        _instance.WindowState = WindowState.Normal;

                    _instance.Focus();
                }

                return _isOpen;
            }
        }

        private void BtQuantityMap_OnClick(object sender, RoutedEventArgs e)
        {
            PopupQuantityMap.IsOpen = !PopupQuantityMap.IsOpen;
        }
    }
}
