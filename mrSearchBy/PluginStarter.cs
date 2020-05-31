namespace mrSearchBy
{
    using ModPlus;

    /// <inheritdoc />
    public class PluginStarter : IRengaFunction
    {
        /// <inheritdoc/>
        public void Start()
        {
            if (MainWindow.IsOpen)
                return;
#if !DEBUG
            ModPlusAPI.Statistic.SendCommandStarting(ModPlusConnector.Instance);
#endif
            var mainViewModel = new MainViewModel();
            var mainWindow = new MainWindow { DataContext = mainViewModel };
            mainWindow.Show();
        }
    }
}
