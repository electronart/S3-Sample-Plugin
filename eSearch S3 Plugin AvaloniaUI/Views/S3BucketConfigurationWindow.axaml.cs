using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using eSearch_S3_Plugin_AvaloniaUI.ViewModels;
using System;

namespace eSearch_S3_Plugin_AvaloniaUI;

public partial class S3BucketConfigurationWindow : Window
{

    public event EventHandler<S3BucketConfigurationWindowViewModel> ClickedOK;

    public S3BucketConfigurationWindow()
    {
        InitializeComponent();

        BtnOK.Click += BtnOK_Click; // We only need an OK event for testing the connection.
    }

    private void BtnOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is S3BucketConfigurationWindowViewModel viewModel)
        {
            ClickedOK?.Invoke(this, viewModel);
        }
    }
}