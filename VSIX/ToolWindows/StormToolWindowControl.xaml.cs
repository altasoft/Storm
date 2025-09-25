#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AltaSoft.Storm.Generator.Common;
using AltaSoft.Storm.Helpers;
using AltaSoft.Storm.Models;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Data.Core;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Shell;

namespace AltaSoft.Storm.ToolWindows;

public partial class StormToolWindowControl : UserControl
{
    private readonly Action<object> _onSelectAction;
    private ConnectionData? _connectionData;

    public StormToolWindowControl(Action<object> onSelectAction)
    {
        _onSelectAction = onSelectAction;

        InitializeComponent();

        DataContext = this;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        GridStormTypes.SelectedItemChanged += TreeView_SelectedItemChanged;
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        object? selectedObject;

        switch (e.NewValue)
        {
            case StormTypeDef selectedStormType:
                selectedObject = selectedStormType.BindObjectData;
                break;

            case StormPropertyDef selectedProperty:
                selectedObject = selectedProperty.BindColumnData;
                break;

            default:
                return;
        }

        _onSelectAction(selectedObject);
    }

    //private void MainTabControlGotFocus(object sender, RoutedEventArgs e)
    //{
    //    //if (MainTabControl.SelectedContent is UIElement uiElement)
    //    //    uiElement.Focus();
    //}

    //private void button1_Click(object sender, RoutedEventArgs e)
    //{
    //    VS.MessageBox.Show("AltaSoft.GuidAltaSoftStormPackage", "Button clicked");
    //}

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_connectionData is not null) // Already have data
            return;

        InternalRefreshAsync().FireAndForget();
        return;

        async Task InternalRefreshAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _connectionData = await ConnectionDataExt.ReadConnectionDataAsync() ?? await ShowConnectionDialogAsync();

            if (_connectionData is not null)
                await RefreshAsync().ConfigureAwait(false);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
    }

    private async Task<ConnectionData?> ShowConnectionDialogAsync()
    {
        var package = StormPackage.s_packageInstance;

        var dialogFactory =
            await package.GetServiceAsync<IVsDataConnectionDialogFactory, IVsDataConnectionDialogFactory>();
        var providerManager = await package.GetServiceAsync<IVsDataProviderManager, IVsDataProviderManager>();

        var dialog = dialogFactory.CreateConnectionDialog();

        dialog.SaveSelection = false;
        dialog.AddSources((_, provider) => GetDatabaseInfo(provider, providerManager).dbType != DatabaseType.Undefined);

        if (_connectionData is null)
        {
            dialog.SelectedSource = new Guid("067ea0d9-ba62-43f7-9106-34930c60c528");
            dialog.SelectedProvider =
                providerManager.Providers.ContainsKey(new Guid(Properties.Resources.MicrosoftSqlServerDotNetProvider))
                    ? new Guid(Properties.Resources.MicrosoftSqlServerDotNetProvider)
                    : new Guid(Properties.Resources.SqlServerDotNetProvider);
        }
        else
        {
            dialog.SelectedSource = _connectionData.Source;
            dialog.SelectedProvider = _connectionData.Provider;

            dialog.LoadExistingConfiguration(_connectionData.Provider, _connectionData.ConnectionString, false);
        }

        var dialogResult = dialog.ShowDialog(connect: true);

        if (dialogResult is null)
        {
            return _connectionData;
        }

        var result = new ConnectionData(DataProtection.DecryptString(dialogResult.EncryptedConnectionString),
            dialogResult.Source, dialogResult.Provider);

        await result.SaveConnectionDataAsync();

        return result;
    }

    private static (DatabaseType dbType, string providerGuid) GetDatabaseInfo(Guid provider,
        IVsDataProviderManager providerManager)
    {
        var dbType = DatabaseType.Undefined;
        var providerGuid = Guid.Empty.ToString();

        // Find provider
        //var providerManager = await ServiceProvider.GetGlobalServiceAsync<IVsDataProviderManager, IVsDataProviderManager>();

        providerManager.Providers.TryGetValue(provider, out var dp);
        if (dp is not null)
        {
            var providerInvariant = (string)dp.GetProperty("InvariantName");
            dbType = DatabaseType.Undefined;

            if (providerInvariant == "System.Data.SQLite.EF6")
            {
                dbType = DatabaseType.SQLite;
                providerGuid = Properties.Resources.SQLitePrivateProvider;
            }

            if (providerInvariant == "Microsoft.Data.Sqlite")
            {
                dbType = DatabaseType.SQLite;
                providerGuid = Properties.Resources.MicrosoftSQLiteProvider;
            }

            if (providerInvariant == "System.Data.SqlClient")
            {
                dbType = DatabaseType.SQLServer;
                providerGuid = Properties.Resources.SqlServerDotNetProvider;
            }

            if (providerInvariant == "Microsoft.Data.SqlClient")
            {
                dbType = DatabaseType.SQLServer;
                providerGuid = Properties.Resources.MicrosoftSqlServerDotNetProvider;
            }

            if (providerInvariant == "Npgsql")
            {
                dbType = DatabaseType.Npgsql;
                providerGuid = Properties.Resources.NpgsqlProvider;
            }

            if (providerInvariant == "Oracle.ManagedDataAccess.Client")
            {
                dbType = DatabaseType.Oracle;
                providerGuid = Properties.Resources.OracleProvider;
            }

            if (providerInvariant is "Mysql" or "MySql.Data.MySqlClient")
            {
                dbType = DatabaseType.Mysql;
                providerGuid = Properties.Resources.MysqlVSProvider;
            }
        }

        return (dbType, providerGuid);
    }

    private async Task RefreshAsync()
    {
        ProgressBarTypes.IsIndeterminate = true;
        ProgressBarTypes.Value = 0;

        ProgressBarDbObjects.IsIndeterminate = true;
        ProgressBarDbObjects.Value = 0;

        LogListBoxTypes.Items.Clear();
        LogListBoxDbObjects.Items.Clear();

        TabItemLoading.Visibility = Visibility.Visible;
        MainTabControl.SelectedIndex = 0;

        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationTokenSource.Token);

            await VS.StatusBar.ShowMessageAsync("Loading...");
            await VS.StatusBar.StartAnimationAsync(StatusAnimation.Sync);

            TabItemStormTypes.IsEnabled = false;
            TabItemDbObjects.IsEnabled = false;

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                await RefreshInternalAsync(cancellationTokenSource.Token);

                await VS.StatusBar.ShowMessageAsync(string.Empty);
            }
            finally
            {
                TabItemStormTypes.IsEnabled = true;
                TabItemDbObjects.IsEnabled = true;

                Mouse.OverrideCursor = null; // Resets the cursor to the default
                await VS.StatusBar.EndAnimationAsync(StatusAnimation.Sync);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            await VS.StatusBar.ShowMessageAsync("Error: " + ex.Message);
            await VS.MessageBox.ShowErrorAsync("AltaSoft Storm", ex.ToString());
        }

        TabItemLoading.Visibility = Visibility.Collapsed;
        MainTabControl.SelectedIndex = 1;

        //var model = new InfoBarModel(
        //    new[] { new InfoBarTextSpan("The text in the Info Bar. "), new InfoBarHyperlink("Click me") },
        //    KnownMonikers.UpdateDatabase);

        //var infoBar = await VS.InfoBar.CreateAsync(model);
        //if (infoBar is not null)
        //{
        //    infoBar.ActionItemClicked += InfoBar_ActionItemClicked;
        //    await infoBar.TryShowInfoBarUIAsync();
        //}
    }

    private async Task RefreshInternalAsync(CancellationToken cancellationToken)
    {
        if (_connectionData is null)
        {
            throw new InvalidOperationException("No connection data");
        }

        var taskTypes = RoslynHelper.GetStormTypesAsync((name, step, max) => OnProgressTypes(name, step, max, cancellationToken), cancellationToken);

        var taskDbEntities = DbHelper.GetDbEntitiesAsync(_connectionData, (name, step, max) => OnProgressDbObjects(name, step, max, cancellationToken), cancellationToken);

        var tasks = new Task[] { taskTypes, taskDbEntities };

        await Task.WhenAll(tasks);

        if (cancellationToken.IsCancellationRequested)
            cancellationToken.ThrowIfCancellationRequested();

        var stormTypes = await taskTypes;
        var dbEntities = await taskDbEntities;

        OnProgressDbObjects("Loading Procedures and functions...", -1, 100, cancellationToken);

        await DbHelper.GetProceduresAndFunctionsAsync(_connectionData, stormTypes, dbEntities, (name, step, max) => OnProgressDbObjects(name, step, max, cancellationToken), cancellationToken);

        LogListBoxTypes.Items.Add("Comparing...");
        LogListBoxDbObjects.Items.Add("Comparing...");

        await VS.StatusBar.ShowMessageAsync("Comparing...");

        Comparator.Compare(stormTypes, dbEntities, cancellationToken);

        // DetachExpandedEventHandlers(GridDbObjects.Items);

        GridStormTypes.ItemsSource = stormTypes;
        GridStormTypes.TrySelectFirstItem();

        GridDbObjects.ItemsSource = dbEntities;
        GridDbObjects.TrySelectFirstItem();

        //   AttachExpandedEventHandlers(GridDbObjects.Items);
    }

    private void AttachExpandedEventHandlers(ItemCollection items)
    {
        foreach (var item in items)
        {
            if (item is TreeViewItem treeViewItem)
            {
                treeViewItem.Expanded += TreeViewItem_Expanded;
                AttachExpandedEventHandlers(treeViewItem.Items); // Recursive call for child items
            }
            //else
            //if (item is TreeNode treeNode)
            //{
            //    // If your TreeView uses data binding, you may need to create and attach
            //    // TreeViewItems programmatically here and then attach the event handler
            //    // ...
            //}
        }
    }

    private void DetachExpandedEventHandlers(ItemCollection items)
    {
        foreach (var item in items)
        {
            if (item is TreeViewItem treeViewItem)
            {
                treeViewItem.Expanded -= TreeViewItem_Expanded; // Detach
                DetachExpandedEventHandlers(treeViewItem.Items); // Recursive call for child items
            }
            // Handle TreeNode or other data-bound items if necessary
        }
    }

    private void ButtonConnect_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(async () =>
        {
            _connectionData = await ShowConnectionDialogAsync();
            await RefreshAsync();
        }).FireAndForget();
    }

    private void ButtonRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshAsync().FireAndForget();

    private void ButtonGenerateClass_OnClick(object sender, RoutedEventArgs e)
    {
        if (GridDbObjects.SelectedItem is not DbObjectDef dbObject)
        {
            if (GridDbObjects.SelectedItem is not DbColumnDef column)
                return;

            dbObject = column.ParentDbObject;
        }

        var code = Generators.GenerateCSharpClass(dbObject);

        var dialog = new CodeDialog(code);
        dialog.ShowDialog();
    }

    private void ButtonGenerateSql_OnClick(object sender, RoutedEventArgs e)
    {
        if (GridStormTypes.SelectedItem is not StormTypeDef type)
        {
            if (GridStormTypes.SelectedItem is not StormPropertyDef prop)
                return;

            type = prop.ParentStormType;
        }

        string sql;
        try
        {
            sql = Generators.GenerateCreateTableSql(type, "dbo", true);
        }
        catch (Exception ex)
        {
            sql = "Error occured while generating sql script.\n" + ex.Message;
        }

        var dialog = new CodeDialog(sql);
        dialog.ShowDialog();
    }

    private void OnProgressTypes(string logMessage, int value, int maxValue, CancellationToken cancellationToken)
    {
        OnProgressAsync(LogListBoxTypes, ProgressBarTypes, logMessage, value, maxValue, cancellationToken).FireAndForget();
    }

    private void OnProgressDbObjects(string logMessage, int value, int maxValue, CancellationToken cancellationToken)
    {
        OnProgressAsync(LogListBoxDbObjects, ProgressBarDbObjects, logMessage, value, maxValue, cancellationToken).FireAndForget();
    }

    private static async Task OnProgressAsync(ListBox logListBox, ProgressBar progressBar, string logMessage, int value,
        int maxValue, CancellationToken cancellationToken)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        logListBox.Items.Add(logMessage);
        if (value < 0)
        {
            progressBar.IsIndeterminate = true;
        }
        else
        {
            progressBar.Maximum = maxValue;
            progressBar.Value = value;
            if (progressBar.IsIndeterminate)
                progressBar.IsIndeterminate = false;
        }

        await Task.Delay(200); //TODO
    }

    private void TabItemGitHubGotFocus(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 1;
        OpenBrowser("https://www.github.com/altasoft");
    }

    private void TabItemAltaSoftGotFocus(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 1;
        OpenBrowser("https://en.altasoft.net");
    }

    private static void OpenBrowser(string url)
    {
        System.Diagnostics.Process.Start(url);
    }

    private void InfoBar_ActionItemClicked(object sender, InfoBarActionItemEventArgs e)
    {
        throw new NotImplementedException();
    }

#pragma warning disable VSTHRD100
    private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100
    {
        if (_connectionData is null)
            return;

        if (sender is not TreeViewItem { DataContext: DbObjectDef dbObject } || !ReferenceEquals(dbObject.Columns, DbObjectDef.DummyColumns))
            return;

        try
        {
            await DbHelper.FillColumnsAsync(_connectionData, dbObject, dbObject.ObjectType, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await VS.StatusBar.ShowMessageAsync("Error: " + ex.Message);
            await VS.MessageBox.ShowErrorAsync("AltaSoft Storm", ex.ToString());
        }
    }
}
